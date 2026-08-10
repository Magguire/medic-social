import { useEffect, useMemo, useState } from 'react';
import { useRouter } from 'next/router';
import { adminApi, clearSession, getStoredUser, UnauthorizedError } from './api';

const PUBLIC_ROUTES = new Set(['/login']);
const ADMIN_ROLES = new Set(['SuperAdmin', 'TenantAdmin', 'Auditor']);

export function useAdminAuth() {
  const router = useRouter();
  const [user, setUser] = useState(null);
  const [hydrated, setHydrated] = useState(false);
  const [isAuthenticated, setIsAuthenticated] = useState(false);

  useEffect(() => {
    if (typeof window === 'undefined') {
      return;
    }

    const token = localStorage.getItem('accessToken');
    const storedUser = getStoredUser();
    const publicRoute = PUBLIC_ROUTES.has(router.pathname);

    if (!token) {
      setHydrated(true);
      setUser(null);
      setIsAuthenticated(false);
      if (!publicRoute) {
        router.replace(`/login?next=${encodeURIComponent(router.asPath)}`);
      }
      return;
    }

    if (storedUser) {
      setUser(storedUser);
      setIsAuthenticated(true);
    }

    adminApi.getCurrentUser()
      .then((currentUser) => {
        if (!ADMIN_ROLES.has(currentUser.userType)) {
          clearSession();
          setUser(currentUser);
          setIsAuthenticated(false);
          setHydrated(true);
          return;
        }

        setUser(currentUser);
        setIsAuthenticated(true);
        if (typeof window !== 'undefined') {
          localStorage.setItem('medsocial.admin.user', JSON.stringify(currentUser));
        }
      })
      .catch((error) => {
        clearSession();
        setUser(null);
        setIsAuthenticated(false);
        if (!(error instanceof UnauthorizedError) && !publicRoute) {
          router.replace(`/login?next=${encodeURIComponent(router.asPath)}`);
        }
      })
      .finally(() => setHydrated(true));
  }, [router.asPath, router.pathname]);

  const auth = useMemo(() => ({
    user,
    hydrated,
    isAuthenticated,
    logout: async () => {
      await adminApi.logout();
      setUser(null);
      setIsAuthenticated(false);
      router.push('/login');
    },
  }), [hydrated, isAuthenticated, router, user]);

  return auth;
}
