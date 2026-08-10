using System;

namespace Professional.Domain
{
    public class EducationRecord
    {
        public Guid Id { get; set; }
        public Guid ProfessionalId { get; set; }
        public string Institution { get; set; } = string.Empty;
        public string Award { get; set; } = string.Empty;
        public string FieldOfStudy { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Grade { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
