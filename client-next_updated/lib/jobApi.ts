import apiClient from './apiClient';
import { getApiBaseUrl } from './runtimeConfig';
import type { EmployerApplicant, Job, JobApplication, JobListResponse, JobPoster } from '../types';

const JOBS_API = '/api/jobs';

export const jobApi = {
  listJobs: (tenantId?: string, pageNumber = 1, pageSize = 12, filters: {
    q?: string;
    category?: string;
    department?: string;
    engagementType?: string;
    location?: string;
    requireVerifiedProfessional?: string;
    salaryMin?: string;
    salaryMax?: string;
  } = {}) => {
    const params = new URLSearchParams({ pageNumber: String(pageNumber), pageSize: String(pageSize) });
    if (tenantId) params.set('tenantId', tenantId);
    if (filters.q) params.set('q', filters.q);
    if (filters.category) params.set('category', filters.category);
    if (filters.department) params.set('department', filters.department);
    if (filters.engagementType) params.set('engagementType', filters.engagementType);
    if (filters.location) params.set('location', filters.location);
    if (filters.requireVerifiedProfessional) params.set('requireVerifiedProfessional', filters.requireVerifiedProfessional);
    if (filters.salaryMin) params.set('salaryMin', filters.salaryMin);
    if (filters.salaryMax) params.set('salaryMax', filters.salaryMax);
    return apiClient.get<JobListResponse>(`${JOBS_API}?${params.toString()}`);
  },
  getSearchOptions: () => apiClient.get<{
    categories: Array<{ name: string; slug: string }>;
    locations: string[];
    departments: string[];
    engagementTypes: Array<{ id: string; name: string; slug: string; description: string; allowsShiftPattern: boolean; isActive: boolean; displayOrder: number }>;
    metrics: { totalPublishedJobs: number; verifiedRequiredJobs: number; locationCount: number; categoryCount: number };
  }>(`${JOBS_API}/search-options`),
  getMarketplaceMetrics: () => apiClient.get<{ liveJobs: number; employers: number; professionals: number }>(`${JOBS_API}/marketplace-metrics`, { skipAuthRedirect: true }),
  getJob: (jobId: string) => apiClient.get<Job>(`${JOBS_API}/${jobId}`),
  createJob: (data: {
    employerId: string;
    tenantId: string;
    title: string;
    description: string;
    department: string;
    engagementType?: string | null;
    shiftPattern?: string | null;
    location: string;
    salaryMin: number;
    salaryMax: number;
    requiredProfessionalCategory?: string | null;
    minimumYearsOfExperience?: number | null;
    requireVerifiedProfessional: boolean;
    allowInvites: boolean;
    closesAt: string;
    requiredDocuments?: Array<{
      documentType: string;
      isMandatory: boolean;
      verificationMode: string;
      allowAdminOverride: boolean;
    }>;
  }) => apiClient.post<Job>(JOBS_API, data),
  updateJob: (jobId: string, data: {
    tenantId: string;
    title: string;
    description: string;
    department: string;
    engagementType?: string | null;
    shiftPattern?: string | null;
    location: string;
    salaryMin: number;
    salaryMax: number;
    requiredProfessionalCategory?: string | null;
    minimumYearsOfExperience?: number | null;
    requireVerifiedProfessional: boolean;
    allowInvites: boolean;
    closesAt: string;
    requiredDocuments?: Array<{
      documentType: string;
      isMandatory: boolean;
      verificationMode: string;
      allowAdminOverride: boolean;
    }>;
  }) => apiClient.put<Job>(`${JOBS_API}/${jobId}`, data),
  uploadPosters: async (jobId: string, files: File[]) => {
    const form = new FormData();
    files.forEach((file) => form.append('files', file));
    const token = typeof window !== 'undefined' ? localStorage.getItem('accessToken') : null;
    const response = await fetch(`${getApiBaseUrl()}${JOBS_API}/${jobId}/posters`, {
      method: 'POST',
      headers: token ? { Authorization: `Bearer ${token}` } : {},
      body: form,
    });
    if (!response.ok) {
      const payload = await response.json().catch(() => null);
      throw new Error(payload?.errors?.[0] || 'Unable to upload job posters.');
    }
    return response.json() as Promise<JobPoster[]>;
  },
  deletePoster: (jobId: string, posterId: string) => apiClient.delete<void>(`${JOBS_API}/${jobId}/posters/${posterId}`),
  watchJob: (jobId: string) => apiClient.post<{ jobId: string; isWatching: boolean }>(`${JOBS_API}/${jobId}/watch`),
  unwatchJob: (jobId: string) => apiClient.delete<{ jobId: string; isWatching: boolean }>(`${JOBS_API}/${jobId}/watch`),
  getWatchStatus: (jobId: string) => apiClient.get<{ jobId: string; isWatching: boolean }>(`${JOBS_API}/${jobId}/watch`),
  publishJob: (jobId: string, tenantId: string) => apiClient.post<void>(`${JOBS_API}/${jobId}/publish`, { tenantId }),
  changeStatus: (jobId: string, tenantId: string, status: 'Draft' | 'Closed' | 'Cancelled') => apiClient.post(`${JOBS_API}/${jobId}/status`, { tenantId, status }),
  applyForJob: (jobId: string, professionalId: string, tenantId: string) => apiClient.post<void>(`${JOBS_API}/${jobId}/apply`, { professionalId, tenantId }),
  getProfessionalApplications: (professionalId: string) => apiClient.get<JobApplication[]>(`${JOBS_API}/applications/professional/${professionalId}`),
  getApplicationsByJob: (jobId: string, tenantId: string) => apiClient.get<EmployerApplicant[]>(`${JOBS_API}/${jobId}/applications?tenantId=${tenantId}`),
  shortlistCandidate: (applicationId: string, tenantId: string) => apiClient.post<void>(`${JOBS_API}/applications/${applicationId}/shortlist`, { tenantId }),
  getEmployerJobs: (employerId: string, tenantId: string) => apiClient.get<JobListResponse>(`${JOBS_API}/employer/${employerId}?tenantId=${tenantId}`),
  reviewApplicationDocument: (applicationId: string, documentId: string, payload: { tenantId: string; isApproved: boolean; notes?: string }) => apiClient.post(`${JOBS_API}/applications/${applicationId}/documents/${documentId}/review`, payload),
};
