using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Employer.Domain;
using Communication.Domain;
using Identity.Domain;
using Job.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Professional.Domain;
using Verification.Domain;
using Identity.Infrastructure;
using Shared.Tenant;

namespace Shared.Data
{
    public static class DbContextFactory
    {
        public static (DatabaseProvider Provider, string ConnectionString) GetCentralizedDatabaseConfig(IConfiguration config)
        {
            var provider = Enum.Parse<DatabaseProvider>(config["Database:Provider"] ?? "SqlServer");
            var connStr = config.GetConnectionString(provider.ToString()) ?? throw new InvalidOperationException($"Connection string for provider '{provider}' not found in configuration.");
            return (provider, connStr);
        }

        public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration config, DatabaseProvider provider, string connectionString, Action<DbContextOptionsBuilder>? extra = null)
        {
            services.AddSingleton(typeof(DatabaseProvider), provider);
            return AddDatabase<ApplicationDbContext>(services, provider, connectionString, extra: extra);
        }

        public static IServiceCollection AddDatabase<TContext>(this IServiceCollection services, DatabaseProvider provider, string connectionString, Action<DbContextOptionsBuilder>? extra = null)
            where TContext : DbContext
        {
            services.AddScoped<Shared.Audit.AuditInterceptor>();
            services.AddDbContext<TContext>((sp, options) =>
            {
                var interceptor = sp.GetService<Shared.Audit.AuditInterceptor>();
                if (interceptor != null)
                    options.AddInterceptors(interceptor);

                extra?.Invoke(options);
                switch (provider)
                {
                    case DatabaseProvider.SqlServer:
                        options.UseSqlServer(connectionString);
                        break;
                    case DatabaseProvider.Sqlite:
                        options.UseSqlite(connectionString);
                        break;
                    case DatabaseProvider.InMemory:
                        options.UseInMemoryDatabase("TestDb");
                        break;
                    default:
                        throw new NotSupportedException($"Provider {provider} not available in the current build.");
                }
            });

            services.AddScoped<IDbConnectionFactory, DbConnectionFactory>();
            return services;
        }

        public static IServiceCollection AddAllDatabases(this IServiceCollection services, DatabaseProvider provider, string connectionString)
        {
            services.AddSingleton(typeof(DatabaseProvider), provider);

            try
            {
                var loaded = AppDomain.CurrentDomain.GetAssemblies().Where(a => !string.IsNullOrEmpty(a.Location)).Select(a => a.Location).ToHashSet(StringComparer.OrdinalIgnoreCase);
                var baseDir = AppContext.BaseDirectory;
                if (Directory.Exists(baseDir))
                {
                    foreach (var dll in Directory.GetFiles(baseDir, "*.dll", SearchOption.TopDirectoryOnly))
                    {
                        if (!loaded.Contains(dll))
                        {
                            try { Assembly.LoadFrom(dll); } catch { }
                        }
                    }
                }
            }
            catch { }

            var assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(a => !a.IsDynamic).ToArray();
            var ctxTypes = assemblies.SelectMany(a => { try { return a.GetTypes(); } catch { return Array.Empty<Type>(); } })
                .Where(t => typeof(DbContext).IsAssignableFrom(t) && !t.IsAbstract)
                .Distinct()
                .ToList();

            var filteredCtxTypes = ctxTypes.Where(t =>
            {
                try
                {
                    var optionsType = typeof(DbContextOptions<>).MakeGenericType(t);
                    var ctors = t.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
                    return ctors.Any(c => c.GetParameters().Any(p => p.ParameterType == optionsType));
                }
                catch
                {
                    return false;
                }
            }).ToList();

            foreach (var ctxType in filteredCtxTypes)
            {
                RegisterContext(services, ctxType, provider, connectionString);
            }

            return services;
        }

        private static void RegisterContext(IServiceCollection services, Type ctxType, DatabaseProvider provider, string connectionString)
        {
            var addDbGeneric = typeof(DbContextFactory)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(m => m.Name == "AddDatabase" && m.IsGenericMethodDefinition && m.GetGenericArguments().Length == 1 && m.GetParameters().Length >= 3 && m.GetParameters()[0].ParameterType == typeof(IServiceCollection) && m.GetParameters()[1].ParameterType == typeof(DatabaseProvider) && m.GetParameters()[2].ParameterType == typeof(string))
                .FirstOrDefault();

            if (addDbGeneric != null)
            {
                var method = addDbGeneric.MakeGenericMethod(ctxType);
                var paramCount = method.GetParameters().Length;
                var args = new object[paramCount];
                args[0] = services;
                args[1] = provider;
                args[2] = connectionString;
                for (int i = 3; i < paramCount; i++)
                {
                    args[i] = null!;
                }

                method.Invoke(null, args);
            }
        }

        public static async System.Threading.Tasks.Task MigrateAllContextsAsync(this IServiceProvider provider)
        {
            using var scope = provider.CreateScope();
            var sp = scope.ServiceProvider;
            var assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(a => !a.IsDynamic);
            var ctxTypes = assemblies.SelectMany(a => { try { return a.GetTypes(); } catch { return Array.Empty<Type>(); } })
                .Where(t => typeof(DbContext).IsAssignableFrom(t) && !t.IsAbstract)
                .Distinct()
                .ToList();

            foreach (var ctxType in ctxTypes)
            {
                var ctx = sp.GetService(ctxType) as DbContext;
                if (ctx != null)
                {
                    await ctx.Database.MigrateAsync();
                }
            }
        }

        public static async System.Threading.Tasks.Task SeedDefaultDataAsync(this IServiceProvider provider)
        {
            using var scope = provider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

            var defaultTenant = await dbContext.Tenants.FirstOrDefaultAsync(t => t.Id == PlatformTenant.Id || t.Slug == PlatformTenant.Slug);
            if (defaultTenant == null)
            {
                defaultTenant = new Identity.Domain.Tenant
                {
                    Id = PlatformTenant.Id,
                    Name = PlatformTenant.Name,
                    Slug = PlatformTenant.Slug,
                    SubscriptionTier = "free",
                    Status = Identity.Domain.TenantStatus.Active,
                    CreatedAt = DateTime.UtcNow,
                    RegionCode = "KE",
                    MaxProfessionals = 500,
                    MaxEmployers = 200
                };
                await dbContext.Tenants.AddAsync(defaultTenant);
                await dbContext.SaveChangesAsync();
            }
            else if (defaultTenant.Id != PlatformTenant.Id)
            {
                defaultTenant.Id = PlatformTenant.Id;
                defaultTenant.Slug = PlatformTenant.Slug;
                defaultTenant.Name = string.IsNullOrWhiteSpace(defaultTenant.Name) ? PlatformTenant.Name : defaultTenant.Name;
                await dbContext.SaveChangesAsync();
            }

            await dbContext.Users.Where(u => u.TenantId == Guid.Empty).ExecuteUpdateAsync(setters => setters.SetProperty(u => u.TenantId, PlatformTenant.Id));
            await dbContext.EmployerProfiles.Where(e => e.TenantId == Guid.Empty).ExecuteUpdateAsync(setters => setters.SetProperty(e => e.TenantId, PlatformTenant.Id));
            await dbContext.EmployerDocuments.Where(d => d.TenantId == Guid.Empty).ExecuteUpdateAsync(setters => setters.SetProperty(d => d.TenantId, PlatformTenant.Id));
            await dbContext.ProfessionalProfiles.Where(p => p.TenantId == Guid.Empty).ExecuteUpdateAsync(setters => setters.SetProperty(p => p.TenantId, PlatformTenant.Id));
            await dbContext.Documents.Where(d => d.TenantId == Guid.Empty).ExecuteUpdateAsync(setters => setters.SetProperty(d => d.TenantId, PlatformTenant.Id));
            await dbContext.VerificationRequests.Where(v => v.TenantId == Guid.Empty).ExecuteUpdateAsync(setters => setters.SetProperty(v => v.TenantId, PlatformTenant.Id));

            if (!await dbContext.PasswordPolicies.AnyAsync(p => p.Id == PasswordPolicyConfig.DefaultId))
            {
                dbContext.PasswordPolicies.Add(PasswordPolicyConfig.Default());
            }

            var adminEmail = configuration["DefaultAdmin:Email"] ?? "admin@medicsocial.local";
            var adminPassword = configuration["DefaultAdmin:Password"] ?? "Admin@12345";
            var resetDefaultAdmin = configuration.GetValue<bool>("DefaultAdmin:ResetOnStartup");
            var adminUser = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == adminEmail);
            if (adminUser == null)
            {
                adminUser = new User
                {
                    Id = new Guid("00000000-0000-0000-0000-000000000002"),
                    TenantId = defaultTenant.Id,
                    Email = adminEmail,
                    PasswordHash = PasswordHasher.Hash(adminPassword),
                    FirstName = "Admin",
                    LastName = "User",
                    PhoneNumber = "+254700000000",
                    UserType = UserType.SuperAdmin,
                    Status = UserStatus.Active,
                    VerificationStatus = "Verified",
                    SubscriptionTier = "free",
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };
                await dbContext.Users.AddAsync(adminUser);
            }
            else if (resetDefaultAdmin || PasswordHasher.NeedsRehash(adminUser.PasswordHash))
            {
                adminUser.PasswordHash = PasswordHasher.Hash(adminPassword);
                adminUser.IsActive = true;
                adminUser.Status = UserStatus.Active;
                adminUser.UserType = UserType.SuperAdmin;
                adminUser.SubscriptionTier = string.IsNullOrWhiteSpace(adminUser.SubscriptionTier) ? "free" : adminUser.SubscriptionTier;
                adminUser.VerificationStatus = string.IsNullOrWhiteSpace(adminUser.VerificationStatus) ? "Verified" : adminUser.VerificationStatus;
            }

            var categories = new[]
            {
                ("Pharmacist", "pharmacist"),
                ("Pharmtech", "pharmtech"),
                ("Clinical Officer", "clinical-officer"),
                ("Nurse", "nurse"),
                ("Nurse Assistant", "nurse-assistant"),
                ("Dentist", "dentist"),
                ("Medical Officer", "medical-officer"),
                ("Lab Technician", "lab-technician"),
                ("Physiotherapist", "physiotherapist"),
                ("Care Giver", "care-giver"),
                ("Other", "other")
            };

            foreach (var (name, slug) in categories)
            {
                if (!await dbContext.ProfessionalCategories.AnyAsync(c => c.Slug == slug))
                {
                    dbContext.ProfessionalCategories.Add(new ProfessionalCategory { Id = Guid.NewGuid(), Name = name, Slug = slug, IsActive = true, CreatedAt = DateTime.UtcNow });
                }
            }

            foreach (var (name, slug, description, allowsShift, order) in new[]
            {
                ("Permanent", "permanent", "Ongoing role with no fixed end date.", false, 0),
                ("Temporary", "temporary", "Time-bound cover, relief, or interim role.", false, 1),
                ("Contract", "contract", "Fixed-term contract engagement.", false, 2),
                ("Shift driven", "shift-driven", "Roster, shift, locum, or session-based work.", true, 3),
                ("Part time", "part-time", "Reduced-hours or recurring part-time role.", true, 4)
            })
            {
                if (!await dbContext.JobEngagementTypes.AnyAsync(item => item.Slug == slug))
                {
                    dbContext.JobEngagementTypes.Add(new JobEngagementType { Id = Guid.NewGuid(), Name = name, Slug = slug, Description = description, AllowsShiftPattern = allowsShift, IsActive = true, DisplayOrder = order, CreatedAt = DateTime.UtcNow });
                }
            }

            foreach (var (name, slug, target, description) in new[]
            {
                ("Professional License", "professional-license", DocumentTargetType.Professional, "Current professional practice license"),
                ("National ID", "national-id", DocumentTargetType.Professional, "National identity document"),
                ("Academic Certificate", "academic-certificate", DocumentTargetType.Professional, "Education or qualification proof"),
                ("Tax Certificate", "tax-certificate", DocumentTargetType.Employer, "Employer tax compliance certificate"),
                ("Facility License", "facility-license", DocumentTargetType.Employer, "Facility operating license"),
                ("Business Registration", "business-registration", DocumentTargetType.Employer, "Business or organization registration certificate")
            })
            {
                if (!await dbContext.DocumentTypes.AnyAsync(d => d.Slug == slug))
                {
                    dbContext.DocumentTypes.Add(new DocumentTypeCatalog { Id = Guid.NewGuid(), Name = name, Slug = slug, TargetType = target, Description = description, IsActive = true, CreatedAt = DateTime.UtcNow });
                }
            }

            if (!await dbContext.SubscriptionPlans.AnyAsync(p => p.Slug == "free"))
            {
                dbContext.SubscriptionPlans.Add(new SubscriptionPlan
                {
                    Id = Guid.NewGuid(),
                    Name = "Free",
                    Slug = "free",
                    Description = "Entry bundle for a single active opening with basic employer onboarding.",
                    PriceAmount = 0,
                    Currency = "USD",
                    BillingInterval = "Monthly",
                    MaxPublishedJobs = 1,
                    MaxTeamMembers = 1,
                    MaxCandidateInvitesPerPeriod = 0,
                    MaxMessagesPerPeriod = 0,
                    CanAccessJobPostingModule = true,
                    CanAccessApplicantReviewModule = true,
                    CanAccessTalentSearchModule = false,
                    CanAccessReportsModule = false,
                    CanAccessCommunicationsModule = false,
                    CanViewProfessionalProfiles = true,
                    CanViewProfessionalContactDetails = false,
                    CanViewProfessionalDocuments = false,
                    CanViewProfessionalVerificationStatus = true,
                    CanInviteCandidates = false,
                    CanMessageCandidates = false,
                    CanUseEmailCommunications = false,
                    CanUseSmsCommunications = false,
                    CanUseWhatsAppCommunications = false,
                    RequiresEmployerVerificationToPublishJobs = true,
                    IsDefault = true,
                    CreatedAt = DateTime.UtcNow
                });
            }

            if (!await dbContext.SubscriptionPlans.AnyAsync(p => p.Slug == "growth"))
            {
                dbContext.SubscriptionPlans.Add(new SubscriptionPlan
                {
                    Id = Guid.NewGuid(),
                    Name = "Growth",
                    Slug = "growth",
                    Description = "Expanded hiring bundle with outreach, talent search, and higher publishing capacity.",
                    PriceAmount = 149,
                    Currency = "USD",
                    BillingInterval = "Monthly",
                    MaxPublishedJobs = 10,
                    MaxTeamMembers = 10,
                    MaxCandidateInvitesPerPeriod = 100,
                    MaxMessagesPerPeriod = 1000,
                    CanAccessJobPostingModule = true,
                    CanAccessApplicantReviewModule = true,
                    CanAccessTalentSearchModule = true,
                    CanAccessReportsModule = true,
                    CanAccessCommunicationsModule = true,
                    CanViewProfessionalProfiles = true,
                    CanViewProfessionalContactDetails = true,
                    CanViewProfessionalDocuments = true,
                    CanViewProfessionalVerificationStatus = true,
                    CanInviteCandidates = true,
                    CanMessageCandidates = true,
                    CanUseEmailCommunications = true,
                    CanUseSmsCommunications = true,
                    CanUseWhatsAppCommunications = true,
                    RequiresEmployerVerificationToPublishJobs = true,
                    IsDefault = false,
                    CreatedAt = DateTime.UtcNow
                });
            }

            if (!await dbContext.RequiredDocumentRules.AnyAsync())
            {
                dbContext.RequiredDocumentRules.AddRange(
                    new RequiredDocumentRule { Id = Guid.NewGuid(), TargetType = DocumentTargetType.Professional, AppliesToCategoryOrFacilityType = null, DocumentType = "License", IsMandatory = true, CreatedAt = DateTime.UtcNow },
                    new RequiredDocumentRule { Id = Guid.NewGuid(), TargetType = DocumentTargetType.Professional, AppliesToCategoryOrFacilityType = null, DocumentType = "NationalId", IsMandatory = true, CreatedAt = DateTime.UtcNow },
                    new RequiredDocumentRule { Id = Guid.NewGuid(), TargetType = DocumentTargetType.Employer, AppliesToCategoryOrFacilityType = null, DocumentType = "TaxCertificate", IsMandatory = true, CreatedAt = DateTime.UtcNow },
                    new RequiredDocumentRule { Id = Guid.NewGuid(), TargetType = DocumentTargetType.Employer, AppliesToCategoryOrFacilityType = null, DocumentType = "FacilityLicense", IsMandatory = true, CreatedAt = DateTime.UtcNow }
                );
            }

            if (!await dbContext.VerificationPolicies.AnyAsync())
            {
                dbContext.VerificationPolicies.AddRange(
                    new VerificationPolicy { Id = Guid.NewGuid(), Name = "Professional Application Policy", SubjectType = VerificationSubjectType.Professional, Stage = VerificationStage.JobApplication, ActionKey = "ApplyForJob", PolicyMode = VerificationPolicyMode.StatusGate, RequireVerifiedStatusForAction = true, RequireAllMandatoryDocuments = false, BlockOnPending = true, BlockOnFailure = true, BypassWhenIntegrationMissing = true, AllowManualOverride = true, CreatedAt = DateTime.UtcNow },
                    new VerificationPolicy { Id = Guid.NewGuid(), Name = "Professional Application Documents", SubjectType = VerificationSubjectType.Professional, Stage = VerificationStage.JobApplication, ActionKey = "ApplyForJob", PolicyMode = VerificationPolicyMode.MandatoryDocumentsGate, RequireVerifiedStatusForAction = false, RequireAllMandatoryDocuments = true, BlockOnPending = true, BlockOnFailure = true, BypassWhenIntegrationMissing = true, AllowManualOverride = true, CreatedAt = DateTime.UtcNow },
                    new VerificationPolicy { Id = Guid.NewGuid(), Name = "Employer Publishing Policy", SubjectType = VerificationSubjectType.Employer, Stage = VerificationStage.EmployerPublishing, ActionKey = "PublishJob", PolicyMode = VerificationPolicyMode.StatusGate, RequireVerifiedStatusForAction = true, RequireAllMandatoryDocuments = false, BlockOnPending = true, BlockOnFailure = true, BypassWhenIntegrationMissing = true, AllowManualOverride = true, CreatedAt = DateTime.UtcNow },
                    new VerificationPolicy { Id = Guid.NewGuid(), Name = "Employer Publishing Documents", SubjectType = VerificationSubjectType.Employer, Stage = VerificationStage.EmployerPublishing, ActionKey = "PublishJob", PolicyMode = VerificationPolicyMode.MandatoryDocumentsGate, RequireVerifiedStatusForAction = false, RequireAllMandatoryDocuments = true, BlockOnPending = true, BlockOnFailure = true, BypassWhenIntegrationMissing = true, AllowManualOverride = true, CreatedAt = DateTime.UtcNow }
                );
            }

            foreach (var (channel, providerName) in new[]
            {
                (CommunicationChannel.Email, "SMTP"),
                (CommunicationChannel.Sms, "SMS Gateway"),
                (CommunicationChannel.WhatsApp, "WhatsApp Business")
            })
            {
                if (!await dbContext.CommunicationProviderConfigs.AnyAsync(c => c.Channel == channel))
                {
                    dbContext.CommunicationProviderConfigs.Add(new CommunicationProviderConfig
                    {
                        Id = Guid.NewGuid(),
                        Channel = channel,
                        ProviderName = providerName,
                        IsEnabled = false,
                        SimulateWhenDisabled = true,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            await dbContext.SaveChangesAsync();
        }
    }
}
