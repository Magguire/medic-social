using System.Reflection;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using Communication.Application;
using Employer.Application;
using Employer.Infrastructure;
using Identity.Infrastructure;
using Job.Application;
using Job.Infrastructure;
using MediatR;
using MedSocial2.Api.Modules.Social.Application;
using MedSocial2.Api.Modules.Social.Hubs;
using MedSocial2.Api.Modules.Social.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Professional.Infrastructure;
using Professional.Infrastructure.Storage;
using Shared.Audit;
using Shared.Auth;
using Shared.Data;
using Shared.Features;
using Shared.Notifications;
using Shared.Security;
using Shared.Tenant;
using Verification.Domain;
using Verification.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("sharedsettings.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSignalR();
builder.Services.AddScoped<TenantContext>();
builder.Services.AddHealthChecks();

builder.Services.AddCors(options =>
{
    options.AddPolicy("Default", policy =>
        policy.SetIsOriginAllowed(_ => true)
              .AllowCredentials()
              .AllowAnyHeader()
              .AllowAnyMethod());
});

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
builder.Services.Configure<VerificationIntegrationOptions>(builder.Configuration.GetSection("VerificationIntegration"));
builder.Services.AddSingleton(sp => sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<JwtOptions>>().Value);
builder.Services.AddScoped<IRefreshTokenStore, DatabaseRefreshTokenStore>();
builder.Services.AddScoped<IJwtService, JwtService>();

var jwtOptions = builder.Configuration.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();
var signingKey = Encoding.UTF8.GetBytes(jwtOptions.SigningKey);

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(signingKey),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrWhiteSpace(accessToken) && path.StartsWithSegments("/hubs/social"))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            },
            OnTokenValidated = async context =>
            {
                var userIdValue = context.Principal?.FindFirst("UserId")?.Value;
                var issuedAtValue = context.Principal?.FindFirst(JwtRegisteredClaimNames.Iat)?.Value;
                if (!Guid.TryParse(userIdValue, out var userId))
                {
                    context.Fail("User identity is missing.");
                    return;
                }

                var db = context.HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();
                var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(item => item.Id == userId);
                if (user == null || !user.IsActive)
                {
                    context.Fail("This account is no longer active.");
                    return;
                }

                if (user.SessionsInvalidatedAt.HasValue &&
                    long.TryParse(issuedAtValue, out var issuedAtSeconds) &&
                    DateTimeOffset.FromUnixTimeSeconds(issuedAtSeconds).UtcDateTime <= user.SessionsInvalidatedAt.Value)
                {
                    context.Fail("This session has been ended by an administrator.");
                }
            }
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
builder.Services.AddHttpClient<ICommunicationService, CommunicationService>();
builder.Services.AddScoped<IEmployerAccessService, EmployerAccessService>();
builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();
builder.Services.AddHttpClient<IPaymentService, PaymentService>();
builder.Services.AddScoped<IPlatformFeatureService, PlatformFeatureService>();
builder.Services.Configure<FileSecurityOptions>(builder.Configuration.GetSection("FileSecurity"));
builder.Services.AddSingleton<IFileUploadSecurityService, FileUploadSecurityService>();
builder.Services.Configure<SocialMongoOptions>(builder.Configuration.GetSection("SocialMongo"));
builder.Services.AddSingleton<SocialMongoContext>();
builder.Services.AddScoped<ISocialService, SocialService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddHostedService<JobLifecycleService>();

var (provider, connectionString) = DbContextFactory.GetCentralizedDatabaseConfig(builder.Configuration);
builder.Services.AddAllDatabases(provider, connectionString);

builder.Services.AddScoped(sp => new ProfessionalDbContext(sp.GetRequiredService<DbContextOptions<ApplicationDbContext>>()));
builder.Services.AddScoped(sp => new IdentityDbContext(sp.GetRequiredService<DbContextOptions<ApplicationDbContext>>()));
builder.Services.AddScoped(sp => new EmployerDbContext(sp.GetRequiredService<DbContextOptions<ApplicationDbContext>>()));
builder.Services.AddScoped(sp => new JobDbContext(sp.GetRequiredService<DbContextOptions<ApplicationDbContext>>()));
builder.Services.AddScoped(sp => new VerificationDbContext(sp.GetRequiredService<DbContextOptions<ApplicationDbContext>>()));

builder.Services.Configure<DocumentStorageOptions>(builder.Configuration.GetSection("DocumentStorage"));
builder.Services.AddScoped<IDocumentStorageService>(sp =>
{
    var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<DocumentStorageOptions>>().Value;
    return options.Provider.ToLowerInvariant() switch
    {
        "s3" => ActivatorUtilities.CreateInstance<S3StorageService>(sp),
        "ssh" => ActivatorUtilities.CreateInstance<SshStorageService>(sp),
        _ => ActivatorUtilities.CreateInstance<LocalStorageService>(sp)
    };
});

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "MedSocial2 API",
        Version = "v1",
        Description = "Single-host modular backend for healthcare jobs, employer onboarding, verification, and matching."
    });

    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "JWT Bearer token",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Reference = new OpenApiReference
        {
            Type = ReferenceType.SecurityScheme,
            Id = JwtBearerDefaults.AuthenticationScheme
        }
    };

    options.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, securityScheme);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            securityScheme,
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors("Default");
app.UseStaticFiles();
app.UseMiddleware<TenantMiddleware>();
app.UseMiddleware<RequestAuditMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => Results.Ok(new
{
    name = "MedSocial2.Api",
    mode = "modular-monolith",
    modules = new[] { "Identity", "Professional", "Employer", "Job", "Verification", "Matching", "Social" }
}));
app.MapHealthChecks("/health");
app.MapControllers();
app.MapHub<SocialHub>("/hubs/social");

await app.Services.MigrateAllContextsAsync();
await app.Services.SeedDefaultDataAsync();
await app.Services.GetRequiredService<SocialMongoContext>().EnsureCreatedAsync();

app.Run();
