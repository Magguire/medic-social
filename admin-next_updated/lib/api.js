import { buildApiUrl } from './runtimeConfig';
import { getBrowserDeviceId } from './deviceIdentity';

const ACCESS_TOKEN_KEY = 'accessToken';
const REFRESH_TOKEN_KEY = 'refreshToken';
const ADMIN_USER_KEY = 'medsocial.admin.user';

export class UnauthorizedError extends Error {
  constructor(message = 'Authentication required') {
    super(message);
    this.name = 'UnauthorizedError';
  }
}

export function getStoredUser() {
  if (typeof window === 'undefined') {
    return null;
  }

  try {
    const raw = localStorage.getItem(ADMIN_USER_KEY);
    return raw ? JSON.parse(raw) : null;
  } catch {
    return null;
  }
}

export function persistSession(payload) {
  if (typeof window === 'undefined') {
    return;
  }

  localStorage.setItem(ACCESS_TOKEN_KEY, payload.accessToken);
  localStorage.setItem(REFRESH_TOKEN_KEY, payload.refreshToken);
  localStorage.setItem(ADMIN_USER_KEY, JSON.stringify(payload.user));
}

export function clearSession() {
  if (typeof window === 'undefined') {
    return;
  }

  localStorage.removeItem(ACCESS_TOKEN_KEY);
  localStorage.removeItem(REFRESH_TOKEN_KEY);
  localStorage.removeItem(ADMIN_USER_KEY);
}

function redirectToLogin() {
  if (typeof window === 'undefined') {
    return;
  }

  if (window.location.pathname === '/login') {
    return;
  }

  const next = `${window.location.pathname}${window.location.search || ''}`;
  window.location.assign(`/login?next=${encodeURIComponent(next)}`);
}

export async function apiRequest(path, options = {}) {
  const headers = { ...(options.headers || {}) };
  const includeAuth = options.includeAuth !== false;
  const redirectOnUnauthorized = options.redirectOnUnauthorized !== false;

  if (includeAuth && typeof window !== 'undefined') {
    const token = localStorage.getItem(ACCESS_TOKEN_KEY);
    if (token) {
      headers.Authorization = `Bearer ${token}`;
    }
    headers['X-MedSocial-Device-Id'] = await getBrowserDeviceId();
  }

  if (!(options.body instanceof FormData) && !headers['Content-Type']) {
    headers['Content-Type'] = 'application/json';
  }

  const { includeAuth: _, redirectOnUnauthorized: __, ...fetchOptions } = options;
  const response = await fetch(buildApiUrl(path), { ...fetchOptions, headers });
  if (!response.ok) {
    let message = 'Request failed';
    try {
      const payload = await response.json();
      message = payload.errors?.[0] || payload.message || message;
    } catch {
      // ignore json parse failures
    }

    if (response.status === 401) {
      if (redirectOnUnauthorized) {
        clearSession();
        redirectToLogin();
      }
      throw new UnauthorizedError(message);
    }

    throw new Error(message);
  }

  if (response.status === 204) {
    return null;
  }

  const contentType = response.headers.get('content-type');
  if (!contentType || !contentType.includes('application/json')) {
    return null;
  }

  return response.json();
}

export const adminApi = {
  login: async ({ email, password }) => {
    const deviceId = await getBrowserDeviceId();
    const response = await apiRequest('/api/auth/login', {
      method: 'POST',
      body: JSON.stringify({ email, password, deviceId }),
      headers: { 'Content-Type': 'application/json' },
      includeAuth: false,
      redirectOnUnauthorized: false,
    });
    persistSession(response);
    return response;
  },
  changePassword: (payload) => apiRequest('/api/auth/reset-password', {
    method: 'POST',
    body: JSON.stringify(payload),
    headers: { 'Content-Type': 'application/json' },
  }),
  getPasswordPolicy: () => apiRequest('/api/auth/password-policy'),
  updatePasswordPolicy: (payload) => apiRequest('/api/auth/password-policy', {
    method: 'PUT',
    body: JSON.stringify(payload),
    headers: { 'Content-Type': 'application/json' },
  }),
  adminResetPassword: (payload) => apiRequest('/api/auth/admin-reset-password', {
    method: 'POST',
    body: JSON.stringify(payload),
    headers: { 'Content-Type': 'application/json' },
  }),
  logout: async () => {
    const refreshToken = typeof window !== 'undefined' ? localStorage.getItem(REFRESH_TOKEN_KEY) : null;
    try {
      if (refreshToken) {
        await apiRequest('/api/auth/logout', {
          method: 'POST',
          body: JSON.stringify({ refreshToken }),
          headers: { 'Content-Type': 'application/json' },
        });
      }
    } catch {
      // Ignore transport errors and clear the local session regardless.
    } finally {
      clearSession();
    }
  },
  refresh: async () => {
    const refreshToken = typeof window !== 'undefined' ? localStorage.getItem(REFRESH_TOKEN_KEY) : null;
    if (!refreshToken) {
      throw new UnauthorizedError('No refresh token is available');
    }

    const deviceId = await getBrowserDeviceId();
    const response = await apiRequest('/api/auth/refresh', {
      method: 'POST',
      body: JSON.stringify({ refreshToken, deviceId }),
      headers: { 'Content-Type': 'application/json' },
      includeAuth: false,
      redirectOnUnauthorized: false,
    });
    persistSession(response);
    return response;
  },
  getCurrentUser: () => apiRequest('/api/users/me'),
  heartbeat: async () => apiRequest('/api/audit/heartbeat', {
    method: 'POST',
    body: JSON.stringify({ deviceId: await getBrowserDeviceId() }),
  }),
  getDashboard: () => apiRequest('/api/admin/dashboard'),
  getAudit: ({ pageNumber = 1, pageSize = 25, action = '', entityName = '', archived = false } = {}) => {
    const params = new URLSearchParams({ pageNumber: String(pageNumber), pageSize: String(pageSize) });
    if (action) params.set('action', action);
    if (entityName) params.set('entityName', entityName);
    params.set('archived', String(archived));
    return apiRequest(`/api/audit?${params.toString()}`);
  },
  archiveAudit: (ids) => apiRequest('/api/audit/archive', { method: 'POST', body: JSON.stringify({ ids }) }),
  getSessions: () => apiRequest('/api/admin/sessions'),
  getSession: (id) => apiRequest(`/api/admin/sessions/${id}`),
  endSession: (id) => apiRequest(`/api/admin/sessions/${id}/end`, { method: 'POST' }),
  endUserSessions: (userId) => apiRequest(`/api/admin/sessions/users/${userId}/end`, { method: 'POST' }),
  getCommunicationConfigs: () => apiRequest('/api/communications/configs'),
  saveCommunicationConfig: (payload) => apiRequest('/api/communications/configs', { method: 'POST', body: JSON.stringify(payload) }),
  sendCommunication: (payload) => apiRequest('/api/communications/send', { method: 'POST', body: JSON.stringify(payload) }),
  getCommunicationMessages: ({ pageNumber = 1, pageSize = 25, channel = '' } = {}) => {
    const params = new URLSearchParams({ pageNumber: String(pageNumber), pageSize: String(pageSize) });
    if (channel) params.set('channel', channel);
    return apiRequest(`/api/communications/messages?${params.toString()}`);
  },
  getVerificationRequests: (status) => apiRequest(`/api/verification${status ? `?status=${encodeURIComponent(status)}` : ''}`),
  approveVerification: (id, reviewerId, bypassIntegration) => apiRequest(`/api/verification/${id}/approve`, { method: 'POST', body: JSON.stringify({ reviewerId, bypassIntegration }) }),
  rejectVerification: (id, reviewerId, reason, bypassIntegration) => apiRequest(`/api/verification/${id}/reject`, { method: 'POST', body: JSON.stringify({ reviewerId, reason, bypassIntegration }) }),
  getConfiguration: () => apiRequest('/api/admin/configuration'),
  createCategory: (payload) => apiRequest('/api/admin/configuration/categories', { method: 'POST', body: JSON.stringify(payload) }),
  updateCategory: (id, payload) => apiRequest(`/api/admin/configuration/categories/${id}`, { method: 'PUT', body: JSON.stringify(payload) }),
  createJobEngagementType: (payload) => apiRequest('/api/admin/configuration/job-engagement-types', { method: 'POST', body: JSON.stringify(payload) }),
  updateJobEngagementType: (id, payload) => apiRequest(`/api/admin/configuration/job-engagement-types/${id}`, { method: 'PUT', body: JSON.stringify(payload) }),
  createPlan: (payload) => apiRequest('/api/admin/configuration/subscription-plans', { method: 'POST', body: JSON.stringify(payload) }),
  updatePlan: (id, payload) => apiRequest(`/api/admin/configuration/subscription-plans/${id}`, { method: 'PUT', body: JSON.stringify(payload) }),
  getPaymentConfigs: () => apiRequest('/api/subscriptions/admin/payment-configs'),
  savePaymentConfig: (payload) => apiRequest('/api/subscriptions/admin/payment-configs', { method: 'POST', body: JSON.stringify(payload) }),
  testPaymentConfig: (provider) => apiRequest(`/api/subscriptions/admin/payment-configs/${provider}/test`, { method: 'POST' }),
  getPaymentTransactions: () => apiRequest('/api/subscriptions/admin/transactions'),
  getPayAsYouGoRules: () => apiRequest('/api/subscriptions/admin/paygo-rules'),
  savePayAsYouGoRule: (payload) => apiRequest('/api/subscriptions/admin/paygo-rules', { method: 'POST', body: JSON.stringify(payload) }),
  getPayAsYouGoCharges: () => apiRequest('/api/subscriptions/admin/paygo-charges'),
  getContentPages: () => apiRequest('/api/content-pages/admin'),
  saveContentPage: (payload) => apiRequest('/api/content-pages/admin', { method: 'POST', body: JSON.stringify(payload) }),
  uploadContentPageDocument: (slug, file) => {
    const form = new FormData();
    form.append('file', file);
    return apiRequest(`/api/content-pages/admin/${encodeURIComponent(slug)}/document`, { method: 'POST', body: form });
  },
  getLandingPage: () => apiRequest('/api/landing-page/admin'),
  saveLandingPage: (payload) => apiRequest('/api/landing-page/admin', { method: 'PUT', body: JSON.stringify(payload) }),
  getClientTheme: () => apiRequest('/api/client-theme/admin'),
  saveClientTheme: (payload) => apiRequest('/api/client-theme/admin', { method: 'PUT', body: JSON.stringify(payload) }),
  activateSubscription: (payload) => apiRequest('/api/subscriptions/admin/activate', { method: 'POST', body: JSON.stringify(payload) }),
  createDocumentRule: (payload) => apiRequest('/api/admin/configuration/document-rules', { method: 'POST', body: JSON.stringify(payload) }),
  updateDocumentRule: (id, payload) => apiRequest(`/api/admin/configuration/document-rules/${id}`, { method: 'PUT', body: JSON.stringify(payload) }),
  createVerificationPolicy: (payload) => apiRequest('/api/admin/configuration/verification-policies', { method: 'POST', body: JSON.stringify(payload) }),
  updateVerificationPolicy: (id, payload) => apiRequest(`/api/admin/configuration/verification-policies/${id}`, { method: 'PUT', body: JSON.stringify(payload) }),
  createDocumentType: (payload) => apiRequest('/api/admin/configuration/document-types', { method: 'POST', body: JSON.stringify(payload) }),
  updateDocumentType: (id, payload) => apiRequest(`/api/admin/configuration/document-types/${id}`, { method: 'PUT', body: JSON.stringify(payload) }),
  createVerificationIntegration: (payload) => apiRequest('/api/admin/configuration/verification-integrations', { method: 'POST', body: JSON.stringify(payload) }),
  updateVerificationIntegration: (id, payload) => apiRequest(`/api/admin/configuration/verification-integrations/${id}`, { method: 'PUT', body: JSON.stringify(payload) }),
  getProfessionals: () => apiRequest('/api/professionals'),
  getEmployers: () => apiRequest('/api/employers'),
  updateEmployer: (id, payload) => apiRequest(`/api/employers/${id}`, { method: 'PUT', body: JSON.stringify(payload) }),
  updateProfessional: (id, payload) => apiRequest(`/api/professionals/${id}`, { method: 'PUT', body: JSON.stringify(payload) }),
  setProfessionalVerification: (id, payload) => apiRequest(`/api/professionals/${id}/verification`, { method: 'POST', body: JSON.stringify(payload) }),
  getJobs: (filters = {}) => {
    const params = new URLSearchParams({
      pageNumber: String(filters.pageNumber || 1),
      pageSize: String(filters.pageSize || 40),
    });
    if (filters.q) params.set('q', filters.q);
    if (filters.category) params.set('category', filters.category);
    if (filters.department) params.set('department', filters.department);
    if (filters.engagementType) params.set('engagementType', filters.engagementType);
    if (filters.location) params.set('location', filters.location);
    if (filters.requireVerifiedProfessional !== undefined && filters.requireVerifiedProfessional !== '') {
      params.set('requireVerifiedProfessional', String(filters.requireVerifiedProfessional));
    }
    if (filters.salaryMin) params.set('salaryMin', filters.salaryMin);
    if (filters.salaryMax) params.set('salaryMax', filters.salaryMax);
    return apiRequest(`/api/jobs?${params.toString()}`);
  },
  getJobSearchOptions: () => apiRequest('/api/jobs/search-options'),
  adminCreateJob: (payload) => apiRequest('/api/jobs/admin', { method: 'POST', body: JSON.stringify(payload) }),
  getAdminJobs: (filters = {}) => {
    const params = new URLSearchParams({
      pageNumber: String(filters.pageNumber || 1),
      pageSize: String(filters.pageSize || 50),
      moderationState: filters.moderationState || 'active',
    });
    if (filters.q) params.set('q', filters.q);
    if (filters.category) params.set('category', filters.category);
    if (filters.department) params.set('department', filters.department);
    if (filters.engagementType) params.set('engagementType', filters.engagementType);
    if (filters.location) params.set('location', filters.location);
    return apiRequest(`/api/jobs/admin?${params.toString()}`);
  },
  getAdminJob: (id) => apiRequest(`/api/jobs/admin/${id}`),
  changeAdminJobStatus: (id, payload) => apiRequest(`/api/jobs/admin/${id}/status`, { method: 'POST', body: JSON.stringify(payload) }),
  restoreAdminJob: (id) => apiRequest(`/api/jobs/admin/${id}/restore`, { method: 'POST' }),
  getDeclarations: () => apiRequest('/api/admin/configuration/declarations'),
  createDeclaration: (payload) => apiRequest('/api/admin/configuration/declarations', { method: 'POST', body: JSON.stringify(payload) }),
  updateDeclaration: (id, payload) => apiRequest(`/api/admin/configuration/declarations/${id}`, { method: 'PUT', body: JSON.stringify(payload) }),
  getSocialOverview: () => apiRequest('/api/admin/social/overview'),
  getAdminSocialChannels: (includeInactive = true) => apiRequest(`/api/admin/social/channels?includeInactive=${includeInactive}`),
  updateAdminSocialChannel: (idOrSlug, payload) => apiRequest(`/api/admin/social/channels/${encodeURIComponent(idOrSlug)}`, { method: 'PUT', body: JSON.stringify(payload) }),
  getAdminSocialPosts: ({ channelSlug = '', status = '', q = '', pageNumber = 1, pageSize = 25 } = {}) => {
    const params = new URLSearchParams({ pageNumber: String(pageNumber), pageSize: String(pageSize) });
    if (channelSlug) params.set('channelSlug', channelSlug);
    if (status) params.set('status', status);
    if (q) params.set('q', q);
    return apiRequest(`/api/admin/social/posts?${params.toString()}`);
  },
  getAdminSocialPostDetails: (postId) => apiRequest(`/api/admin/social/posts/${encodeURIComponent(postId)}`),
  getAdminSocialProfiles: ({ q = '', role = '', pageNumber = 1, pageSize = 25 } = {}) => {
    const params = new URLSearchParams({ pageNumber: String(pageNumber), pageSize: String(pageSize) });
    if (q) params.set('q', q);
    if (role) params.set('role', role);
    return apiRequest(`/api/admin/social/profiles?${params.toString()}`);
  },
  getAdminSocialProfileDetails: (profileIdOrUserIdOrUsername) => apiRequest(`/api/admin/social/profiles/${encodeURIComponent(profileIdOrUserIdOrUsername)}`),
  getAdminSocialConversations: ({ status = '', pageNumber = 1, pageSize = 25 } = {}) => {
    const params = new URLSearchParams({ pageNumber: String(pageNumber), pageSize: String(pageSize) });
    if (status) params.set('status', status);
    return apiRequest(`/api/admin/social/conversations?${params.toString()}`);
  },
  getAdminSocialConversationMessages: (conversationId) => apiRequest(`/api/admin/social/conversations/${encodeURIComponent(conversationId)}/messages`),
  searchSocialPeople: ({ q = '', role = '' } = {}) => {
    const params = new URLSearchParams();
    if (q) params.set('q', q);
    if (role) params.set('role', role);
    return apiRequest(`/api/social/people/search?${params.toString()}`);
  },
  startSocialConversation: (payload) => apiRequest('/api/social/conversations', { method: 'POST', body: JSON.stringify(payload) }),
  getSocialReports: (status = '') => apiRequest(`/api/admin/social/reports${status ? `?status=${encodeURIComponent(status)}` : ''}`),
  updateSocialReport: (id, payload) => apiRequest(`/api/admin/social/reports/${id}`, { method: 'PUT', body: JSON.stringify(payload) }),
  moderateSocialPost: (id, payload) => apiRequest(`/api/admin/social/posts/${id}/moderation`, { method: 'PUT', body: JSON.stringify(payload) }),
  moderateSocialComment: (id, payload) => apiRequest(`/api/admin/social/comments/${id}/moderation`, { method: 'PUT', body: JSON.stringify(payload) }),
  getFeature: (featureKey) => apiRequest(`/api/admin/features/${featureKey}`),
  updateFeature: (featureKey, payload) => apiRequest(`/api/admin/features/${featureKey}`, { method: 'PUT', body: JSON.stringify(payload) }),
};
