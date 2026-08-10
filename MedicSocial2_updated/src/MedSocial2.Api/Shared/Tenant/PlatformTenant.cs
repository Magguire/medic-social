using System;

namespace Shared.Tenant
{
    public static class PlatformTenant
    {
        public static readonly Guid Id = new("00000000-0000-0000-0000-000000000001");
        public const string Slug = "default";
        public const string Name = "Default Organization";
    }
}
