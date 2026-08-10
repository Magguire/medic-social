using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.Data;

namespace Shared.Audit
{
    [ApiController]
    [Route("api/admin/dashboard")]
    [Authorize(Roles = "SuperAdmin,TenantAdmin,Auditor")]
    public class AdminDashboardController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        public AdminDashboardController(ApplicationDbContext db) => _db = db;

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            try
            {
                var now = DateTime.UtcNow;
                var recentCutoff = now.AddDays(-30);
                var totalProfessionals = await _db.Users.CountAsync(u => u.UserType == Identity.Domain.UserType.Professional);
                var completedProfessionalProfiles = await _db.ProfessionalProfiles.CountAsync();
                var totalEmployers = await _db.Users.CountAsync(u => u.UserType == Identity.Domain.UserType.Employer);
                var activeJobs = await _db.Jobs.CountAsync(j => j.Status == Job.Domain.JobStatus.Published);
                var newProfessionals = await _db.Users.CountAsync(u =>
                    u.UserType == Identity.Domain.UserType.Professional && u.CreatedAt >= recentCutoff);
                var verifiedEmployers = await _db.EmployerProfiles.CountAsync(e => e.VerificationStatus == "Verified");
                var professionalDocumentsUploaded = await _db.Documents.CountAsync();
                var professionalsWithDocuments = await _db.Documents.Select(document => document.ProfessionalId).Distinct().CountAsync();
                var verifiedProfessionalDocuments = await _db.Documents.CountAsync(document => document.Status == Professional.Domain.DocumentStatus.Verified);
                var totalJobs = await _db.Jobs.CountAsync();
                var draftJobs = await _db.Jobs.CountAsync(job => job.Status == Job.Domain.JobStatus.Draft);
                var closedJobs = await _db.Jobs.CountAsync(job => job.Status == Job.Domain.JobStatus.Closed);
                var totalApplications = await _db.JobApplications.CountAsync();
                var jobsWithApplicants = await _db.JobApplications.Select(application => application.JobId).Distinct().CountAsync();
                var pendingApplications = await _db.JobApplications.CountAsync(application => application.Status == Job.Domain.ApplicationStatus.Submitted);
                var shortlistedApplications = await _db.JobApplications.CountAsync(application => application.IsShortlisted);
                var averageApplicantsPerJob = totalJobs == 0 ? 0 : Math.Round((double)totalApplications / totalJobs, 1);

                var activeCutoff = now.AddMinutes(-15);
                var activeTokenRows = await _db.RefreshTokens
                    .Where(token => !token.RevokedAt.HasValue && token.Expiry > now)
                    .Join(_db.Users, token => token.UserId, user => user.Id, (token, user) => new { token, user })
                    .ToListAsync();
                var logicalSessions = activeTokenRows
                    .GroupBy(row => new { row.token.UserId, row.token.DeviceId })
                    .Select(group => group.OrderByDescending(row => row.token.LastSeenAt).First())
                    .Where(row => row.token.LastSeenAt >= activeCutoff)
                    .ToList();
                var activeSessions = logicalSessions.Count;
                var activeUsers = logicalSessions.Select(row => row.user.Id).Distinct().Count();
                var sessionRoles = logicalSessions.GroupBy(row => row.user.UserType).Select(group => new { role = group.Key.ToString(), users = group.Select(row => row.user.Id).Distinct().Count(), sessions = group.Count() }).ToList();
                var activeSessionsList = logicalSessions
                    .OrderByDescending(row => row.token.LastSeenAt)
                    .Take(12)
                    .Select(row => new
                    {
                        sessionId = row.token.Id,
                        userId = row.user.Id,
                        row.user.Email,
                        fullName = (row.user.FirstName + " " + row.user.LastName).Trim(),
                        role = row.user.UserType.ToString(),
                        row.token.DeviceId,
                        row.token.Ip,
                        row.token.UserAgent,
                        row.token.CreatedAt,
                        row.token.LastSeenAt,
                        row.token.Expiry
                    })
                    .ToList();
                var recentSessions = await _db.RefreshTokens
                    .OrderByDescending(token => token.LastSeenAt)
                    .Take(12)
                    .Join(_db.Users, token => token.UserId, user => user.Id, (token, user) => new
                    {
                        sessionId = token.Id,
                        userId = user.Id,
                        user.Email,
                        fullName = (user.FirstName + " " + user.LastName).Trim(),
                        role = user.UserType.ToString(),
                        token.DeviceId,
                        token.Ip,
                        token.UserAgent,
                        token.CreatedAt,
                        token.LastSeenAt,
                        token.Expiry,
                        token.RevokedAt
                    })
                    .ToListAsync();

                var verificationRequests = await _db.VerificationRequests
                    .OrderByDescending(v => v.CreatedAt)
                    .Take(8)
                    .Select(v => new { v.Id, v.SubjectType, v.SubjectId, v.Status, v.CreatedAt, v.ReviewedAt, v.Notes })
                    .ToListAsync();
                var recentActivity = await _db.AuditLog
                    .OrderByDescending(a => a.Timestamp)
                    .Take(10)
                    .Select(a => new { a.Id, a.Action, a.EntityName, a.EntityId, a.Timestamp, a.UserId })
                    .ToListAsync();

                return Ok(new
                {
                    stats = new
                    {
                        totalProfessionals,
                        completedProfessionalProfiles,
                        incompleteProfessionalProfiles = Math.Max(0, totalProfessionals - completedProfessionalProfiles),
                        professionalsWithDocuments,
                        professionalsWithoutDocuments = Math.Max(0, completedProfessionalProfiles - professionalsWithDocuments),
                        professionalDocumentsUploaded,
                        verifiedProfessionalDocuments,
                        totalEmployers,
                        activeJobs,
                        totalJobs,
                        draftJobs,
                        closedJobs,
                        jobsWithApplicants,
                        jobsWithoutApplicants = Math.Max(0, totalJobs - jobsWithApplicants),
                        totalApplications,
                        pendingApplications,
                        shortlistedApplications,
                        averageApplicantsPerJob,
                        newProfessionals,
                        verifiedEmployers,
                        activeSessions,
                        activeUsers
                    },
                    sessionMetrics = new
                    {
                        activeSessions,
                        activeUsers,
                        byRole = sessionRoles,
                        activeSessionsList,
                        recentSessions
                    },
                    verificationRequests,
                    recentActivity
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { errors = new[] { ex.Message } });
            }
        }
    }
}
