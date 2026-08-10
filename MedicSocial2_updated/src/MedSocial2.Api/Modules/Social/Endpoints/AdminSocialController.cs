using MedSocial2.Api.Modules.Social.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedSocial2.Api.Modules.Social.Endpoints;

[ApiController]
[Route("api/admin/social")]
[Authorize(Roles = "SuperAdmin,TenantAdmin")]
public class AdminSocialController : ControllerBase
{
    private readonly ISocialService _social;

    public AdminSocialController(ISocialService social)
    {
        _social = social;
    }

    [HttpGet("overview")]
    public async Task<IActionResult> Overview(CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _social.GetAdminOverviewAsync(cancellationToken));
        }
        catch (Exception ex)
        {
            return BadRequest(new { errors = new[] { ex.Message } });
        }
    }

    [HttpGet("channels")]
    public async Task<IActionResult> Channels([FromQuery] bool includeInactive = true, CancellationToken cancellationToken = default)
    {
        try
        {
            return Ok(await _social.GetAdminChannelsAsync(includeInactive, cancellationToken));
        }
        catch (Exception ex)
        {
            return BadRequest(new { errors = new[] { ex.Message } });
        }
    }

    [HttpPut("channels/{channelIdOrSlug}")]
    public async Task<IActionResult> UpdateChannel(string channelIdOrSlug, [FromBody] UpdateSocialChannelRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var channel = await _social.UpdateChannelAsync(channelIdOrSlug, request, cancellationToken);
            return channel == null ? NotFound(new { errors = new[] { "Channel not found." } }) : Ok(channel);
        }
        catch (Exception ex)
        {
            return BadRequest(new { errors = new[] { ex.Message } });
        }
    }

    [HttpGet("posts")]
    public async Task<IActionResult> Posts([FromQuery] string? channelSlug, [FromQuery] string? status, [FromQuery] string? q, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 25, CancellationToken cancellationToken = default)
    {
        try
        {
            return Ok(await _social.GetAdminPostsAsync(channelSlug, status, q, pageNumber, pageSize, cancellationToken));
        }
        catch (Exception ex)
        {
            return BadRequest(new { errors = new[] { ex.Message } });
        }
    }

    [HttpGet("posts/{postId}")]
    public async Task<IActionResult> PostDetails(string postId, CancellationToken cancellationToken)
    {
        try
        {
            var post = await _social.GetAdminPostDetailsAsync(postId, cancellationToken);
            return post == null ? NotFound(new { errors = new[] { "Post not found." } }) : Ok(post);
        }
        catch (Exception ex)
        {
            return BadRequest(new { errors = new[] { ex.Message } });
        }
    }

    [HttpGet("profiles")]
    public async Task<IActionResult> Profiles([FromQuery] string? q, [FromQuery] string? role, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 25, CancellationToken cancellationToken = default)
    {
        try
        {
            return Ok(await _social.GetAdminProfilesAsync(q, role, pageNumber, pageSize, cancellationToken));
        }
        catch (Exception ex)
        {
            return BadRequest(new { errors = new[] { ex.Message } });
        }
    }

    [HttpGet("profiles/{profileIdOrUserIdOrUsername}")]
    public async Task<IActionResult> ProfileDetails(string profileIdOrUserIdOrUsername, CancellationToken cancellationToken)
    {
        try
        {
            var profile = await _social.GetAdminProfileDetailsAsync(profileIdOrUserIdOrUsername, cancellationToken);
            return profile == null ? NotFound(new { errors = new[] { "Profile not found." } }) : Ok(profile);
        }
        catch (Exception ex)
        {
            return BadRequest(new { errors = new[] { ex.Message } });
        }
    }

    [HttpGet("conversations")]
    public async Task<IActionResult> Conversations([FromQuery] string? status, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 25, CancellationToken cancellationToken = default)
    {
        try
        {
            return Ok(await _social.GetAdminConversationsAsync(status, pageNumber, pageSize, cancellationToken));
        }
        catch (Exception ex)
        {
            return BadRequest(new { errors = new[] { ex.Message } });
        }
    }

    [HttpGet("conversations/{conversationId}/messages")]
    public async Task<IActionResult> ConversationMessages(string conversationId, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _social.GetAdminMessagesAsync(conversationId, cancellationToken));
        }
        catch (Exception ex)
        {
            return BadRequest(new { errors = new[] { ex.Message } });
        }
    }

    [HttpGet("reports")]
    public async Task<IActionResult> Reports([FromQuery] string? status, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _social.GetReportsAsync(status, cancellationToken));
        }
        catch (Exception ex)
        {
            return BadRequest(new { errors = new[] { ex.Message } });
        }
    }

    [HttpPut("reports/{reportId}")]
    public async Task<IActionResult> UpdateReport(string reportId, [FromBody] ModerateReportRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var report = await _social.UpdateReportStatusAsync(reportId, request, cancellationToken);
            return report == null ? NotFound(new { errors = new[] { "Report not found." } }) : Ok(report);
        }
        catch (Exception ex)
        {
            return BadRequest(new { errors = new[] { ex.Message } });
        }
    }

    [HttpPut("posts/{postId}/moderation")]
    public async Task<IActionResult> ModeratePost(string postId, [FromBody] ModerationRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var post = await _social.ModeratePostAsync(postId, request, cancellationToken);
            return post == null ? NotFound(new { errors = new[] { "Post not found." } }) : Ok(post);
        }
        catch (Exception ex)
        {
            return BadRequest(new { errors = new[] { ex.Message } });
        }
    }

    [HttpPut("comments/{commentId}/moderation")]
    public async Task<IActionResult> ModerateComment(string commentId, [FromBody] ModerationRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var comment = await _social.ModerateCommentAsync(commentId, request, cancellationToken);
            return comment == null ? NotFound(new { errors = new[] { "Comment not found." } }) : Ok(comment);
        }
        catch (Exception ex)
        {
            return BadRequest(new { errors = new[] { ex.Message } });
        }
    }
}
