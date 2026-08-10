import { useRouter } from 'next/router';
import { useEffect, useState } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { setAuthTokens, clearAuth, setLoading, setUser } from '../store/authSlice';
import type { AppDispatch, RootState } from '../store';
import { authApi } from './authApi';

const AUTH_SESSION_VERSION_KEY = 'medsocial.client.auth.version';
const AUTH_SESSION_VERSION = '2026-04-24-2';

function clearBrowserSession() {
  if (typeof window === 'undefined') {
    return;
  }

  localStorage.removeItem('accessToken');
  localStorage.removeItem('refreshToken');
  localStorage.removeItem(AUTH_SESSION_VERSION_KEY);
}

function storeBrowserSession(accessToken: string, refreshToken: string) {
  localStorage.setItem('accessToken', accessToken);
  localStorage.setItem('refreshToken', refreshToken);
  localStorage.setItem(AUTH_SESSION_VERSION_KEY, AUTH_SESSION_VERSION);
}

export function useAuth() {
  const dispatch = useDispatch<AppDispatch>();
  const router = useRouter();
  const { user, isAuthenticated, isLoading } = useSelector((state: RootState) => state.auth);
  const [hydrated, setHydrated] = useState(false);

  useEffect(() => {
    let mounted = true;

    async function hydrateUser() {
      setHydrated(true);
      if (typeof window === 'undefined') {
        return;
      }

      const accessToken = localStorage.getItem('accessToken');
      const refreshToken = localStorage.getItem('refreshToken');
      const sessionVersion = localStorage.getItem(AUTH_SESSION_VERSION_KEY);
      if (sessionVersion !== AUTH_SESSION_VERSION) {
        clearBrowserSession();
        dispatch(clearAuth());
        return;
      }

      if (!accessToken || !refreshToken || user) {
        return;
      }

      try {
        dispatch(setLoading(true));
        const currentUser = await authApi.getCurrentUser();
        if (mounted) {
          dispatch(setAuthTokens({ accessToken, refreshToken, user: currentUser }));
        }
      } catch {
        if (mounted) {
          clearBrowserSession();
          dispatch(clearAuth());
        }
      } finally {
        if (mounted) {
          dispatch(setLoading(false));
        }
      }
    }

    hydrateUser();
    return () => {
      mounted = false;
    };
  }, [dispatch, user]);

  const login = async (email: string, password: string) => {
    if (process.env.NEXT_PUBLIC_BYPASS_AUTH === 'true') {
      const role = email.toLowerCase().includes('admin') ? 'SuperAdmin' : email.toLowerCase().includes('employer') ? 'Employer' : 'Professional';
      const fakeUser = {
        id: '00000000-0000-0000-0000-000000000000',
        tenantId: '00000000-0000-0000-0000-000000000001',
        email,
        firstName: 'Demo',
        lastName: role,
        userType: role,
        subscriptionTier: 'free',
        verificationStatus: 'Verified',
        createdAt: new Date().toISOString(),
      };
      const fakeToken = 'bypass-token';
      storeBrowserSession(fakeToken, fakeToken);
      dispatch(setAuthTokens({ accessToken: fakeToken, refreshToken: fakeToken, user: fakeUser }));
      return { success: true, user: fakeUser };
    }

    try {
      dispatch(setLoading(true));
      const response = await authApi.login(email, password);
      storeBrowserSession(response.accessToken, response.refreshToken);
      const currentUser = await authApi.getCurrentUser();
      dispatch(setAuthTokens({ accessToken: response.accessToken, refreshToken: response.refreshToken, user: currentUser }));
      return { success: true, user: currentUser };
    } catch (error: any) {
      clearBrowserSession();
      dispatch(clearAuth());
      return {
        success: false,
        error: error.response?.data?.errors?.[0] || 'Login failed',
      };
    } finally {
      dispatch(setLoading(false));
    }
  };

  const register = async (email: string, password: string, firstName: string, lastName: string, userType = 'Professional', acceptedTerms = false, acceptedPrivacyPolicy = false) => {
    try {
      dispatch(setLoading(true));
      const response = await authApi.register(email, password, firstName, lastName, userType, acceptedTerms, acceptedPrivacyPolicy);
      storeBrowserSession(response.accessToken, response.refreshToken);
      const currentUser = await authApi.getCurrentUser();
      dispatch(setAuthTokens({ accessToken: response.accessToken, refreshToken: response.refreshToken, user: currentUser }));
      return { success: true, user: currentUser };
    } catch (error: any) {
      clearBrowserSession();
      dispatch(clearAuth());
      return {
        success: false,
        error: error.response?.data?.errors?.[0] || 'Registration failed',
      };
    } finally {
      dispatch(setLoading(false));
    }
  };

  const bootstrapUser = async () => {
    const currentUser = await authApi.getCurrentUser();
    dispatch(setUser(currentUser));
    return currentUser;
  };

  const logout = async () => {
    try {
      const refreshToken = localStorage.getItem('refreshToken');
      if (refreshToken && process.env.NEXT_PUBLIC_BYPASS_AUTH !== 'true') {
        await authApi.logout(refreshToken);
      }
    } catch {
      // Ignore logout transport errors and clear client state.
    } finally {
      clearBrowserSession();
      dispatch(clearAuth());
      router.push('/login');
    }
  };

  return {
    user,
    isAuthenticated,
    isLoading,
    hydrated,
    login,
    register,
    logout,
    bootstrapUser,
  };
}

export function useRequireAuth() {
  const router = useRouter();
  const { isAuthenticated, isLoading, hydrated } = useAuth();

  useEffect(() => {
    if (hydrated && !isLoading && !isAuthenticated) {
      router.push(`/login?next=${encodeURIComponent(router.asPath)}`);
    }
  }, [hydrated, isAuthenticated, isLoading, router]);

  return { hydrated, isAuthenticated, isLoading };
}
