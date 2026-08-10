import apiClient from './apiClient';
import type { EmployerDocument, EmployerListResponse, EmployerProfile } from '../types';

const EMPLOYERS_API = '/api/employers';

export const employerApi = {
  register: (data: {
    name: string;
    facilityType: string;
    contactEmail: string;
    contactPhone?: string;
    address?: string;
    businessRegistrationNumber?: string;
    kraPin?: string;
    licenseNumber?: string;
  }) => apiClient.post<EmployerProfile>(`${EMPLOYERS_API}/register`, data),
  update: (employerId: string, data: Partial<EmployerProfile>) => apiClient.put<EmployerProfile>(`${EMPLOYERS_API}/${employerId}`, data),
  list: (tenantId?: string, options?: { skipAuthRedirect?: boolean }) => {
    const query = tenantId ? `?tenantId=${tenantId}` : '';
    return apiClient.get<EmployerListResponse>(`${EMPLOYERS_API}${query}`, options);
  },
  getCurrent: () => apiClient.get<EmployerProfile>(`${EMPLOYERS_API}/me`),
  getByEmail: (email: string) => apiClient.get<EmployerProfile>(`${EMPLOYERS_API}/by-email?email=${encodeURIComponent(email)}`),
  uploadDocument: async (employerId: string, documentType: string, file: File) => {
    const formData = new FormData();
    formData.append('documentType', documentType);
    formData.append('file', file);
    return apiClient.post<void>(`${EMPLOYERS_API}/${employerId}/documents`, formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    });
  },
  getDocuments: (employerId: string) => apiClient.get<EmployerDocument[]>(`${EMPLOYERS_API}/${employerId}/documents`),
  getTeam: (employerId: string) => apiClient.get<any[]>(`${EMPLOYERS_API}/${employerId}/team`),
  addTeamMember: (employerId: string, payload: any) => apiClient.post(`${EMPLOYERS_API}/${employerId}/team`, payload),
  updateTeamMember: (employerId: string, memberId: string, payload: any) => apiClient.put(`${EMPLOYERS_API}/${employerId}/team/${memberId}`, payload),
};
