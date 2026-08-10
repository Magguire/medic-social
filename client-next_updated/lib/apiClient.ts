import axios, { AxiosError, AxiosInstance } from 'axios';
import { getApiBaseUrl } from './runtimeConfig';
import { getBrowserDeviceId } from './deviceIdentity';

const AUTH_SESSION_VERSION_KEY = 'medsocial.client.auth.version';
const AUTH_SESSION_VERSION = '2026-04-24-2';

interface ApiErrorResponse {
  message: string;
  code?: string;
}

type RetryableRequestConfig = {
  _retry?: boolean;
  skipAuthRedirect?: boolean;
  headers?: Record<string, string>;
  url?: string;
};

class ApiClient {
  private client: AxiosInstance;

  constructor() {
    this.client = axios.create({
      headers: {
        'Content-Type': 'application/json',
      },
    });

    this.client.interceptors.request.use(this.requestInterceptor.bind(this));
    this.client.interceptors.response.use(
      this.responseInterceptor.bind(this),
      this.errorInterceptor.bind(this)
    );
  }

  private async requestInterceptor(config: any) {
    config.baseURL = getApiBaseUrl();
    if (typeof window !== 'undefined') {
      config.headers = config.headers || {};
      const token = localStorage.getItem('accessToken');
      if (token) {
        config.headers.Authorization = `Bearer ${token}`;
      }
      config.headers['X-MedSocial-Device-Id'] = await getBrowserDeviceId();
    }
    return config;
  }

  private responseInterceptor(response: any) {
    return response;
  }

  private async errorInterceptor(error: AxiosError) {
    const responseStatus = error.response?.status;
    const originalRequest = (error.config || {}) as RetryableRequestConfig;

    if (responseStatus !== 401 || typeof window === 'undefined') {
      return Promise.reject(error);
    }

    const requestUrl = originalRequest.url || '';
    const isAuthEndpoint = requestUrl.includes('/api/auth/login')
      || requestUrl.includes('/api/auth/register')
      || requestUrl.includes('/api/auth/refresh')
      || requestUrl.includes('/api/auth/logout');

    if (originalRequest.skipAuthRedirect) {
      return Promise.reject(error);
    }

    if (isAuthEndpoint || originalRequest._retry) {
      this.clearSessionAndRedirect();
      return Promise.reject(error);
    }

    try {
      originalRequest._retry = true;
      const accessToken = await this.refreshAccessToken();
      originalRequest.headers = {
        ...(originalRequest.headers || {}),
        Authorization: `Bearer ${accessToken}`,
      };
      return this.client(originalRequest as any);
    } catch (refreshError) {
      this.clearSessionAndRedirect();
      return Promise.reject(refreshError);
    }
  }

  private async refreshAccessToken() {
    try {
      if (typeof window === 'undefined') {
        throw new Error('Refresh token not available');
      }

      const refreshToken = localStorage.getItem('refreshToken');
      if (!refreshToken) {
        throw new Error('No refresh token available');
      }

      const response = await axios.post(`${getApiBaseUrl()}/api/auth/refresh`, {
        refreshToken,
        deviceId: await getBrowserDeviceId(),
      });

      const { accessToken, refreshToken: newRefreshToken } = response.data;
      localStorage.setItem('accessToken', accessToken);
      if (newRefreshToken) {
        localStorage.setItem('refreshToken', newRefreshToken);
      }
      return accessToken as string;
    } catch (error) {
      throw error;
    }
  }

  private clearSessionAndRedirect() {
    if (typeof window === 'undefined') {
      return;
    }

    localStorage.removeItem('accessToken');
    localStorage.removeItem('refreshToken');
    localStorage.removeItem(AUTH_SESSION_VERSION_KEY);

    if (window.location.pathname.startsWith('/login')) {
      return;
    }

    const next = `${window.location.pathname}${window.location.search}${window.location.hash}`;
    window.location.replace(`/login?next=${encodeURIComponent(next)}`);
  }

  async get<T>(url: string, config?: any): Promise<T> {
    const response = await this.client.get<T>(url, config);
    return response.data;
  }

  async post<T>(url: string, data?: any, config?: any): Promise<T> {
    const response = await this.client.post<T>(url, data, config);
    return response.data;
  }

  async put<T>(url: string, data?: any, config?: any): Promise<T> {
    const response = await this.client.put<T>(url, data, config);
    return response.data;
  }

  async patch<T>(url: string, data?: any, config?: any): Promise<T> {
    const response = await this.client.patch<T>(url, data, config);
    return response.data;
  }

  async delete<T>(url: string, config?: any): Promise<T> {
    const response = await this.client.delete<T>(url, config);
    return response.data;
  }
}

export default new ApiClient();
