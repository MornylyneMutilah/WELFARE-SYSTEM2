import { test } from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import vm from 'node:vm';
import { fileURLToPath } from 'node:url';
import ts from 'typescript';
import axios from 'axios';

const testDirectory = path.dirname(fileURLToPath(import.meta.url));

function storage() {
  const values = new Map();
  return { getItem: key => values.get(key) ?? null, setItem: (key, value) => values.set(key, String(value)), removeItem: key => values.delete(key) };
}
function tab(shared, session = storage()) {
  const redirects = [];
  const window = { localStorage: shared, sessionStorage: session, location: { origin: 'http://localhost:3000', assign: url => redirects.push(url) }, dispatchEvent: () => {} };
  function load(relative, dependencies = {}) {
    const filename = path.join(testDirectory, '..', relative);
    const code = ts.transpileModule(fs.readFileSync(filename, 'utf8'), { compilerOptions: { module: ts.ModuleKind.CommonJS, target: ts.ScriptTarget.ES2022, esModuleInterop: true } }).outputText;
    const compiledModule = { exports: {} };
    const dependency = name => {
      if (name === 'axios') return axios;
      if (dependencies[name]) return dependencies[name];
      throw new Error(`Unexpected test dependency: ${name}`);
    };
    vm.runInNewContext(code, { module: compiledModule, exports: compiledModule.exports, require: dependency, window, localStorage: shared, sessionStorage: session, URL, FormData, Event, process: { env: {} } }, { filename });
    return compiledModule.exports;
  }
  const tokens = load('lib/auth/tokenUtils.ts').tokenUtils;
  const client = load('lib/api/client.ts', { '../auth/tokenUtils': { tokenUtils: tokens } }).default;
  return { tokens, client, session, redirects };
}
function signIn(t, name) {
  t.tokens.setToken(name + '-access');
  t.tokens.setRefreshToken(name + '-refresh');
  t.tokens.setUser(JSON.stringify({ role: name }));
}
const success = headers => async config => { headers.push(config.headers.Authorization); return { status: 200, statusText: 'OK', headers: {}, config, data: { id: 1 } }; };

test('administrator saves use the administrator token after another tab signs in as Employee', async () => {
  const shared = storage(), admin = tab(shared), employee = tab(shared);
  signIn(admin, 'Administrator'); signIn(employee, 'Employee');
  const headers = [];
  await admin.client.put('/welfareprograms/1', { maxAmount: 2000 }, { adapter: success(headers) });
  await employee.client.get('/applications', { adapter: success(headers) });
  assert.deepEqual(headers, ['Bearer Administrator-access', 'Bearer Employee-access']);
  assert.equal(JSON.parse(admin.tokens.getUser()).role, 'Administrator');
  assert.equal(shared.getItem('token'), null);
});

test('refresh and reload preserve each tab and logout does not sign out the other role', async () => {
  const shared = storage(), admin = tab(shared), employee = tab(shared);
  signIn(admin, 'Administrator'); signIn(employee, 'Employee');
  employee.tokens.setToken('Employee-refreshed');
  const reloadedAdmin = tab(shared, admin.session);
  assert.equal(reloadedAdmin.tokens.getToken(), 'Administrator-access');
  employee.tokens.clearAll();
  assert.equal(employee.tokens.getToken(), null);
  assert.equal(employee.tokens.getUser(), null);
  assert.equal(reloadedAdmin.tokens.getRefreshToken(), 'Administrator-refresh');
});

test('401 clears only the rejected tab session', async () => {
  const shared = storage(), admin = tab(shared), employee = tab(shared);
  signIn(admin, 'Administrator'); signIn(employee, 'Employee');
  await assert.rejects(employee.client.get('/applications', { adapter: async config => { throw new axios.AxiosError('Unauthorized', 'ERR_BAD_REQUEST', config, null, { status: 401, data: {}, config }); } }));
  assert.equal(employee.tokens.getToken(), null);
  assert.equal(admin.tokens.getToken(), 'Administrator-access');
  assert.deepEqual(employee.redirects, ['http://localhost:3000/login']);
  assert.deepEqual(admin.redirects, []);
});

test('legacy sessions migrate once and cannot silently return after logout', () => {
  const shared = storage();
  shared.setItem('token', 'legacy'); shared.setItem('refreshToken', 'legacy-refresh'); shared.setItem('user', '{"role":"Administrator"}');
  const old = tab(shared);
  assert.equal(old.tokens.getToken(), 'legacy');
  assert.equal(old.tokens.getRefreshToken(), 'legacy-refresh');
  assert.equal(shared.getItem('token'), null);
  old.tokens.clearAll(); shared.setItem('token', 'some-other-old-tab');
  assert.equal(old.tokens.getToken(), null);
});

test('a new tab never imports another tab’s isolated session', () => {
  const shared = storage(), admin = tab(shared);
  signIn(admin, 'Administrator');
  const fresh = tab(shared);
  assert.equal(fresh.tokens.getToken(), null);
  assert.equal(fresh.tokens.getUser(), null);
});

test('password-change 403 redirects to account security in the response handler', async () => {
  const current = tab(storage());signIn(current, 'Administrator');
  await assert.rejects(current.client.put('/welfareprograms/1', {}, { adapter: async config => { throw new axios.AxiosError('Forbidden', 'ERR_BAD_REQUEST', config, null, { status: 403, data: { code: 'PASSWORD_CHANGE_REQUIRED' }, config }); } }));
  assert.deepEqual(current.redirects, ['http://localhost:3000/account/security']);
  assert.equal(current.tokens.getToken(), 'Administrator-access');
});
