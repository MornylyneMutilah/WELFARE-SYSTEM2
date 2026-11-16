const SESSION_KEYS = ['token', 'refreshToken', 'user'] as const;
const MIGRATION_KEY = 'welfare:tab-session';

function tabStorage(): Storage | null {
  if (typeof window === 'undefined') return null;
  const storage = window.sessionStorage;
  if (storage.getItem(MIGRATION_KEY) !== '1') {
    // Retain the existing sign-in once, then stop sharing credentials between tabs.
    // Keep the marker on logout so a stale legacy token cannot sign this tab back in.
    for (const key of SESSION_KEYS) {
      const legacy = window.localStorage.getItem(key);
      if (!storage.getItem(key) && legacy) storage.setItem(key, legacy);
    }
    storage.setItem(MIGRATION_KEY, '1');
    for (const key of SESSION_KEYS) window.localStorage.removeItem(key);
  }
  return storage;
}

export const tokenUtils = {
  getToken: (): string | null => tabStorage()?.getItem('token') ?? null,
  setToken: (token: string): void => { tabStorage()?.setItem('token', token); },
  removeToken: (): void => { tabStorage()?.removeItem('token'); },
  getRefreshToken: (): string | null => tabStorage()?.getItem('refreshToken') ?? null,
  setRefreshToken: (token: string): void => { tabStorage()?.setItem('refreshToken', token); },
  removeRefreshToken: (): void => { tabStorage()?.removeItem('refreshToken'); },
  getUser: (): string | null => tabStorage()?.getItem('user') ?? null,
  setUser: (user: string): void => { tabStorage()?.setItem('user', user); },
  clearAll: (): void => {
    const storage = tabStorage();
    for (const key of SESSION_KEYS) storage?.removeItem(key);
  },
};
