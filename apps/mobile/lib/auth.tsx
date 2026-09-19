import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from 'react';
import { endpoints } from '@/lib/endpoints';
import { ApiError } from '@/lib/api';
import { getToken, setToken } from '@/lib/tokenStorage';
import type { AuthResponse, Entitlements, User } from '@/lib/types';

type AuthContextValue = {
  ready: boolean;
  token: string | null;
  user: User | null;
  entitlements: Entitlements | null;
  isPro: boolean;
  signIn: (email: string, password: string) => Promise<void>;
  register: (payload: { organisationName: string; fullName: string; email: string; password: string }) => Promise<void>;
  signOut: () => Promise<void>;
  refresh: () => Promise<void>;
  refreshBilling: () => Promise<void>;
  setUser: (user: User) => void;
};

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [ready, setReady] = useState(false);
  const [token, setTokenState] = useState<string | null>(null);
  const [user, setUser] = useState<User | null>(null);
  const [entitlements, setEntitlements] = useState<Entitlements | null>(null);

  const loadBilling = useCallback(async () => {
    try {
      setEntitlements(await endpoints.entitlements());
    } catch {
      setEntitlements(null);
    }
  }, []);

  const apply = useCallback(async (auth: AuthResponse) => {
    await setToken(auth.accessToken);
    setTokenState(auth.accessToken);
    setUser(auth.user);
    await loadBilling();
  }, [loadBilling]);

  useEffect(() => {
    let cancelled = false;
    (async () => {
      const stored = await getToken();
      if (!stored) {
        if (!cancelled) {
          setReady(true);
        }
        return;
      }
      setTokenState(stored);
      try {
        const me = await endpoints.me();
        if (!cancelled) {
          setUser(me);
          await loadBilling();
        }
      } catch (err) {
        if (err instanceof ApiError && (err.status === 401 || err.status === 404)) {
          await setToken(null);
          if (!cancelled) {
            setTokenState(null);
            setUser(null);
          }
        }
      } finally {
        if (!cancelled) {
          setReady(true);
        }
      }
    })();
    return () => {
      cancelled = true;
    };
  }, []);

  const signIn = useCallback(
    async (email: string, password: string) => {
      const auth = await endpoints.login(email.trim(), password);
      await apply(auth);
    },
    [apply],
  );

  const register = useCallback(
    async (payload: { organisationName: string; fullName: string; email: string; password: string }) => {
      const auth = await endpoints.register(payload);
      await apply(auth);
    },
    [apply],
  );

  const signOut = useCallback(async () => {
    await setToken(null);
    setTokenState(null);
    setUser(null);
    setEntitlements(null);
  }, []);

  const refresh = useCallback(async () => {
    const me = await endpoints.me();
    setUser(me);
    await loadBilling();
  }, [loadBilling]);

  const value = useMemo(
    () => ({
      ready,
      token,
      user,
      entitlements,
      isPro: Boolean(entitlements?.isPro),
      signIn,
      register,
      signOut,
      refresh,
      refreshBilling: loadBilling,
      setUser,
    }),
    [ready, token, user, entitlements, signIn, register, signOut, refresh, loadBilling],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext);
  if (!ctx) {
    throw new Error('useAuth must be used within AuthProvider');
  }
  return ctx;
}
