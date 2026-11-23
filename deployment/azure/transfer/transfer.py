"""Private, one-shot PostgreSQL/evidence import and snapshot jobs."""
import hashlib
import json
import os
from pathlib import Path
import re
import shutil
import subprocess
from datetime import datetime, timezone

TRANSFER = Path('/transfer')
DOCUMENTS = Path('/appdata/Documents')

def sql(statement, variables=()):
    args = ['psql', '--no-psqlrc', '--no-password', '--tuples-only', '--no-align', '--set=ON_ERROR_STOP=on']
    for name, value in variables:
        args.extend(['--set', name + '=' + value])
    result = subprocess.run(args, input=statement, text=True, capture_output=True, check=True)
    return result.stdout.strip()

def sha(path):
    digest = hashlib.sha256()
    with path.open('rb') as source:
        for block in iter(lambda: source.read(1024*1024), b''):
            digest.update(block)
    return digest.hexdigest()

def counts():
    return json.loads(sql('''SELECT json_build_object(
        'Users',(SELECT count(*) FROM "Users"),
        'WelfareApplications',(SELECT count(*) FROM "WelfareApplications"),
        'ApplicationReviews',(SELECT count(*) FROM "ApplicationReviews"),
        'ApplicationDocuments',(SELECT count(*) FROM "ApplicationDocuments"),
        'AuditLogs',(SELECT count(*) FROM "AuditLogs"));'''))

def document_manifest():
    entries = []
    DOCUMENTS.mkdir(parents=True, exist_ok=True)
    for path in sorted(DOCUMENTS.iterdir()):
        if not path.is_file() or not re.fullmatch(r'[a-f0-9]{32}\.(pdf|png|jpg|jpeg|doc|docx)', path.name):
            raise RuntimeError('Unexpected evidence entry; snapshot stopped.')
        entries.append({'file': path.name, 'sha256': sha(path)})
    return entries

def check_database_documents():
    rows = json.loads(sql('''SELECT coalesce(json_agg(json_build_object('key',"FilePath",'hash',"ContentHash")), '[]'::json) FROM "ApplicationDocuments";'''))
    for row in rows:
        key = row['key']
        if not re.fullmatch(r'[a-f0-9]{32}\.(pdf|png|jpg|jpeg|doc|docx)', key or ''):
            raise RuntimeError('Document metadata has an unsupported storage key.')
        path = DOCUMENTS / key
        if not path.is_file() or (row['hash'] and sha(path).lower() != row['hash'].lower()):
            raise RuntimeError('A stored document is missing or its checksum differs.')

def import_data():
    expected = json.loads((TRANSFER/'import-manifest.json').read_text())
    dump = TRANSFER/'AzurePrepared.dump'
    if sha(dump).lower() != expected['databaseSha256'].lower():
        raise RuntimeError('Import database checksum mismatch.')
    marker = TRANSFER/'import-complete.json'
    if sql('SELECT to_regclass(\'public."Users"\') IS NOT NULL;') == 't':
        if marker.exists() and json.loads(marker.read_text())['databaseSha256'] == expected['databaseSha256']:
            check_database_documents()
            print('The verified presentation import is already present; no records were replaced.')
            return
        raise RuntimeError('Target already has application tables. Existing records will not be replaced.')
    sql(r'''SELECT 'CREATE ROLE welfare_app LOGIN' WHERE NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname='welfare_app') \gexec
ALTER ROLE welfare_app PASSWORD :'app_password';
GRANT welfare_app TO welfare_admin;
GRANT CONNECT ON DATABASE "WelfareDb" TO welfare_app;
GRANT USAGE, CREATE ON SCHEMA public TO welfare_app;
''', [('app_password', os.environ['WELFARE_APP_PASSWORD'])])
    subprocess.run(['pg_restore', '--no-password', '--exit-on-error', '--single-transaction', '--no-owner', '--no-acl', '--role=welfare_app', '--dbname=WelfareDb', str(dump)], check=True)
    actual = counts()
    if actual != expected['rowCounts']:
        raise RuntimeError('Imported record counts differ from the prepared snapshot.')
    for entry in expected['evidence']:
        key = entry['file']
        if Path(key).name != key or sha(DOCUMENTS/key).lower() != entry['sha256'].lower():
            raise RuntimeError('Imported evidence checksum mismatch.')
    check_database_documents()
    marker.write_text(json.dumps({'databaseSha256':expected['databaseSha256'], 'rowCounts':actual, 'verifiedAtUtc':datetime.now(timezone.utc).isoformat()}, indent=2))
    print('Presentation database imported; record counts and evidence checksums match.')

def export_snapshot():
    snapshot = os.environ['SNAPSHOT_ID']
    if not re.fullmatch(r'[a-f0-9]{32}', snapshot):
        raise RuntimeError('Invalid snapshot identifier.')
    if os.environ.get('WRITES_QUIESCED') != 'true':
        raise RuntimeError('The coordinating client must pause application writes first.')
    destination = TRANSFER / ('snapshot-' + snapshot)
    destination.mkdir(exist_ok=False)
    check_database_documents()
    entries = document_manifest()
    dump = destination/'WelfareDb.dump'
    subprocess.run(['pg_dump', '--no-password', '--format=custom', '--no-owner', '--no-acl', '--file', str(dump)], check=True)
    evidence = destination/'Documents'
    evidence.mkdir()
    for entry in entries:
        shutil.copy2(DOCUMENTS/entry['file'], evidence/entry['file'])
        if sha(evidence/entry['file']) != entry['sha256']:
            raise RuntimeError('Evidence changed during snapshot creation.')
    manifest = {'createdAtUtc':datetime.now(timezone.utc).isoformat(), 'databaseFile':'WelfareDb.dump', 'databaseSha256':sha(dump), 'rowCounts':counts(), 'evidence':entries, 'restoreTested':False}
    (destination/'manifest.json').write_text(json.dumps(manifest, indent=2))
    print('Coordinated snapshot exported with database and evidence checksums.')

if os.environ.get('PGDATABASE') != 'WelfareDb':
    raise RuntimeError('Only the configured WelfareDb presentation database is supported.')
os.environ['PGSSLMODE'] = 'require'
mode = os.environ.get('MODE')
if mode == 'import': import_data()
elif mode == 'export': export_snapshot()
else: raise RuntimeError('Choose import or export mode explicitly.')
