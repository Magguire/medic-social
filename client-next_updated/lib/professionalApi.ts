import apiClient from './apiClient';
import type { EducationRecord, ExperienceRecord, ProfessionalCategory, ProfessionalDocument, ProfessionalProfile, QualificationRecord } from '../types';

const PROFESSIONALS_API = '/api/professionals';

export const professionalApi = {
  listProfessionals: (filters?: {
    tenantId?: string;
    search?: string;
    category?: string;
    location?: string;
    specialty?: string;
    minimumYearsOfExperience?: number;
    verificationStatus?: string;
  }, options?: { skipAuthRedirect?: boolean }) => {
    const query = new URLSearchParams();
    Object.entries(filters || {}).forEach(([key, value]) => {
      if (value !== undefined && value !== null && value !== '') query.set(key, String(value));
    });
    return apiClient.get<ProfessionalProfile[]>(`${PROFESSIONALS_API}${query.size ? `?${query}` : ''}`, options);
  },
  getCategories: () => apiClient.get<ProfessionalCategory[]>(`${PROFESSIONALS_API}/categories`),
  getProfile: (professionalId: string) => apiClient.get<ProfessionalProfile>(`${PROFESSIONALS_API}/${professionalId}`),
  getProfileByUser: (userId: string) => apiClient.get<ProfessionalProfile>(`${PROFESSIONALS_API}/by-user/${userId}`),
  register: (data: {
    userId: string;
    nationality: string;
    phoneNumber?: string;
    emailAddress?: string;
    nationalIdOrPassport?: string;
    addressLine?: string;
    city?: string;
    county?: string;
    postalAddress?: string;
    professionalCategory: string;
    licenseNumber: string;
    licenseBoard: string;
    yearsOfExperience: number;
    specialty: string;
  }) => apiClient.post<ProfessionalProfile>(`${PROFESSIONALS_API}/register`, data),
  updateProfile: (professionalId: string, data: {
    nationality?: string;
    phoneNumber?: string;
    emailAddress?: string;
    nationalIdOrPassport?: string;
    addressLine?: string;
    city?: string;
    county?: string;
    postalAddress?: string;
    bio?: string;
    yearsOfExperience?: number;
    currentPosition?: string;
    currentEmployer?: string;
    preferredLocation?: string;
    relocationWillingness?: number;
    expectedSalary?: number;
    availabilityType?: string;
    professionalCategory?: string;
    licenseExpiryDate?: string;
    skills?: string;
    languages?: string;
    workPermitStatus?: string;
    specialty?: string;
  }) => apiClient.put<ProfessionalProfile>(`${PROFESSIONALS_API}/${professionalId}`, data),
  addEducation: (professionalId: string, data: {
    institution: string;
    award: string;
    fieldOfStudy: string;
    startDate: string;
    endDate?: string;
    grade?: string;
  }) => apiClient.post<EducationRecord>(`${PROFESSIONALS_API}/${professionalId}/education`, data),
  getEducation: (professionalId: string) => apiClient.get<EducationRecord[]>(`${PROFESSIONALS_API}/${professionalId}/education`),
  addQualification: (professionalId: string, data: {
    title: string;
    issuingBody: string;
    licenseNumber?: string;
    issuedOn?: string;
    expiresOn?: string;
  }) => apiClient.post<QualificationRecord>(`${PROFESSIONALS_API}/${professionalId}/qualifications`, data),
  getQualifications: (professionalId: string) => apiClient.get<QualificationRecord[]>(`${PROFESSIONALS_API}/${professionalId}/qualifications`),
  addExperience: (professionalId: string, data: {
    employerName: string;
    jobTitle: string;
    employmentType?: string;
    location?: string;
    startDate: string;
    endDate?: string;
    isCurrentRole: boolean;
    responsibilities?: string;
  }) => apiClient.post<ExperienceRecord>(`${PROFESSIONALS_API}/${professionalId}/experience`, data),
  getExperience: (professionalId: string) => apiClient.get<ExperienceRecord[]>(`${PROFESSIONALS_API}/${professionalId}/experience`),
  uploadDocument: async (professionalId: string, documentType: string, file: File) => {
    const formData = new FormData();
    formData.append('documentType', documentType);
    formData.append('file', file);
    return apiClient.post<void>(`${PROFESSIONALS_API}/${professionalId}/documents`, formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    });
  },
  getDocuments: (professionalId: string) => apiClient.get<ProfessionalDocument[]>(`${PROFESSIONALS_API}/${professionalId}/documents`),
};
