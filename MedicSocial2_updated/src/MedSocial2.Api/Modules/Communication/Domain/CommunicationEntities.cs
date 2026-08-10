using System;

namespace Communication.Domain
{
    public enum CommunicationChannel
    {
        Email = 0,
        Sms = 1,
        WhatsApp = 2
    }

    public enum CommunicationMessageStatus
    {
        Queued = 0,
        Sent = 1,
        Failed = 2,
        Simulated = 3
    }

    public class CommunicationProviderConfig
    {
        public Guid Id { get; set; }
        public CommunicationChannel Channel { get; set; }
        public string ProviderName { get; set; } = string.Empty;
        public bool IsEnabled { get; set; }
        public string? BaseUrl { get; set; }
        public string? SenderId { get; set; }
        public string? ApiKeySecret { get; set; }
        public string? AccountSid { get; set; }
        public string? TemplateNamespace { get; set; }
        public bool SimulateWhenDisabled { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }

    public class CommunicationMessage
    {
        public Guid Id { get; set; }
        public Guid? TenantId { get; set; }
        public Guid? UserId { get; set; }
        public CommunicationChannel Channel { get; set; }
        public string Recipient { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public string? TemplateKey { get; set; }
        public string? RelatedEntityName { get; set; }
        public string? RelatedEntityId { get; set; }
        public string ProviderName { get; set; } = string.Empty;
        public CommunicationMessageStatus Status { get; set; }
        public string? ProviderResponse { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? SentAt { get; set; }
    }
}
