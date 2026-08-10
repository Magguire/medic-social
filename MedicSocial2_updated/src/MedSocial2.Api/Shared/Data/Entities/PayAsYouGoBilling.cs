namespace Shared.Data.Entities;

public enum PayAsYouGoAction
{
    ProfessionalJobView = 0,
    EmployerJobPosting = 1
}

public enum PayAsYouGoChargeStatus
{
    Free = 0,
    PendingPayment = 1,
    PendingAdminReview = 2,
    Paid = 3,
    Waived = 4,
    Failed = 5
}

public class PayAsYouGoRule
{
    public Guid Id { get; set; }
    public PayAsYouGoAction Action { get; set; }
    public bool IsEnabled { get; set; }
    public int FreeUnitsPerPeriod { get; set; }
    public decimal UnitPrice { get; set; }
    public string Currency { get; set; } = "USD";
    public string PeriodKey { get; set; } = "Monthly";
    public bool RequirePaymentBeforeAction { get; set; } = true;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

public class PayAsYouGoCharge
{
    public Guid Id { get; set; }
    public PayAsYouGoAction Action { get; set; }
    public Guid UserId { get; set; }
    public Guid? EmployerId { get; set; }
    public Guid TenantId { get; set; }
    public Guid? RelatedEntityId { get; set; }
    public int Units { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public PayAsYouGoChargeStatus Status { get; set; }
    public string PeriodKey { get; set; } = string.Empty;
    public string PayerDetailsJson { get; set; } = "{}";
    public string? ExternalReference { get; set; }
    public string? CheckoutReference { get; set; }
    public string? ProviderResponseJson { get; set; }
    public string? FailureReason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
}
