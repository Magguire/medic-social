namespace Employer.Domain;

public enum EmployerSubscriptionStatus
{
    Pending = 0,
    Active = 1,
    Expired = 2,
    Cancelled = 3,
    Suspended = 4
}

public enum PaymentProviderType
{
    Mpesa = 0,
    PayPal = 1
}

public enum PaymentTransactionStatus
{
    Pending = 0,
    AwaitingCustomerAction = 1,
    PendingAdminReview = 2,
    Successful = 3,
    Failed = 4,
    Cancelled = 5,
    Refunded = 6
}

public class EmployerSubscription
{
    public Guid Id { get; set; }
    public Guid EmployerId { get; set; }
    public Guid TenantId { get; set; }
    public Guid PlanId { get; set; }
    public EmployerSubscriptionStatus Status { get; set; } = EmployerSubscriptionStatus.Pending;
    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }
    public bool AutoRenew { get; set; }
    public string ProvisioningSource { get; set; } = "Payment";
    public Guid? PaymentTransactionId { get; set; }
    public Guid? ApprovedByUserId { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

public class SubscriptionUsage
{
    public Guid Id { get; set; }
    public Guid EmployerSubscriptionId { get; set; }
    public Guid EmployerId { get; set; }
    public Guid TenantId { get; set; }
    public string MetricKey { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public DateTime PeriodStartsAt { get; set; }
    public DateTime PeriodEndsAt { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class PaymentProviderConfig
{
    public Guid Id { get; set; }
    public PaymentProviderType Provider { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public bool IsSandbox { get; set; } = true;
    public string ApiBaseUrl { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string? BusinessShortCode { get; set; }
    public string? PassKey { get; set; }
    public string? ReceiverAccount { get; set; }
    public string? CallbackUrl { get; set; }
    public string CallbackVerificationToken { get; set; } = string.Empty;
    public string Currency { get; set; } = "USD";
    public string PromptFieldsJson { get; set; } = "[]";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

public class PaymentTransaction
{
    public Guid Id { get; set; }
    public Guid EmployerId { get; set; }
    public Guid TenantId { get; set; }
    public Guid PlanId { get; set; }
    public Guid? EmployerSubscriptionId { get; set; }
    public PaymentProviderType? Provider { get; set; }
    public PaymentTransactionStatus Status { get; set; } = PaymentTransactionStatus.Pending;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public string? ExternalReference { get; set; }
    public string? CheckoutReference { get; set; }
    public string PayerDetailsJson { get; set; } = "{}";
    public string? ProviderResponseJson { get; set; }
    public string? FailureReason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
}
