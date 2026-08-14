import apiClient from './apiClient';
import { getBrowserDeviceId } from './deviceIdentity';
import type { AuthResponse, User } from '../types';

const AUTH_API = '/api/auth';
const USERS_API = '/api/users';

export const authApi = {
  login: async (email: string, password: string) => apiClient.post<AuthResponse>(
    `${AUTH_API}/login`,
    { email, password, deviceId: await getBrowserDeviceId() },
  ),
  register: (email: string, password: string, firstName: string, lastName: string, userType = 'Professional', organizationName = '', businessPhoneNumber = '', acceptedTerms = false, acceptedPrivacyPolicy = false) =>
    apiClient.post<AuthResponse>(`${AUTH_API}/register`, {
      email,
      password,
      firstName,
      lastName,
      phoneNumber: userType === 'Employer' ? businessPhoneNumber : '',
      userType,
      organizationName: userType === 'Employer' ? organizationName : null,
      businessPhoneNumber: userType === 'Employer' ? businessPhoneNumber : null,
      acceptedTerms,
      acceptedPrivacyPolicy,
    }),
  changePassword: (currentPassword: string, newPassword: string, confirmNewPassword: string) =>
    apiClient.post<{ message: string }>(`${AUTH_API}/reset-password`, { currentPassword, newPassword, confirmNewPassword }),
  refresh: async (refreshToken: string) => apiClient.post<{ accessToken: string; refreshToken: string }>(
    `${AUTH_API}/refresh`,
    { refreshToken, deviceId: await getBrowserDeviceId() },
  ),
  logout: (refreshToken: string) => apiClient.post<void>(`${AUTH_API}/logout`, { refreshToken }),
  getCurrentUser: () => apiClient.get<User>(`${USERS_API}/me`),
};
