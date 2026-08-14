export interface User {
  id: string;
  tenantId: string;
  email: string;
  firstName: string;
  lastName: string;
  userType: 'Professional' | 'Employer' | 'Admin' | 'SuperAdmin' | string;
  subscriptionTier: string;
  verificationStatus: string;
  mustChangePassword?: boolean;
  createdAt: string;
}

export interface AuthResponse {
  user: User;
  accessToken: string;
  refreshToken: string;
}

export interface Job {
  id: string;
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
  status: string;
  displayStatus?: string;
  publishedAt?: string | null;
  closesAt: string;
  createdAt: string;
  requiredDocuments: JobRequiredDocument[];
  posters: JobPoster[];
}

export interface JobPoster {
  id: string;
  fileName: string;
  contentType: string;
  sizeBytes: number;
  publicUrl: string;
  displayOrder: number;
  createdAt: string;
}

export interface JobRequiredDocument {
  id: string;
  documentType: string;
  isMandatory: boolean;
  verificationMode: 'EmployerReview' | 'PlatformVerification' | string;
  allowAdminOverride: boolean;
}

export interface JobListResponse {
  jobs: Job[];
  totalCount: number;
}

export interface ProfessionalProfile {
  id: string;
  userId: string;
  tenantId: string;
  fullName?: string | null;
  email?: string | null;
  nationality?: string | null;
  phoneNumber?: string | null;
  emailAddress?: string | null;
  nationalIdOrPassport?: string | null;
  addressLine?: string | null;
  city?: string | null;
  county?: string | null;
  postalAddress?: string | null;
  professionalCategory?: string | null;
  specialty?: string | null;
  bio?: string | null;
  licenseNumber?: string | null;
  licenseBoard?: string | null;
  licenseExpiryDate?: string | null;
  yearsOfExperience: number;
  currentPosition?: string | null;
  currentEmployer?: string | null;
  preferredLocation?: string | null;
  relocationWillingness?: number | null;
  expectedSalary?: number | null;
  availabilityType?: string | null;
  skills?: string | null;
  languages?: string | null;
  workPermitStatus?: string | null;
  verificationStatus: string;
}

export interface ProfessionalCategory {
  id: string;
  name: string;
  slug: string;
  isActive: boolean;
}

export interface EducationRecord {
  id: string;
  professionalId: string;
  institution: string;
  award: string;
  fieldOfStudy: string;
  startDate: string;
  endDate?: string | null;
  grade?: string | null;
}

export interface QualificationRecord {
  id: string;
  professionalId: string;
  title: string;
  issuingBody: string;
  licenseNumber?: string | null;
  issuedOn?: string | null;
  expiresOn?: string | null;
}

export interface ExperienceRecord {
  id: string;
  professionalId: string;
  employerName: string;
  jobTitle: string;
  employmentType?: string | null;
  location?: string | null;
  startDate: string;
  endDate?: string | null;
  isCurrentRole: boolean;
  responsibilities?: string | null;
}

export interface ProfessionalDocument {
  id: string;
  fileName: string;
  type: string;
  status: string;
  verificationNotes?: string | null;
  createdAt: string;
}

export interface EmployerApplicantDocument {
  id: string;
  fileName: string;
  documentType: string;
  status: string;
  verificationNotes?: string | null;
  createdAt: string;
  isRequired: boolean;
  verificationMode: string;
}

export interface EmployerApplicant {
  id: string;
  jobId: string;
  professionalId: string;
  tenantId: string;
  status: string;
  isShortlisted: boolean;
  score?: number | null;
  appliedAt: string;
  professionalCategory?: string | null;
  specialty?: string | null;
  yearsOfExperience?: number | null;
  verificationStatus: string;
  requiredDocuments: JobRequiredDocument[];
  documents: EmployerApplicantDocument[];
  missingRequiredDocuments: string[];
}

export interface JobApplication {
  id: string;
  jobId: string;
  professionalId: string;
  tenantId: string;
  status: string;
  isShortlisted: boolean;
  score?: number | null;
  appliedAt: string;
  jobTitle: string;
  jobDepartment: string;
  jobLocation: string;
  jobStatus: string;
  jobClosesAt: string;
}

export interface EmployerProfile {
  id: string;
  tenantId: string;
  name: string;
  organizationSlug: string;
  facilityType: string;
  contactEmail: string;
  contactPhone?: string | null;
  isContactPhonePublic: boolean;
  address?: string | null;
  subscriptionTier: string;
  verificationStatus: string;
  businessRegistrationNumber?: string | null;
  kraPin?: string | null;
  licenseNumber?: string | null;
  createdAt?: string;
  updatedAt?: string | null;
}

export interface EmployerDocument {
  id: string;
  employerId: string;
  documentType: string;
  fileName: string;
  status: string;
  verificationNotes?: string | null;
  createdAt: string;
}

export interface EmployerListResponse {
  items: EmployerProfile[];
  totalCount: number;
}

export interface MatchingCandidate {
  professionalId: string;
  fullName?: string | null;
  professionalCategory?: string | null;
  specialty?: string | null;
  yearsOfExperience: number;
  verificationStatus: string;
  score: number;
}

export interface MatchInvitation {
  id: string;
  jobId: string;
  professionalId: string;
  tenantId: string;
  message?: string | null;
  createdAt: string;
}

export interface AuditItem {
  id: string;
  tenantId?: string | null;
  userId?: string | null;
  action: string;
  entityName?: string | null;
  entityId?: string | null;
  changes?: string | null;
  timestamp: string;
  ipAddress?: string | null;
  userAgent?: string | null;
}
export interface VerificationRequest {
  id: string;
  tenantId?: string | null;
  professionalId?: string | null;
  employerId?: string | null;
  documentId?: string | null;
  documentType?: string | null;
  targetType?: string | null;
  status: string;
  requestedAt: string;
  reviewedAt?: string | null;
  reviewedBy?: string | null;
  reviewNotes?: string | null;
  bypassIntegration?: boolean;
}

export interface RequiredDocumentRuleSummary {
  id: string;
  documentType: string;
  isMandatory: boolean;
  appliesTo?: string | null;
}
