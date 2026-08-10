using System;

namespace Verification.Domain
{
    /// <summary>
    /// Test entity to demonstrate automatic migration generation via PowerShell script.
    /// This entity will trigger a new migration when the script runs.
    /// </summary>
    public class TestFeature
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public int Priority { get; set; }
    }
}
