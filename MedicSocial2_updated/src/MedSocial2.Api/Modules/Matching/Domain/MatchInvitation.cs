using System;

namespace Matching.Domain;

public sealed class MatchInvitation
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid JobId { get; set; }
    public Guid ProfessionalId { get; set; }
    public string Status { get; set; } = "Pending";
    public string? Message { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
