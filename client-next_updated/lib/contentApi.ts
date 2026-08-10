import apiClient from './apiClient';

export const contentApi = {
  getPage: (slug: string) => apiClient.get<any>(`/api/content-pages/${encodeURIComponent(slug)}`, { skipAuthRedirect: true }),
  getLandingPage: () => apiClient.get<any>('/api/landing-page', { skipAuthRedirect: true }),
  getClientTheme: () => apiClient.get<any>('/api/client-theme', { skipAuthRedirect: true }),
};
