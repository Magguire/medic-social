using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.Data;
using Shared.Data.Entities;

namespace Verification.Api.Controllers;

[ApiController]
[Route("api/content-pages")]
public class ContentPagesController : ControllerBase
{
    private static readonly Dictionary<string, string> AllowedDocumentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".pdf"] = "application/pdf",
        [".doc"] = "application/msword",
        [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
    };
    private const long MaxLegalDocumentBytes = 10 * 1024 * 1024;
    private readonly ApplicationDbContext _db;
    private readonly IWebHostEnvironment _environment;

    public ContentPagesController(ApplicationDbContext db, IWebHostEnvironment environment)
    {
        _db = db;
        _environment = environment;
    }

    [AllowAnonymous]
    [HttpGet("{slug}")]
    public async Task<IActionResult> GetPublic(string slug)
    {
        try
        {
            var page = await _db.ContentPages.FirstOrDefaultAsync(item => item.Slug == slug && item.IsPublished, HttpContext.RequestAborted);
            if (page == null)
            {
                return Ok(DefaultPage(slug));
            }

            return Ok(Map(page));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { errors = new[] { ex.Message } });
        }
    }

    [Authorize(Roles = "SuperAdmin,TenantAdmin")]
    [HttpGet("admin")]
    public async Task<IActionResult> AdminList()
    {
        try
        {
            var pages = await _db.ContentPages.OrderBy(item => item.Slug).ToListAsync(HttpContext.RequestAborted);
            return Ok(pages.Select(Map));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { errors = new[] { ex.Message } });
        }
    }

    [Authorize(Roles = "SuperAdmin,TenantAdmin")]
    [HttpPost("admin")]
    public async Task<IActionResult> Save([FromBody] SaveContentPageRequest request)
    {
        try
        {
            var slug = request.Slug.Trim().ToLowerInvariant();
            var page = await _db.ContentPages.FirstOrDefaultAsync(item => item.Slug == slug, HttpContext.RequestAborted);
            if (page == null)
            {
                page = new ContentPage { Id = Guid.NewGuid(), Slug = slug, CreatedAt = DateTime.UtcNow };
                _db.ContentPages.Add(page);
            }

            page.Title = request.Title.Trim();
            page.HtmlContent = request.HtmlContent ?? string.Empty;
            page.CssContent = request.CssContent ?? string.Empty;
            page.SourceType = string.IsNullOrWhiteSpace(request.SourceType) ? "Html" : request.SourceType.Trim();
            if (!string.IsNullOrWhiteSpace(request.DocumentUrl))
            {
                page.DocumentUrl = request.DocumentUrl.Trim();
                page.SourceType = "ExternalDocument";
            }
            page.IsPublished = request.IsPublished;
            page.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(HttpContext.RequestAborted);
            return Ok(Map(page));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { errors = new[] { ex.Message } });
        }
    }

    [Authorize(Roles = "SuperAdmin,TenantAdmin")]
    [HttpPost("admin/{slug}/document")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxLegalDocumentBytes + 1024)]
    public async Task<IActionResult> UploadDocument(string slug, [FromForm] LegalDocumentUploadRequest request)
    {
        try
        {
            var file = request.File;
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { errors = new[] { "A PDF or Word document is required." } });
            }

            if (file.Length > MaxLegalDocumentBytes)
            {
                return BadRequest(new { errors = new[] { "Legal document uploads are limited to 10 MB." } });
            }

            var extension = Path.GetExtension(file.FileName);
            if (!AllowedDocumentTypes.ContainsKey(extension))
            {
                return BadRequest(new { errors = new[] { "Only PDF, DOC, and DOCX legal documents are supported." } });
            }

            var normalizedSlug = slug.Trim().ToLowerInvariant();
            if (normalizedSlug is not ("privacy" or "terms"))
            {
                return BadRequest(new { errors = new[] { "Only privacy and terms pages can receive legal documents." } });
            }

            var page = await _db.ContentPages.FirstOrDefaultAsync(item => item.Slug == normalizedSlug, HttpContext.RequestAborted);
            if (page == null)
            {
                page = new ContentPage
                {
                    Id = Guid.NewGuid(),
                    Slug = normalizedSlug,
                    Title = normalizedSlug == "terms" ? "Terms and Conditions" : "Privacy Policy",
                    CreatedAt = DateTime.UtcNow,
                };
                _db.ContentPages.Add(page);
            }

            var safeName = $"{normalizedSlug}-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
            var webRoot = _environment.WebRootPath;
            if (string.IsNullOrWhiteSpace(webRoot))
            {
                webRoot = Path.Combine(AppContext.BaseDirectory, "wwwroot");
            }

            var targetDirectory = Path.Combine(webRoot, "legal-pages");
            Directory.CreateDirectory(targetDirectory);
            var targetPath = Path.Combine(targetDirectory, safeName);
            await using (var stream = System.IO.File.Create(targetPath))
            {
                await file.CopyToAsync(stream, HttpContext.RequestAborted);
            }

            page.SourceType = "UploadedDocument";
            page.DocumentFileName = file.FileName;
            page.DocumentContentType = AllowedDocumentTypes[extension];
            page.DocumentUrl = $"/legal-pages/{safeName}";
            page.DocumentSizeBytes = file.Length;
            page.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(HttpContext.RequestAborted);

            return Ok(Map(page));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { errors = new[] { ex.Message } });
        }
    }

    private static ContentPageDto Map(ContentPage page)
    {
        return new ContentPageDto(
            page.Id,
            page.Slug,
            page.Title,
            page.HtmlContent,
            page.CssContent,
            page.IsPublished,
            page.UpdatedAt ?? page.CreatedAt,
            page.SourceType ?? "Html",
            page.DocumentFileName,
            page.DocumentContentType,
            page.DocumentUrl,
            page.DocumentSizeBytes);
    }

    private static ContentPageDto DefaultPage(string slug)
    {
        var isTerms = slug.Equals("terms", StringComparison.OrdinalIgnoreCase);
        var title = isTerms ? "Terms and Conditions" : "Privacy Policy";
        var css = """
            .legal-page-shell {
              --ink: #141412;
              --muted: #66645d;
              --cream: #f7f1e7;
              --paper: #fffdf8;
              --moss: #51624f;
              --clay: #8b4a32;
              --line: rgba(20, 20, 18, 0.12);
              color: var(--ink);
              font-family: Inter, ui-sans-serif, system-ui, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif;
            }
            .legal-page-shell * { box-sizing: border-box; }
            .legal-hero {
              position: relative;
              overflow: hidden;
              border-radius: 34px;
              padding: clamp(2rem, 5vw, 5rem);
              background:
                radial-gradient(circle at 76% 18%, rgba(255,255,255,.22), transparent 22%),
                radial-gradient(circle at 8% 84%, rgba(218,176,116,.24), transparent 28%),
                linear-gradient(135deg, #181715 0%, #4d2c24 48%, #1e332f 100%);
              color: #fffdf8;
              box-shadow: 0 34px 90px rgba(38, 25, 18, .22);
            }
            .legal-kicker {
              display: inline-flex;
              align-items: center;
              gap: .5rem;
              border: 1px solid rgba(255,255,255,.18);
              border-radius: 999px;
              padding: .45rem .85rem;
              background: rgba(255,255,255,.08);
              color: rgba(255,253,248,.78);
              font-size: .74rem;
              font-weight: 800;
              letter-spacing: .18em;
              text-transform: uppercase;
            }
            .legal-hero h1 {
              max-width: 860px;
              margin: 1.5rem 0 .9rem;
              font-size: clamp(3rem, 8vw, 7rem);
              line-height: .88;
              letter-spacing: -.07em;
            }
            .legal-hero p {
              max-width: 760px;
              color: rgba(255,253,248,.76);
              font-size: clamp(1rem, 1.8vw, 1.25rem);
              line-height: 1.8;
            }
            .legal-meta {
              display: flex;
              flex-wrap: wrap;
              gap: .8rem;
              margin-top: 1.6rem;
            }
            .legal-meta span {
              border: 1px solid rgba(255,255,255,.16);
              border-radius: 16px;
              padding: .75rem 1rem;
              background: rgba(255,255,255,.08);
              color: rgba(255,253,248,.82);
              font-weight: 700;
            }
            .legal-grid {
              display: grid;
              grid-template-columns: minmax(0, .78fr) minmax(0, 1.22fr);
              gap: clamp(1rem, 3vw, 2rem);
              margin-top: 2rem;
            }
            .legal-nav, .legal-content-card {
              border: 1px solid var(--line);
              border-radius: 30px;
              background: rgba(255,253,248,.86);
              box-shadow: 0 24px 70px rgba(38, 25, 18, .09);
            }
            .legal-nav {
              position: sticky;
              top: 6.5rem;
              align-self: start;
              padding: 1.25rem;
            }
            .legal-nav strong {
              display: block;
              margin-bottom: .75rem;
              font-size: .8rem;
              letter-spacing: .16em;
              text-transform: uppercase;
              color: var(--moss);
            }
            .legal-nav a {
              display: block;
              border-radius: 18px;
              padding: .85rem 1rem;
              color: var(--ink);
              font-weight: 800;
              text-decoration: none;
            }
            .legal-nav a:hover { background: #efe7db; }
            .legal-content-card {
              padding: clamp(1.4rem, 4vw, 3rem);
            }
            .legal-content-card section {
              border-top: 1px solid var(--line);
              padding-top: 1.5rem;
              margin-top: 1.5rem;
            }
            .legal-content-card section:first-child {
              border-top: 0;
              padding-top: 0;
              margin-top: 0;
            }
            .legal-content-card h2 {
              margin: 0 0 .75rem;
              font-size: clamp(1.45rem, 3vw, 2.4rem);
              letter-spacing: -.04em;
            }
            .legal-content-card p, .legal-content-card li {
              color: var(--muted);
              line-height: 1.85;
            }
            .legal-content-card ul {
              display: grid;
              gap: .75rem;
              margin: 1rem 0 0;
              padding-left: 1.25rem;
            }
            .legal-callout {
              border: 1px solid rgba(81,98,79,.22);
              border-radius: 24px;
              margin-top: 1.5rem;
              padding: 1.2rem;
              background: linear-gradient(135deg, rgba(81,98,79,.12), rgba(139,74,50,.10));
            }
            @media (max-width: 860px) {
              .legal-grid { grid-template-columns: 1fr; }
              .legal-nav { position: static; }
            }
            """;
        var html = isTerms ? TermsHtml() : PrivacyHtml();
        return new ContentPageDto(Guid.Empty, slug, title, html, css, true, DateTime.UtcNow, "Html", null, null, null, null);
    }

    private static string PrivacyHtml() => """
        <article class="legal-page-shell">
          <header class="legal-hero">
            <span class="legal-kicker">Privacy and trust</span>
            <h1>Privacy Policy</h1>
            <p>We designed medicSocial to support healthcare hiring with clear data boundaries, practical verification, and respectful account controls. This policy explains the information we collect, why we use it, and the choices available to account holders.</p>
            <div class="legal-meta"><span>Applies to visitors and account holders</span><span>Built for healthcare hiring workflows</span><span>Last reviewed: platform default</span></div>
          </header>
          <div class="legal-grid">
            <aside class="legal-nav">
              <strong>On this page</strong>
              <a href="#privacy-data">Information we collect</a>
              <a href="#privacy-use">How we use information</a>
              <a href="#privacy-sharing">Sharing and access</a>
              <a href="#privacy-retention">Retention and security</a>
              <a href="#privacy-rights">Your choices</a>
            </aside>
            <div class="legal-content-card">
              <section id="privacy-data"><h2>Information we collect</h2><p>We collect account details, profile information, employer facility records, professional credentials, uploaded documents, job applications, communication records, payment activity, device/session details, and audit events needed to operate the platform.</p></section>
              <section id="privacy-use"><h2>How we use information</h2><ul><li>To let professionals browse jobs, complete profiles, upload documents, apply, watch jobs, and participate in community or messaging features.</li><li>To let employers onboard facilities, manage team access, post openings, review applicants, shortlist talent, verify applicant documents, and communicate with candidates.</li><li>To operate account security, support, moderation, billing, legal notices, and service-quality reporting.</li></ul></section>
              <section id="privacy-sharing"><h2>Sharing and access</h2><p>Professional profile and document visibility depends on role, application context, employer permissions, service plan, and applicable verification requirements. Employers see applicant information relevant to jobs they manage. Authorized platform operators may access records for support, compliance, moderation, billing, and verification operations.</p><div class="legal-callout"><strong>Important:</strong> Sensitive uploaded documents should only be used for verification, eligibility, hiring workflow, dispute handling, and legally required administration.</div></section>
              <section id="privacy-retention"><h2>Retention and security</h2><p>We retain records for as long as needed to provide the service, resolve disputes, enforce platform rules, and comply with applicable obligations. Session and device activity may be used to protect accounts and investigate suspicious access.</p></section>
              <section id="privacy-rights"><h2>Your choices</h2><p>Account holders can update profile details, manage notification and account settings, request correction of inaccurate records, and contact platform administrators for support with verification, account access, or data concerns.</p></section>
            </div>
          </div>
        </article>
        """;

    private static string TermsHtml() => """
        <article class="legal-page-shell">
          <header class="legal-hero">
            <span class="legal-kicker">Platform agreement</span>
            <h1>Terms and Conditions</h1>
            <p>These terms describe the baseline rules for using medicSocial as a healthcare employment marketplace, community space, communication workspace, and trust-focused hiring platform.</p>
            <div class="legal-meta"><span>For professionals and employers</span><span>Includes paid and free platform use</span><span>Last reviewed: platform default</span></div>
          </header>
          <div class="legal-grid">
            <aside class="legal-nav">
              <strong>On this page</strong>
              <a href="#terms-accounts">Accounts and eligibility</a>
              <a href="#terms-employers">Employer responsibilities</a>
              <a href="#terms-professionals">Professional responsibilities</a>
              <a href="#terms-billing">Billing and payments</a>
              <a href="#terms-social">Feed and messaging</a>
              <a href="#terms-enforcement">Moderation and enforcement</a>
            </aside>
            <div class="legal-content-card">
              <section id="terms-accounts"><h2>Accounts and eligibility</h2><p>Users are responsible for providing accurate account, contact, profile, and verification information. Admins may restrict access, request supporting evidence, or require profile completion before certain actions are allowed.</p></section>
              <section id="terms-employers"><h2>Employer responsibilities</h2><ul><li>Employers must provide accurate facility, registration, licence, tax, contact, and job-posting information.</li><li>Employer team members must only access applicant details, documents, communications, and candidate discovery features for legitimate hiring activity.</li><li>Employers are responsible for configuring reasonable job requirements and reviewing applicant documents fairly.</li></ul></section>
              <section id="terms-professionals"><h2>Professional responsibilities</h2><p>Professionals should keep biodata, education, qualifications, licences, experience, skills, and uploaded proof documents accurate and current. Applications may be blocked or delayed when required profile or verification steps are incomplete.</p></section>
              <section id="terms-billing"><h2>Billing and payments</h2><p>The platform may support subscriptions, free tiers, administrator-approved billing, and pay-as-you-go charges. Pricing, usage allowances, plan benefits, and payment requirements are shown in the product or provided during upgrade requests.</p><div class="legal-callout"><strong>Billing note:</strong> Some upgrade or pay-as-you-go requests may require manual review before access changes take effect.</div></section>
              <section id="terms-social"><h2>Feed and messaging</h2><p>Registered users may create posts, participate in channels or communities, send connection requests, and message other users when community features are available. Content and conversations must remain professional, lawful, respectful, and relevant to healthcare work.</p></section>
              <section id="terms-enforcement"><h2>Moderation and enforcement</h2><p>The platform may remove or restore jobs, restrict content, review verification records, manage access, end suspicious sessions, and take action where platform rules, trust, safety, or compliance require intervention.</p></section>
            </div>
          </div>
        </article>
        """;
}

public record ContentPageDto(
    Guid Id,
    string Slug,
    string Title,
    string HtmlContent,
    string CssContent,
    bool IsPublished,
    DateTime UpdatedAt,
    string SourceType,
    string? DocumentFileName,
    string? DocumentContentType,
    string? DocumentUrl,
    long? DocumentSizeBytes);
public record SaveContentPageRequest(string Slug, string Title, string? HtmlContent, string? CssContent, bool IsPublished, string? SourceType, string? DocumentUrl);
public class LegalDocumentUploadRequest
{
    public IFormFile? File { get; set; }
}
