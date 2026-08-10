import apiClient from './apiClient';
import { RequiredDocumentRuleSummary, VerificationRequest } from '../types';

const VERIFICATION_API = '/api/verification';

export const verificationApi = {
  getDocumentTypes: (targetType?: 'Professional' | 'Employer') => {
    const query = targetType ? `?targetType=${targetType === 'Professional' ? 0 : 1}` : '';
    return apiClient.get<Array<{ name: string; slug: string; targetType: string; description?: string; allowedExtensions?: string; maxFileSizeMb?: number }>>(`${VERIFICATION_API}/document-types${query}`);
  },
  getRequiredDocuments: (targetType: 'Professional' | 'Employer', filters?: { category?: string; facilityType?: string }) => {
    const search = new URLSearchParams({ targetType: targetType === 'Professional' ? '0' : '1' });
    if (filters?.category) search.set('category', filters.category);
    if (filters?.facilityType) search.set('facilityType', filters.facilityType);
    return apiClient.get<RequiredDocumentRuleSummary[]>(`${VERIFICATION_API}/required-documents?${search.toString()}`);
  },
  getRequests: async (tenantId: string): Promise<VerificationRequest[]> => {
    return apiClient.get(`${VERIFICATION_API}?tenantId=${tenantId}`);
  },

  getRequest: async (requestId: string): Promise<VerificationRequest> => {
    return apiClient.get(`${VERIFICATION_API}/${requestId}`);
  },

  approveRequest: async (requestId: string, tenantId: string, reviewedBy: string): Promise<void> => {
    return apiClient.post(`${VERIFICATION_API}/${requestId}/approve`, {
      tenantId,
      reviewedBy,
    });
  },

  rejectRequest: async (requestId: string, tenantId: string, reviewedBy: string, reason: string): Promise<void> => {
    return apiClient.post(`${VERIFICATION_API}/${requestId}/reject`, {
      tenantId,
      reviewedBy,
      reason,
    });
  },
};
