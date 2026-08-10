using System;

namespace Verification.Domain
{
    public enum VerificationSubjectType
    {
        Professional = 0,
        Employer = 1
    }

    public enum DocumentTargetType
    {
        Professional = 0,
        Employer = 1
    }

    public enum VerificationStage
    {
        Registration = 0,
        ProfileCompletion = 1,
        JobApplication = 2,
        EmployerPublishing = 3,
        AdminReview = 4
    }

    public enum VerificationPolicyMode
    {
        StatusGate = 0,
        MandatoryDocumentsGate = 1,
        DocumentIntegration = 2,
        FieldIntegration = 3
    }

    public class VerificationPolicy
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public VerificationSubjectType SubjectType { get; set; }
        public VerificationStage Stage { get; set; } = VerificationStage.ProfileCompletion;
        public string ActionKey { get; set; } = string.Empty;
        public VerificationPolicyMode PolicyMode { get; set; } = VerificationPolicyMode.StatusGate;
        public string? DocumentType { get; set; }
        public string? FieldName { get; set; }
        public Guid? IntegrationConfigId { get; set; }
        public bool RequireVerifiedStatusForAction { get; set; } = true;
        public bool RequireAllMandatoryDocuments { get; set; } = true;
        public bool BlockOnPending { get; set; } = true;
        public bool BlockOnFailure { get; set; } = true;
        public bool BypassWhenIntegrationMissing { get; set; } = true;
        public bool AllowManualOverride { get; set; } = true;
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class RequiredDocumentRule
    {
        public Guid Id { get; set; }
        public DocumentTargetType TargetType { get; set; }
        public string? AppliesToCategoryOrFacilityType { get; set; }
        public string DocumentType { get; set; } = string.Empty;
        public bool IsMandatory { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class DocumentTypeCatalog
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public DocumentTargetType TargetType { get; set; }
        public string Description { get; set; } = string.Empty;
        public string AllowedExtensions { get; set; } = ".pdf,.doc,.docx,.jpg,.jpeg,.png";
        public int MaxFileSizeMb { get; set; } = 10;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public enum VerificationTransportMode
    {
        Manual = 0,
        JsonBase64 = 1,
        Multipart = 2,
        BlobReference = 3,
        PublicUrl = 4,
        FieldValue = 5
    }

    public class VerificationIntegrationConfig
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Subject { get; set; } = "Document";
        public string DocumentType { get; set; } = string.Empty;
        public string? FieldName { get; set; }
        public VerificationTransportMode TransportMode { get; set; } = VerificationTransportMode.Manual;
        public string? EndpointUrl { get; set; }
        public string? HttpMethod { get; set; } = "POST";
        public string? ApiKeySecret { get; set; }
        public string? AuthenticationType { get; set; } = "None";
        public string? RequestHeadersJson { get; set; }
        public string? QueryParametersJson { get; set; }
        public string? RequestBodyTemplate { get; set; }
        public string? RequestFieldMapJson { get; set; }
        public string? SuccessConditionsJson { get; set; }
        public string? FailureConditionsJson { get; set; }
        public string? ResponseMapJson { get; set; }
        public int TimeoutSeconds { get; set; } = 30;
        public int RetryCount { get; set; } = 0;
        public int RetryDelaySeconds { get; set; } = 0;
        public bool RetryOnTimeout { get; set; }
        public bool RetryOn5xx { get; set; } = true;
        public bool ParseJsonResponse { get; set; } = true;
        public bool StoreRawRequestResponse { get; set; } = true;
        public bool IsEnabled { get; set; }
        public bool AllowManualOverride { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
