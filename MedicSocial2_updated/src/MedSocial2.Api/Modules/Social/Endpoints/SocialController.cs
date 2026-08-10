using System.Security.Claims;
using MedSocial2.Api.Modules.Social.Application;
using MedSocial2.Api.Modules.Social.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Shared.Features;
using Shared.Security;

namespace MedSocial2.Api.Modules.Social.Endpoints;

[ApiController]
[Route("api/social")]
public class SocialController : ControllerBase
{
    private readonly ISocialService _social;
    private readonly IHubContext<SocialHub> _hub;
    private readonly IWebHostEnvironment _environment;
    private readonly IPlatformFeatureService _features;
    private readonly IFileUploadSecurityService _fileSecurity;

    public SocialController(ISocialService social, IHubContext<SocialHub> hub, IWebHostEnvironment environment, IPlatformFeatureService features, IFileUploadSecurityService fileSecurity)
    {
        _social = social;
        _hub = hub;
        _environment = environment;
        _features = features;
        _fileSecurity = fileSecurity;
    }

    [HttpGet("channels")]
    [AllowAnonymous]
    public async Task<IActionResult> Channels(CancellationToken cancellationToken)
    {
        try
        {
            var gate = await EnsureSocialEnabledAsync(cancellationToken);
            if (gate != null) return gate;
            return Ok(await _social.GetChannelsAsync(TryCurrentUserId(), cancellationToken));
        }
        catch (Exception ex)
        {
            return BadRequest(new { errors = new[] { ex.Message } });
        }
    }

    [HttpPost("channels")]
    [Authorize]
    public async Task<IActionResult> CreateChannel([FromBody] CreateSocialChannelRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var gate = await EnsureSocialEnabledAsync(cancellationToken);
            if (gate != null) return gate;
            var channel = await _social.CreateChannelAsync(CurrentUserId(), request, cancellationToken);
            await _hub.Clients.All.SendAsync("channelCreated", channel, cancellationToken);
            return Ok(channel);
        }
        catch (Exception ex)
        {
            return BadRequest(new { errors = new[] { ex.Message } });
        }
    }

    [HttpGet("feed")]
    [AllowAnonymous]
    public async Task<IActionResult> Feed([FromQuery] string? channelSlug, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 25, CancellationToken cancellationToken = default)
    {
        try
        {
            var gate = await EnsureSocialEnabledAsync(cancellationToken);
            if (gate != null) return gate;
            return Ok(await _social.GetFeedAsync(channelSlug, pageNumber, pageSize, cancellationToken));
        }
        catch (Exception ex)
        {
            return BadRequest(new { errors = new[] { ex.Message } });
        }
    }

    [HttpGet("profile/me")]
    [Authorize]
    public async Task<IActionResult> MyProfile(CancellationToken cancellationToken)
    {
        try
        {
            var gate = await EnsureSocialEnabledAsync(cancellationToken);
            if (gate != null) return gate;
            var profile = await _social.GetProfileAsync(CurrentUserId(), cancellationToken);
            return profile == null ? NotFound(new { errors = new[] { "Social profile has not been created." } }) : Ok(profile);
        }
        catch (Exception ex)
        {
            return BadRequest(new { errors = new[] { ex.Message } });
        }
    }

    [HttpPut("profile/me")]
    [Authorize]
    public async Task<IActionResult> UpsertProfile([FromBody] UpsertSocialProfileRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var gate = await EnsureSocialEnabledAsync(cancellationToken);
            if (gate != null) return gate;
            var profile = await _social.UpsertProfileAsync(CurrentUserId(), request, cancellationToken);
            await _hub.Clients.All.SendAsync("profileUpdated", profile, cancellationToken);
            return Ok(profile);
        }
        catch (Exception ex)
        {
            return BadRequest(new { errors = new[] { ex.Message } });
        }
    }

    [HttpGet("profiles/{username}")]
    [AllowAnonymous]
    public async Task<IActionResult> ProfileByUsername(string username, CancellationToken cancellationToken)
    {
        try
        {
            var gate = await EnsureSocialEnabledAsync(cancellationToken);
            if (gate != null) return gate;
            var profile = await _social.GetProfileByUsernameAsync(username, cancellationToken);
            return profile == null ? NotFound(new { errors = new[] { "Social profile not found." } }) : Ok(profile);
        }
        catch (Exception ex)
        {
            return BadRequest(new { errors = new[] { ex.Message } });
        }
    }

    [HttpPost("posts")]
    [Authorize]
    public async Task<IActionResult> CreatePost([FromBody] CreatePostRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var gate = await EnsureSocialEnabledAsync(cancellationToken);
            if (gate != null) return gate;
            var post = await _social.CreatePostAsync(CurrentUserId(), request, cancellationToken);
            await _hub.Clients.Group($"feed:{post.ChannelSlug}").SendAsync("postCreated", post, cancellationToken);
            await _hub.Clients.Group("feed:global").SendAsync("postCreated", post, cancellationToken);
            return Ok(post);
        }
        catch (Exception ex)
        {
            return BadRequest(new { errors = new[] { ex.Message } });
        }
    }

    [HttpGet("posts/{postId}/comments")]
    [AllowAnonymous]
    public async Task<IActionResult> Comments(string postId, CancellationToken cancellationToken)
    {
        try
        {
            var gate = await EnsureSocialEnabledAsync(cancellationToken);
            if (gate != null) return gate;
            return Ok(await _social.GetCommentsAsync(postId, cancellationToken));
        }
        catch (Exception ex)
        {
            return BadRequest(new { errors = new[] { ex.Message } });
        }
    }

    [HttpPost("posts/{postId}/comments")]
    [Authorize]
    public async Task<IActionResult> CreateComment(string postId, [FromBody] CreateCommentRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var gate = await EnsureSocialEnabledAsync(cancellationToken);
            if (gate != null) return gate;
            var comment = await _social.CreateCommentAsync(CurrentUserId(), postId, request, cancellationToken);
            await _hub.Clients.Group($"post:{postId}").SendAsync("commentCreated", comment, cancellationToken);
            await _hub.Clients.All.SendAsync("commentCreated", comment, cancellationToken);
            return Ok(comment);
        }
        catch (Exception ex)
        {
            return BadRequest(new { errors = new[] { ex.Message } });
        }
    }

    [HttpPost("{targetType}/{targetId}/reactions")]
    [Authorize]
    public async Task<IActionResult> React(string targetType, string targetId, [FromBody] ReactRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var gate = await EnsureSocialEnabledAsync(cancellationToken);
            if (gate != null) return gate;
            var updatedPost = await _social.ReactAsync(CurrentUserId(), targetType, targetId, request.ReactionType, cancellationToken);
            if (updatedPost != null)
            {
                await _hub.Clients.All.SendAsync("postUpdated", updatedPost, cancellationToken);
            }

            return Ok(updatedPost);
        }
        catch (Exception ex)
        {
            return BadRequest(new { errors = new[] { ex.Message } });
        }
    }

    [HttpPost("reports")]
    [AllowAnonymous]
    public async Task<IActionResult> Report([FromBody] ReportContentRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var gate = await EnsureSocialEnabledAsync(cancellationToken);
            if (gate != null) return gate;
            var userId = TryCurrentUserId();
            return Ok(await _social.ReportAsync(userId, request, cancellationToken));
        }
        catch (Exception ex)
        {
            return BadRequest(new { errors = new[] { ex.Message } });
        }
    }

    [HttpGet("conversations")]
    [Authorize]
    public async Task<IActionResult> Conversations(CancellationToken cancellationToken)
    {
        try
        {
            var gate = await EnsureSocialEnabledAsync(cancellationToken);
            if (gate != null) return gate;
            return Ok(await _social.GetConversationsAsync(CurrentUserId(), cancellationToken));
        }
        catch (Exception ex)
        {
            return BadRequest(new { errors = new[] { ex.Message } });
        }
    }

    [HttpGet("people/search")]
    [Authorize]
    public async Task<IActionResult> SearchPeople([FromQuery] string q, [FromQuery] string? role, CancellationToken cancellationToken)
    {
        try
        {
            var gate = await EnsureSocialEnabledAsync(cancellationToken);
            if (gate != null) return gate;
            return Ok(await _social.SearchDirectoryAsync(CurrentUserId(), q, role, cancellationToken));
        }
        catch (Exception ex)
        {
            return BadRequest(new { errors = new[] { ex.Message } });
        }
    }

    [HttpPost("conversations")]
    [Authorize]
    public async Task<IActionResult> StartConversation([FromBody] StartConversationRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var gate = await EnsureSocialEnabledAsync(cancellationToken);
            if (gate != null) return gate;
            var conversation = await _social.StartConversationAsync(CurrentUserId(), request, cancellationToken);
            foreach (var participant in conversation.Participants.Where(x => x.UserId.HasValue))
            {
                await _hub.Clients.Group($"user:{participant.UserId!.Value}").SendAsync("conversationUpdated", conversation, cancellationToken);
            }
            return Ok(conversation);
        }
        catch (Exception ex)
        {
            return BadRequest(new { errors = new[] { ex.Message } });
        }
    }

    [HttpPost("conversations/{conversationId}/accept")]
    [Authorize]
    public async Task<IActionResult> AcceptConversation(string conversationId, CancellationToken cancellationToken)
    {
        try
        {
            var gate = await EnsureSocialEnabledAsync(cancellationToken);
            if (gate != null) return gate;
            var conversation = await _social.AcceptConversationAsync(CurrentUserId(), conversationId, cancellationToken);
            await _hub.Clients.Group($"conversation:{conversationId}").SendAsync("conversationUpdated", conversation, cancellationToken);
            return Ok(conversation);
        }
        catch (Exception ex)
        {
            return BadRequest(new { errors = new[] { ex.Message } });
        }
    }

    [HttpPost("conversations/{conversationId}/reject")]
    [Authorize]
    public async Task<IActionResult> RejectConversation(string conversationId, CancellationToken cancellationToken)
    {
        try
        {
            var gate = await EnsureSocialEnabledAsync(cancellationToken);
            if (gate != null) return gate;
            var conversation = await _social.RejectConversationAsync(CurrentUserId(), conversationId, cancellationToken);
            await _hub.Clients.Group($"conversation:{conversationId}").SendAsync("conversationUpdated", conversation, cancellationToken);
            foreach (var participant in conversation.Participants.Where(x => x.UserId.HasValue))
            {
                await _hub.Clients.Group($"user:{participant.UserId!.Value}").SendAsync("conversationUpdated", conversation, cancellationToken);
            }

            return Ok(conversation);
        }
        catch (Exception ex)
        {
            return BadRequest(new { errors = new[] { ex.Message } });
        }
    }

    [HttpGet("conversations/{conversationId}/messages")]
    [Authorize]
    public async Task<IActionResult> Messages(string conversationId, CancellationToken cancellationToken)
    {
        try
        {
            var gate = await EnsureSocialEnabledAsync(cancellationToken);
            if (gate != null) return gate;
            return Ok(await _social.GetMessagesAsync(CurrentUserId(), conversationId, cancellationToken));
        }
        catch (Exception ex)
        {
            return BadRequest(new { errors = new[] { ex.Message } });
        }
    }

    [HttpPost("conversations/{conversationId}/read")]
    [Authorize]
    public async Task<IActionResult> MarkConversationRead(string conversationId, CancellationToken cancellationToken)
    {
        try
        {
            var gate = await EnsureSocialEnabledAsync(cancellationToken);
            if (gate != null) return gate;
            var conversation = await _social.MarkConversationReadAsync(CurrentUserId(), conversationId, cancellationToken);
            await _hub.Clients.Group($"conversation:{conversationId}").SendAsync("conversationRead", conversation, cancellationToken);
            foreach (var participant in conversation.Participants.Where(x => x.UserId.HasValue))
            {
                await _hub.Clients.Group($"user:{participant.UserId!.Value}").SendAsync("conversationUpdated", conversation, cancellationToken);
            }

            return Ok(conversation);
        }
        catch (Exception ex)
        {
            return BadRequest(new { errors = new[] { ex.Message } });
        }
    }

    [HttpPost("conversations/{conversationId}/messages")]
    [Authorize]
    public async Task<IActionResult> SendMessage(string conversationId, [FromBody] SendMessageRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var gate = await EnsureSocialEnabledAsync(cancellationToken);
            if (gate != null) return gate;
            var message = await _social.SendMessageAsync(CurrentUserId(), conversationId, request, cancellationToken);
            await _hub.Clients.Group($"conversation:{conversationId}").SendAsync("messageCreated", message, cancellationToken);
            return Ok(message);
        }
        catch (Exception ex)
        {
            return BadRequest(new { errors = new[] { ex.Message } });
        }
    }

    [HttpPost("media")]
    [Authorize]
    [RequestSizeLimit(50_000_000)]
    public async Task<IActionResult> UploadMedia([FromForm] SocialMediaUploadRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var file = request.File;
            var gate = await EnsureSocialEnabledAsync(cancellationToken);
            if (gate != null) return gate;
            if (file == null)
            {
                return BadRequest(new { errors = new[] { "File is required." } });
            }

            if (file.Length == 0)
            {
                return BadRequest(new { errors = new[] { "File is empty." } });
            }
            var security = await _fileSecurity.ValidateAsync(file, 50_000_000, cancellationToken);
            if (!security.IsSafe)
            {
                return BadRequest(new { errors = new[] { security.Error ?? "The uploaded file failed security validation." } });
            }

            var root = _environment.WebRootPath;
            if (string.IsNullOrWhiteSpace(root))
            {
                root = Path.Combine(AppContext.BaseDirectory, "wwwroot");
            }

            var folder = Path.Combine(root, "social-media", DateTime.UtcNow.ToString("yyyyMMdd"));
            Directory.CreateDirectory(folder);
            var extension = Path.GetExtension(file.FileName);
            var fileName = $"{Guid.NewGuid():N}{extension}";
            var path = Path.Combine(folder, fileName);
            await using (var stream = System.IO.File.Create(path))
            {
                await file.CopyToAsync(stream, cancellationToken);
            }

            var url = $"{Request.Scheme}://{Request.Host}/social-media/{DateTime.UtcNow:yyyyMMdd}/{fileName}";
            var mediaType = file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase) ? "image"
                : file.ContentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase) ? "video"
                : "file";
            return Ok(new SocialMediaAssetDto(url, file.FileName, file.ContentType, file.Length, mediaType));
        }
        catch (Exception ex)
        {
            return BadRequest(new { errors = new[] { ex.Message } });
        }
    }

    private Guid CurrentUserId()
    {
        var value = User.FindFirst("UserId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(value, out var userId))
        {
            throw new InvalidOperationException("User id claim is missing.");
        }

        return userId;
    }

    private Guid? TryCurrentUserId()
    {
        var value = User.FindFirst("UserId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(value, out var userId) ? userId : null;
    }

    private async Task<IActionResult?> EnsureSocialEnabledAsync(CancellationToken cancellationToken)
    {
        var config = await _features.GetOrCreateAsync("social", "The community forum is temporarily unavailable.", cancellationToken);
        return config.IsEnabled ? null : StatusCode(StatusCodes.Status503ServiceUnavailable, new { errors = new[] { config.DisabledMessage ?? "Social features are disabled." } });
    }
}

public class SocialMediaUploadRequest
{
    public IFormFile? File { get; set; }
}
