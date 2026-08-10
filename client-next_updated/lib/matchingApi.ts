import apiClient from './apiClient';
import type { MatchInvitation, MatchingCandidate } from '../types';

const MATCHING_API = '/api/matching';

export const matchingApi = {
  getCandidates: (jobId: string, tenantId: string) => apiClient.get<MatchingCandidate[]>(`${MATCHING_API}/jobs/${jobId}/candidates?tenantId=${tenantId}`),
  invite: (jobId: string, tenantId: string, professionalId: string, message?: string) =>
    apiClient.post<MatchInvitation>(`${MATCHING_API}/jobs/${jobId}/invite`, { tenantId, professionalId, message }),
  getInvites: (jobId: string, tenantId: string) => apiClient.get<MatchInvitation[]>(`${MATCHING_API}/jobs/${jobId}/invites?tenantId=${tenantId}`),
};
