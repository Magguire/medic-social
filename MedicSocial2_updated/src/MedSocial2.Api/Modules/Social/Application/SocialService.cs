using Identity.Domain;
using Employer.Domain;
using MedSocial2.Api.Modules.Social.Domain;
using MedSocial2.Api.Modules.Social.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using Professional.Domain;
using Shared.Data;
using Shared.Notifications;

namespace MedSocial2.Api.Modules.Social.Application;

public interface ISocialService
{
    Task<List<SocialChannelDto>> GetChannelsAsync(Guid? userId, CancellationToken cancellationToken);
    Task<SocialChannelDto> CreateChannelAsync(Guid userId, CreateSocialChannelRequest request, CancellationToken cancellationToken);
    Task<SocialProfileDto> UpsertProfileAsync(Guid userId, UpsertSocialProfileRequest request, CancellationToken cancellationToken);
    Task<SocialProfileDto?> GetProfileAsync(Guid userId, CancellationToken cancellationToken);
    Task<SocialProfileDto?> GetProfileByUsernameAsync(string username, CancellationToken cancellationToken);
    Task SetPresenceAsync(Guid userId, string status, CancellationToken cancellationToken);
    Task<List<SocialPostDto>> GetFeedAsync(string? channelSlug, int pageNumber, int pageSize, CancellationToken cancellationToken);
    Task<SocialPostDto> CreatePostAsync(Guid userId, CreatePostRequest request, CancellationToken cancellationToken);
    Task<List<SocialCommentDto>> GetCommentsAsync(string postId, CancellationToken cancellationToken);
    Task<SocialCommentDto> CreateCommentAsync(Guid userId, string postId, CreateCommentRequest request, CancellationToken cancellationToken);
    Task<SocialPostDto?> ReactAsync(Guid userId, string targetType, string targetId, string reactionType, CancellationToken cancellationToken);
    Task<SocialReportDto> ReportAsync(Guid? userId, ReportContentRequest request, CancellationToken cancellationToken);
    Task<List<SocialConversationDto>> GetConversationsAsync(Guid userId, CancellationToken cancellationToken);
    Task<List<SocialDirectoryUserDto>> SearchDirectoryAsync(Guid requesterUserId, string query, string? role, CancellationToken cancellationToken);
    Task<SocialConversationDto> StartConversationAsync(Guid senderUserId, StartConversationRequest request, CancellationToken cancellationToken);
    Task<SocialConversationDto> AcceptConversationAsync(Guid userId, string conversationId, CancellationToken cancellationToken);
    Task<SocialConversationDto> RejectConversationAsync(Guid userId, string conversationId, CancellationToken cancellationToken);
    Task<List<SocialMessageDto>> GetMessagesAsync(Guid userId, string conversationId, CancellationToken cancellationToken);
    Task<SocialConversationDto> MarkConversationReadAsync(Guid userId, string conversationId, CancellationToken cancellationToken);
    Task<SocialMessageDto> SendMessageAsync(Guid senderUserId, string conversationId, SendMessageRequest request, CancellationToken cancellationToken);
    Task<List<SocialReportDto>> GetReportsAsync(string? status, CancellationToken cancellationToken);
    Task<SocialPostDto?> ModeratePostAsync(string postId, ModerationRequest request, CancellationToken cancellationToken);
    Task<SocialCommentDto?> ModerateCommentAsync(string commentId, ModerationRequest request, CancellationToken cancellationToken);
    Task<AdminSocialOverviewDto> GetAdminOverviewAsync(CancellationToken cancellationToken);
    Task<List<SocialChannelDto>> GetAdminChannelsAsync(bool includeInactive, CancellationToken cancellationToken);
    Task<SocialChannelDto?> UpdateChannelAsync(string channelIdOrSlug, UpdateSocialChannelRequest request, CancellationToken cancellationToken);
    Task<List<AdminSocialPostDto>> GetAdminPostsAsync(string? channelSlug, string? moderationStatus, string? query, int pageNumber, int pageSize, CancellationToken cancellationToken);
    Task<AdminSocialPostDetailDto?> GetAdminPostDetailsAsync(string postId, CancellationToken cancellationToken);
    Task<List<SocialProfileDto>> GetAdminProfilesAsync(string? query, string? role, int pageNumber, int pageSize, CancellationToken cancellationToken);
    Task<AdminSocialProfileDetailDto?> GetAdminProfileDetailsAsync(string profileIdOrUserIdOrUsername, CancellationToken cancellationToken);
    Task<List<AdminSocialConversationDto>> GetAdminConversationsAsync(string? status, int pageNumber, int pageSize, CancellationToken cancellationToken);
    Task<List<SocialMessageDto>> GetAdminMessagesAsync(string conversationId, CancellationToken cancellationToken);
    Task<SocialReportDto?> UpdateReportStatusAsync(string reportId, ModerateReportRequest request, CancellationToken cancellationToken);
}

public class SocialService : ISocialService
{
    private readonly SocialMongoContext _mongo;
    private readonly ApplicationDbContext _db;
    private readonly INotificationService _notifications;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public SocialService(SocialMongoContext mongo, ApplicationDbContext db, INotificationService notifications, IHttpContextAccessor httpContextAccessor)
    {
        _mongo = mongo;
        _db = db;
        _notifications = notifications;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<List<SocialChannelDto>> GetChannelsAsync(Guid? userId, CancellationToken cancellationToken)
    {
        var channels = await _mongo.Channels.Find(x => x.IsActive).SortBy(x => x.Name).ToListAsync(cancellationToken);
        string? userType = null;
        if (userId.HasValue)
        {
            userType = (await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId.Value, cancellationToken))?.UserType.ToString();
        }

        return channels
            .Where(x => x.VisibleToUserTypes.Count == 0 || (userType != null && x.VisibleToUserTypes.Contains(userType, StringComparer.OrdinalIgnoreCase)))
            .Select(Map)
            .ToList();
    }

    public async Task<SocialChannelDto> CreateChannelAsync(Guid userId, CreateSocialChannelRequest request, CancellationToken cancellationToken)
    {
        var name = request.Name?.Trim() ?? string.Empty;
        if (name.Length < 3)
        {
            throw new InvalidOperationException("Channel name must be at least 3 characters.");
        }

        var slug = await CreateUniqueSlugAsync(name, cancellationToken);
        var channel = new SocialChannelDocument
        {
            Name = name,
            Slug = slug,
            Description = request.Description?.Trim() ?? string.Empty,
            IsCommunity = true,
            IsActive = true,
            CreatedByUserId = userId,
            AdminUserIds = [userId],
            JoinPolicy = NormalizeChoice(request.JoinPolicy, ["Anyone", "InviteOnly"], "Anyone"),
            PostingPolicy = NormalizeChoice(request.PostingPolicy, ["Anyone", "AdminsOnly"], "Anyone"),
            AllowedMediaTypes = NormalizeList(request.AllowedMediaTypes, ["text", "image", "video", "file", "link"]),
            VisibleToUserTypes = NormalizeList(request.VisibleToUserTypes, []),
            VisibleToCategories = NormalizeList(request.VisibleToCategories, []),
            VisibleToLocations = NormalizeList(request.VisibleToLocations, []),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        if (channel.AllowedMediaTypes.Count == 0)
        {
            channel.AllowedMediaTypes = ["text"];
        }

        await _mongo.Channels.InsertOneAsync(channel, cancellationToken: cancellationToken);
        return Map(channel);
    }

    public async Task<SocialProfileDto> UpsertProfileAsync(Guid userId, UpsertSocialProfileRequest request, CancellationToken cancellationToken)
    {
        var username = NormalizeUsername(request.Username);
        if (string.IsNullOrWhiteSpace(username))
        {
            throw new InvalidOperationException("Username is required.");
        }

        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == userId, cancellationToken)
            ?? throw new InvalidOperationException("User not found.");
        var existingUsername = await _mongo.Profiles.Find(x => x.Username == username && x.UserId != userId).FirstOrDefaultAsync(cancellationToken);
        if (existingUsername != null)
        {
            throw new InvalidOperationException("Username is already taken.");
        }

        var displayName = string.IsNullOrWhiteSpace(request.DisplayName)
            ? $"{user.FirstName} {user.LastName}".Trim()
            : request.DisplayName.Trim();
        var role = user.UserType.ToString();

        await _mongo.Profiles.UpdateOneAsync(
            x => x.UserId == userId,
            Builders<SocialProfileDocument>.Update
                .Set(x => x.UserId, userId)
                .Set(x => x.Username, username)
                .Set(x => x.DisplayName, displayName)
                .Set(x => x.Role, role)
                .Set(x => x.AvatarUrl, request.AvatarUrl?.Trim() ?? string.Empty)
                .Set(x => x.Status, NormalizeStatus(request.Status))
                .Set(x => x.Bio, request.Bio?.Trim() ?? string.Empty)
                .Set(x => x.UpdatedAt, DateTime.UtcNow)
                .SetOnInsert(x => x.CreatedAt, DateTime.UtcNow),
            new UpdateOptions { IsUpsert = true },
            cancellationToken);

        var profile = await _mongo.Profiles.Find(x => x.UserId == userId).FirstAsync(cancellationToken);
        return Map(profile);
    }

    public async Task<SocialProfileDto?> GetProfileAsync(Guid userId, CancellationToken cancellationToken)
    {
        var profile = await _mongo.Profiles.Find(x => x.UserId == userId).FirstOrDefaultAsync(cancellationToken);
        return profile == null ? null : Map(profile);
    }

    public async Task<SocialProfileDto?> GetProfileByUsernameAsync(string username, CancellationToken cancellationToken)
    {
        var profile = await _mongo.Profiles.Find(x => x.Username == NormalizeUsername(username)).FirstOrDefaultAsync(cancellationToken);
        return profile == null ? null : Map(profile);
    }

    public async Task SetPresenceAsync(Guid userId, string status, CancellationToken cancellationToken)
    {
        await _mongo.Profiles.UpdateOneAsync(
            x => x.UserId == userId,
            Builders<SocialProfileDocument>.Update
                .Set(x => x.Status, NormalizeStatus(status))
                .Set(x => x.LastSeenAt, DateTime.UtcNow)
                .Set(x => x.UpdatedAt, DateTime.UtcNow),
            cancellationToken: cancellationToken);
    }

    public async Task<List<SocialPostDto>> GetFeedAsync(string? channelSlug, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var filter = Builders<SocialPostDocument>.Filter.Eq(x => x.IsHidden, false);
        if (!string.IsNullOrWhiteSpace(channelSlug) && !string.Equals(channelSlug, "all", StringComparison.OrdinalIgnoreCase))
        {
            filter &= Builders<SocialPostDocument>.Filter.Eq(x => x.ChannelSlug, channelSlug.Trim());
        }

        var posts = await _mongo.Posts.Find(filter)
            .SortByDescending(x => x.CreatedAt)
            .Skip(Math.Max(0, pageNumber - 1) * Math.Clamp(pageSize, 1, 100))
            .Limit(Math.Clamp(pageSize, 1, 100))
            .ToListAsync(cancellationToken);
        return posts.Select(Map).ToList();
    }

    public async Task<SocialPostDto> CreatePostAsync(Guid userId, CreatePostRequest request, CancellationToken cancellationToken)
    {
        var author = await GetAuthorAsync(userId, cancellationToken);
        var channelSlug = string.IsNullOrWhiteSpace(request.ChannelSlug) || string.Equals(request.ChannelSlug, "all", StringComparison.OrdinalIgnoreCase)
            ? "global"
            : request.ChannelSlug.Trim();
        await EnsureCanPostToChannelAsync(userId, channelSlug, request, cancellationToken);
        var entity = new SocialPostDocument
        {
            ChannelSlug = channelSlug,
            Text = request.Text?.Trim() ?? string.Empty,
            Links = request.Links?.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct().ToList() ?? [],
            Media = request.Media?.Select(Map).ToList() ?? [],
            Author = author,
            RequestMetadata = CaptureRequestMetadata(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _mongo.Posts.InsertOneAsync(entity, cancellationToken: cancellationToken);
        return Map(entity);
    }

    public async Task<List<SocialCommentDto>> GetCommentsAsync(string postId, CancellationToken cancellationToken)
    {
        var comments = await _mongo.Comments.Find(x => x.PostId == postId && !x.IsHidden).SortBy(x => x.CreatedAt).ToListAsync(cancellationToken);
        return comments.Select(Map).ToList();
    }

    public async Task<SocialCommentDto> CreateCommentAsync(Guid userId, string postId, CreateCommentRequest request, CancellationToken cancellationToken)
    {
        var post = await _mongo.Posts.Find(x => x.Id == postId && !x.IsHidden).FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("Post not found.");
        var entity = new SocialCommentDocument
        {
            PostId = postId,
            Text = request.Text.Trim(),
            Media = request.Media?.Select(Map).ToList() ?? [],
            Author = await GetAuthorAsync(userId, cancellationToken),
            RequestMetadata = CaptureRequestMetadata(),
            CreatedAt = DateTime.UtcNow
        };
        await _mongo.Comments.InsertOneAsync(entity, cancellationToken: cancellationToken);
        await _mongo.Posts.UpdateOneAsync(x => x.Id == post.Id, Builders<SocialPostDocument>.Update.Inc(x => x.CommentCount, 1).Set(x => x.UpdatedAt, DateTime.UtcNow), cancellationToken: cancellationToken);
        return Map(entity);
    }

    public async Task<SocialPostDto?> ReactAsync(Guid userId, string targetType, string targetId, string reactionType, CancellationToken cancellationToken)
    {
        targetType = targetType.Trim().ToLowerInvariant();
        reactionType = reactionType.Trim().ToLowerInvariant();
        if (reactionType is not ("like" or "upvote"))
        {
            throw new InvalidOperationException("Unsupported reaction.");
        }

        var filter = Builders<SocialReactionDocument>.Filter.Eq(x => x.TargetType, targetType)
            & Builders<SocialReactionDocument>.Filter.Eq(x => x.TargetId, targetId)
            & Builders<SocialReactionDocument>.Filter.Eq(x => x.UserId, userId)
            & Builders<SocialReactionDocument>.Filter.Eq(x => x.ReactionType, reactionType);
        var existing = await _mongo.Reactions.Find(filter).FirstOrDefaultAsync(cancellationToken);
        var inc = existing == null ? 1 : -1;
        if (existing == null)
        {
            await _mongo.Reactions.InsertOneAsync(new SocialReactionDocument { TargetType = targetType, TargetId = targetId, UserId = userId, ReactionType = reactionType, RequestMetadata = CaptureRequestMetadata() }, cancellationToken: cancellationToken);
        }
        else
        {
            await _mongo.Reactions.DeleteOneAsync(filter, cancellationToken);
        }

        if (targetType == "post")
        {
            var update = reactionType == "like"
                ? Builders<SocialPostDocument>.Update.Inc(x => x.LikeCount, inc).Set(x => x.UpdatedAt, DateTime.UtcNow)
                : Builders<SocialPostDocument>.Update.Inc(x => x.UpvoteCount, inc).Set(x => x.UpdatedAt, DateTime.UtcNow);
            await _mongo.Posts.UpdateOneAsync(x => x.Id == targetId, update, cancellationToken: cancellationToken);
            var post = await _mongo.Posts.Find(x => x.Id == targetId).FirstOrDefaultAsync(cancellationToken);
            return post == null ? null : Map(post);
        }

        var commentUpdate = reactionType == "like"
            ? Builders<SocialCommentDocument>.Update.Inc(x => x.LikeCount, inc)
            : Builders<SocialCommentDocument>.Update.Inc(x => x.UpvoteCount, inc);
        await _mongo.Comments.UpdateOneAsync(x => x.Id == targetId, commentUpdate, cancellationToken: cancellationToken);
        return null;
    }

    public async Task<SocialReportDto> ReportAsync(Guid? userId, ReportContentRequest request, CancellationToken cancellationToken)
    {
        var report = new SocialReportDocument { TargetType = request.TargetType, TargetId = request.TargetId, ReporterUserId = userId, Reason = request.Reason.Trim() };
        await _mongo.Reports.InsertOneAsync(report, cancellationToken: cancellationToken);
        return Map(report);
    }

    public async Task<List<SocialConversationDto>> GetConversationsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var conversations = await _mongo.Conversations.Find(x => x.ParticipantUserIds.Contains(userId)).SortByDescending(x => x.UpdatedAt).ToListAsync(cancellationToken);
        var items = new List<SocialConversationDto>();
        foreach (var conversation in conversations)
        {
            items.Add(await MapConversationAsync(conversation, userId, cancellationToken));
        }

        return items;
    }

    public async Task<SocialConversationDto> StartConversationAsync(Guid senderUserId, StartConversationRequest request, CancellationToken cancellationToken)
    {
        if (senderUserId == request.RecipientUserId)
        {
            throw new InvalidOperationException("You cannot message yourself.");
        }

        await EnsureConversationInitiationAllowedAsync(senderUserId, request.RecipientUserId, cancellationToken);

        var shouldNotifyRecipient = false;
        var existing = await _mongo.Conversations.Find(x => x.ParticipantUserIds.Contains(senderUserId) && x.ParticipantUserIds.Contains(request.RecipientUserId)).FirstOrDefaultAsync(cancellationToken);
        if (existing == null)
        {
            existing = new SocialConversationDocument
            {
                ParticipantUserIds = [senderUserId, request.RecipientUserId],
                Participants = [await GetAuthorAsync(senderUserId, cancellationToken), await GetAuthorAsync(request.RecipientUserId, cancellationToken)],
                RequestedByUserId = senderUserId,
                RequestedToUserId = request.RecipientUserId,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _mongo.Conversations.InsertOneAsync(existing, cancellationToken: cancellationToken);
            shouldNotifyRecipient = true;
        }
        else if (existing.Status == "Rejected")
        {
            await _mongo.Conversations.UpdateOneAsync(x => x.Id == existing.Id,
                Builders<SocialConversationDocument>.Update
                    .Set(x => x.Status, "Pending")
                    .Set(x => x.RequestedByUserId, senderUserId)
                    .Set(x => x.RequestedToUserId, request.RecipientUserId)
                    .Set(x => x.UpdatedAt, DateTime.UtcNow),
                cancellationToken: cancellationToken);
            existing.Status = "Pending";
            existing.RequestedByUserId = senderUserId;
            existing.RequestedToUserId = request.RecipientUserId;
            existing.UpdatedAt = DateTime.UtcNow;
            shouldNotifyRecipient = true;
        }

        if (!string.IsNullOrWhiteSpace(request.Text) || (request.Media?.Count ?? 0) > 0)
        {
            await SendMessageInternalAsync(senderUserId, existing, request.Text, request.Media ?? [], true, cancellationToken);
        }

        if (shouldNotifyRecipient || existing.Status == "Pending")
        {
            await NotifyConversationRecipientAsync(existing, senderUserId, "Chat request", "You have a new interaction request.", cancellationToken);
        }

        return await MapConversationAsync(existing, senderUserId, cancellationToken);
    }

    public async Task<SocialConversationDto> AcceptConversationAsync(Guid userId, string conversationId, CancellationToken cancellationToken)
    {
        var conversation = await GetOwnedConversationAsync(userId, conversationId, cancellationToken);
        if (conversation.RequestedToUserId != userId)
        {
            throw new InvalidOperationException("Only the requested recipient can accept this interaction.");
        }

        await _mongo.Conversations.UpdateOneAsync(x => x.Id == conversationId,
            Builders<SocialConversationDocument>.Update.Set(x => x.Status, "Accepted").Set(x => x.AcceptedAt, DateTime.UtcNow).Set(x => x.UpdatedAt, DateTime.UtcNow),
            cancellationToken: cancellationToken);
        conversation.Status = "Accepted";
        conversation.AcceptedAt = DateTime.UtcNow;
        conversation.UpdatedAt = DateTime.UtcNow;
        return await MapConversationAsync(conversation, userId, cancellationToken);
    }

    public async Task<SocialConversationDto> RejectConversationAsync(Guid userId, string conversationId, CancellationToken cancellationToken)
    {
        var conversation = await GetOwnedConversationAsync(userId, conversationId, cancellationToken);
        if (conversation.RequestedToUserId != userId)
        {
            throw new InvalidOperationException("Only the requested recipient can reject this interaction.");
        }

        await _mongo.Conversations.UpdateOneAsync(x => x.Id == conversationId,
            Builders<SocialConversationDocument>.Update.Set(x => x.Status, "Rejected").Set(x => x.UpdatedAt, DateTime.UtcNow),
            cancellationToken: cancellationToken);
        conversation.Status = "Rejected";
        conversation.UpdatedAt = DateTime.UtcNow;
        await NotifySpecificParticipantAsync(conversation.RequestedByUserId, "Chat request declined", "Your interaction request was declined.", cancellationToken);
        return await MapConversationAsync(conversation, userId, cancellationToken);
    }

    public async Task<List<SocialMessageDto>> GetMessagesAsync(Guid userId, string conversationId, CancellationToken cancellationToken)
    {
        var conversation = await GetOwnedConversationAsync(userId, conversationId, cancellationToken);
        var messages = await _mongo.Messages.Find(x => x.ConversationId == conversationId).SortBy(x => x.CreatedAt).ToListAsync(cancellationToken);
        var profiles = await GetParticipantProfilesAsync(conversation, cancellationToken);
        return messages.Select(message => Map(message, conversation, profiles)).ToList();
    }

    public async Task<SocialConversationDto> MarkConversationReadAsync(Guid userId, string conversationId, CancellationToken cancellationToken)
    {
        var conversation = await GetOwnedConversationAsync(userId, conversationId, cancellationToken);
        var now = DateTime.UtcNow;
        await _mongo.Messages.UpdateManyAsync(
            x => x.ConversationId == conversationId && x.SenderUserId != userId && !x.IsRead,
            Builders<SocialMessageDocument>.Update.Set(x => x.IsRead, true).Set(x => x.ReadAt, now),
            cancellationToken: cancellationToken);

        return await MapConversationAsync(conversation, userId, cancellationToken);
    }

    public async Task<SocialMessageDto> SendMessageAsync(Guid senderUserId, string conversationId, SendMessageRequest request, CancellationToken cancellationToken)
    {
        var conversation = await GetOwnedConversationAsync(senderUserId, conversationId, cancellationToken);
        if (conversation.Status != "Accepted")
        {
            throw new InvalidOperationException("This interaction must be accepted before messaging can continue.");
        }

        var message = await SendMessageInternalAsync(senderUserId, conversation, request.Text, request.Media ?? [], false, cancellationToken);
        await NotifyConversationRecipientAsync(conversation, senderUserId, "New message", string.IsNullOrWhiteSpace(message.Text) ? "You received a media message." : message.Text, cancellationToken);
        return message;
    }

    public async Task<List<SocialDirectoryUserDto>> SearchDirectoryAsync(Guid requesterUserId, string query, string? role, CancellationToken cancellationToken)
    {
        var requester = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == requesterUserId, cancellationToken)
            ?? throw new InvalidOperationException("Requester not found.");
        var term = query?.Trim() ?? string.Empty;
        var digits = new string(term.Where(char.IsDigit).ToArray());
        var looksLikeEmail = term.Contains('@', StringComparison.Ordinal) && term.Length >= 6;
        var looksLikePhone = digits.Length >= 7;
        if (!looksLikeEmail && !looksLikePhone)
        {
            throw new InvalidOperationException("Search by a complete email address or at least 7 phone digits.");
        }

        var allowedTypes = GetAllowedDirectoryTargets(requester.UserType, role);
        if (allowedTypes.Count == 0)
        {
            throw new InvalidOperationException("Your account cannot initiate this type of conversation.");
        }

        var users = await _db.Users.AsNoTracking()
            .Where(x => x.IsActive && allowedTypes.Contains(x.UserType))
            .Where(x => looksLikeEmail
                ? x.Email == term
                : x.PhoneNumber != null && x.PhoneNumber.Replace("+", "").Replace(" ", "").Replace("-", "").Contains(digits))
            .Take(10)
            .ToListAsync(cancellationToken);

        if (looksLikeEmail)
        {
            var professionalMatches = await _db.Set<ProfessionalProfile>().AsNoTracking()
                .Where(x => x.EmailAddress == term)
                .Select(x => x.UserId)
                .ToListAsync(cancellationToken);
            var employerEmails = await _db.Set<EmployerProfile>().AsNoTracking()
                .Where(x => x.ContactEmail == term)
                .Select(x => x.TenantId)
                .ToListAsync(cancellationToken);
            if (professionalMatches.Count > 0)
            {
                var additional = await _db.Users.AsNoTracking()
                    .Where(x => x.IsActive && allowedTypes.Contains(x.UserType) && professionalMatches.Contains(x.Id))
                    .Take(10)
                    .ToListAsync(cancellationToken);
                users.AddRange(additional);
            }

            if (employerEmails.Count > 0 && allowedTypes.Any(x => x is UserType.Employer or UserType.Recruiter))
            {
                var additional = await _db.Users.AsNoTracking()
                    .Where(x => x.IsActive && allowedTypes.Contains(x.UserType) && employerEmails.Contains(x.TenantId))
                    .Take(10)
                    .ToListAsync(cancellationToken);
                users.AddRange(additional);
            }
        }

        if (looksLikePhone)
        {
            var professionalMatches = await _db.Set<ProfessionalProfile>().AsNoTracking()
                .Where(x => x.PhoneNumber != null && x.PhoneNumber.Replace("+", "").Replace(" ", "").Replace("-", "").Contains(digits))
                .Select(x => x.UserId)
                .ToListAsync(cancellationToken);
            if (professionalMatches.Count > 0)
            {
                var additional = await _db.Users.AsNoTracking()
                    .Where(x => x.IsActive && allowedTypes.Contains(x.UserType) && professionalMatches.Contains(x.Id))
                    .Take(10)
                    .ToListAsync(cancellationToken);
                users.AddRange(additional);
            }
        }

        users = users.GroupBy(x => x.Id).Select(x => x.First()).Take(5).ToList();
        if (users.Count == 0)
        {
            return [];
        }

        var ids = users.Select(x => x.Id).ToList();
        var profiles = await _mongo.Profiles.Find(x => ids.Contains(x.UserId)).ToListAsync(cancellationToken);
        return users.Select(user =>
        {
            var profile = profiles.FirstOrDefault(x => x.UserId == user.Id);
            var displayName = profile?.DisplayName;
            if (string.IsNullOrWhiteSpace(displayName))
            {
                displayName = $"{user.FirstName} {user.LastName}".Trim();
            }

            return new SocialDirectoryUserDto(
                user.Id,
                string.IsNullOrWhiteSpace(displayName) ? "Platform member" : displayName,
                user.UserType.ToString(),
                MaskEmail(user.Email),
                MaskPhone(user.PhoneNumber),
                profile?.Username ?? GenerateDefaultUsername(user),
                profile?.AvatarUrl ?? string.Empty,
                profile?.Status ?? "Offline");
        }).ToList();
    }

    public async Task<List<SocialReportDto>> GetReportsAsync(string? status, CancellationToken cancellationToken)
    {
        var filter = string.IsNullOrWhiteSpace(status)
            ? Builders<SocialReportDocument>.Filter.Empty
            : Builders<SocialReportDocument>.Filter.Eq(x => x.Status, status);
        var reports = await _mongo.Reports.Find(filter).SortByDescending(x => x.CreatedAt).Limit(200).ToListAsync(cancellationToken);
        return reports.Select(Map).ToList();
    }

    public async Task<SocialPostDto?> ModeratePostAsync(string postId, ModerationRequest request, CancellationToken cancellationToken)
    {
        var hidden = string.Equals(request.Status, "Hidden", StringComparison.OrdinalIgnoreCase);
        await _mongo.Posts.UpdateOneAsync(x => x.Id == postId,
            Builders<SocialPostDocument>.Update.Set(x => x.ModerationStatus, request.Status).Set(x => x.ModerationReason, request.Reason).Set(x => x.IsHidden, hidden).Set(x => x.UpdatedAt, DateTime.UtcNow),
            cancellationToken: cancellationToken);
        var post = await _mongo.Posts.Find(x => x.Id == postId).FirstOrDefaultAsync(cancellationToken);
        return post == null ? null : Map(post);
    }

    public async Task<SocialCommentDto?> ModerateCommentAsync(string commentId, ModerationRequest request, CancellationToken cancellationToken)
    {
        var hidden = string.Equals(request.Status, "Hidden", StringComparison.OrdinalIgnoreCase);
        await _mongo.Comments.UpdateOneAsync(x => x.Id == commentId,
            Builders<SocialCommentDocument>.Update.Set(x => x.ModerationStatus, request.Status).Set(x => x.IsHidden, hidden),
            cancellationToken: cancellationToken);
        var comment = await _mongo.Comments.Find(x => x.Id == commentId).FirstOrDefaultAsync(cancellationToken);
        return comment == null ? null : Map(comment);
    }

    public async Task<AdminSocialOverviewDto> GetAdminOverviewAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var onlineCutoff = now.AddMinutes(-15);
        var profiles = await _mongo.Profiles.CountDocumentsAsync(Builders<SocialProfileDocument>.Filter.Empty, cancellationToken: cancellationToken);
        var onlineProfiles = await _mongo.Profiles.CountDocumentsAsync(x => x.LastSeenAt != null && x.LastSeenAt >= onlineCutoff, cancellationToken: cancellationToken);
        var channels = await _mongo.Channels.CountDocumentsAsync(Builders<SocialChannelDocument>.Filter.Empty, cancellationToken: cancellationToken);
        var activeChannels = await _mongo.Channels.CountDocumentsAsync(x => x.IsActive, cancellationToken: cancellationToken);
        var posts = await _mongo.Posts.CountDocumentsAsync(Builders<SocialPostDocument>.Filter.Empty, cancellationToken: cancellationToken);
        var hiddenPosts = await _mongo.Posts.CountDocumentsAsync(x => x.IsHidden, cancellationToken: cancellationToken);
        var comments = await _mongo.Comments.CountDocumentsAsync(Builders<SocialCommentDocument>.Filter.Empty, cancellationToken: cancellationToken);
        var hiddenComments = await _mongo.Comments.CountDocumentsAsync(x => x.IsHidden, cancellationToken: cancellationToken);
        var openReports = await _mongo.Reports.CountDocumentsAsync(x => x.Status == "Open", cancellationToken: cancellationToken);
        var conversations = await _mongo.Conversations.CountDocumentsAsync(Builders<SocialConversationDocument>.Filter.Empty, cancellationToken: cancellationToken);
        var pendingConversations = await _mongo.Conversations.CountDocumentsAsync(x => x.Status == "Pending", cancellationToken: cancellationToken);
        return new AdminSocialOverviewDto(profiles, onlineProfiles, channels, activeChannels, posts, hiddenPosts, comments, hiddenComments, openReports, conversations, pendingConversations, now);
    }

    public async Task<List<SocialChannelDto>> GetAdminChannelsAsync(bool includeInactive, CancellationToken cancellationToken)
    {
        var filter = includeInactive
            ? Builders<SocialChannelDocument>.Filter.Empty
            : Builders<SocialChannelDocument>.Filter.Eq(x => x.IsActive, true);
        var channels = await _mongo.Channels.Find(filter).SortByDescending(x => x.UpdatedAt).ToListAsync(cancellationToken);
        return channels.Select(Map).ToList();
    }

    public async Task<SocialChannelDto?> UpdateChannelAsync(string channelIdOrSlug, UpdateSocialChannelRequest request, CancellationToken cancellationToken)
    {
        var channel = await _mongo.Channels.Find(x => x.Id == channelIdOrSlug || x.Slug == channelIdOrSlug).FirstOrDefaultAsync(cancellationToken);
        if (channel == null)
        {
            return null;
        }

        var name = request.Name?.Trim() ?? string.Empty;
        if (name.Length < 3)
        {
            throw new InvalidOperationException("Channel name must be at least 3 characters.");
        }

        var update = Builders<SocialChannelDocument>.Update
            .Set(x => x.Name, name)
            .Set(x => x.Description, request.Description?.Trim() ?? string.Empty)
            .Set(x => x.IsActive, request.IsActive)
            .Set(x => x.JoinPolicy, NormalizeChoice(request.JoinPolicy, ["Anyone", "InviteOnly"], channel.JoinPolicy))
            .Set(x => x.PostingPolicy, NormalizeChoice(request.PostingPolicy, ["Anyone", "AdminsOnly"], channel.PostingPolicy))
            .Set(x => x.AllowedMediaTypes, NormalizeList(request.AllowedMediaTypes, ["text"]))
            .Set(x => x.VisibleToUserTypes, NormalizeList(request.VisibleToUserTypes, []))
            .Set(x => x.VisibleToCategories, NormalizeList(request.VisibleToCategories, []))
            .Set(x => x.VisibleToLocations, NormalizeList(request.VisibleToLocations, []))
            .Set(x => x.UpdatedAt, DateTime.UtcNow);
        await _mongo.Channels.UpdateOneAsync(x => x.Id == channel.Id, update, cancellationToken: cancellationToken);
        var updated = await _mongo.Channels.Find(x => x.Id == channel.Id).FirstOrDefaultAsync(cancellationToken);
        return updated == null ? null : Map(updated);
    }

    public async Task<List<AdminSocialPostDto>> GetAdminPostsAsync(string? channelSlug, string? moderationStatus, string? query, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var filter = Builders<SocialPostDocument>.Filter.Empty;
        if (!string.IsNullOrWhiteSpace(channelSlug) && !string.Equals(channelSlug, "all", StringComparison.OrdinalIgnoreCase))
        {
            filter &= Builders<SocialPostDocument>.Filter.Eq(x => x.ChannelSlug, channelSlug.Trim());
        }

        if (!string.IsNullOrWhiteSpace(moderationStatus) && !string.Equals(moderationStatus, "all", StringComparison.OrdinalIgnoreCase))
        {
            if (string.Equals(moderationStatus, "Hidden", StringComparison.OrdinalIgnoreCase))
            {
                filter &= Builders<SocialPostDocument>.Filter.Eq(x => x.IsHidden, true);
            }
            else if (string.Equals(moderationStatus, "Visible", StringComparison.OrdinalIgnoreCase))
            {
                filter &= Builders<SocialPostDocument>.Filter.Eq(x => x.IsHidden, false);
            }
            else
            {
                filter &= Builders<SocialPostDocument>.Filter.Eq(x => x.ModerationStatus, moderationStatus.Trim());
            }
        }

        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.Trim();
            filter &= Builders<SocialPostDocument>.Filter.Or(
                Builders<SocialPostDocument>.Filter.Regex(x => x.Text, new MongoDB.Bson.BsonRegularExpression(term, "i")),
                Builders<SocialPostDocument>.Filter.Regex("Author.DisplayName", new MongoDB.Bson.BsonRegularExpression(term, "i")),
                Builders<SocialPostDocument>.Filter.Regex("Author.Username", new MongoDB.Bson.BsonRegularExpression(term, "i")));
        }

        var posts = await _mongo.Posts.Find(filter)
            .SortByDescending(x => x.CreatedAt)
            .Skip(Math.Max(0, pageNumber - 1) * Math.Clamp(pageSize, 1, 100))
            .Limit(Math.Clamp(pageSize, 1, 100))
            .ToListAsync(cancellationToken);
        return posts.Select(MapAdmin).ToList();
    }

    public async Task<AdminSocialPostDetailDto?> GetAdminPostDetailsAsync(string postId, CancellationToken cancellationToken)
    {
        var post = await _mongo.Posts.Find(x => x.Id == postId).FirstOrDefaultAsync(cancellationToken);
        if (post == null)
        {
            return null;
        }

        var comments = await _mongo.Comments.Find(x => x.PostId == postId)
            .SortBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
        var commentIds = comments.Select(x => x.Id).Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x!).ToList();
        var reactionFilter = Builders<SocialReactionDocument>.Filter.Eq(x => x.TargetType, "post")
            & Builders<SocialReactionDocument>.Filter.Eq(x => x.TargetId, postId);
        if (commentIds.Count > 0)
        {
            reactionFilter |= Builders<SocialReactionDocument>.Filter.Eq(x => x.TargetType, "comment")
                & Builders<SocialReactionDocument>.Filter.In(x => x.TargetId, commentIds);
        }

        var reactions = await _mongo.Reactions.Find(reactionFilter)
            .SortByDescending(x => x.CreatedAt)
            .Limit(200)
            .ToListAsync(cancellationToken);
        var reportTargets = new List<string> { postId };
        reportTargets.AddRange(commentIds);
        var reports = await _mongo.Reports.Find(x => reportTargets.Contains(x.TargetId))
            .SortByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
        var authorProfile = post.Author.UserId.HasValue
            ? await _mongo.Profiles.Find(x => x.UserId == post.Author.UserId.Value).FirstOrDefaultAsync(cancellationToken)
            : null;
        var reactionProfiles = await GetProfilesForUsersAsync(reactions.Select(x => x.UserId), cancellationToken);

        return new AdminSocialPostDetailDto(
            MapAdmin(post),
            authorProfile == null ? null : Map(authorProfile),
            comments.Select(MapAdmin).ToList(),
            reactions.Select(x => MapAdmin(x, reactionProfiles)).ToList(),
            reports.Select(Map).ToList());
    }

    public async Task<List<SocialProfileDto>> GetAdminProfilesAsync(string? query, string? role, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var filter = Builders<SocialProfileDocument>.Filter.Empty;
        if (!string.IsNullOrWhiteSpace(role) && !string.Equals(role, "all", StringComparison.OrdinalIgnoreCase))
        {
            filter &= Builders<SocialProfileDocument>.Filter.Eq(x => x.Role, role.Trim());
        }

        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.Trim();
            filter &= Builders<SocialProfileDocument>.Filter.Or(
                Builders<SocialProfileDocument>.Filter.Regex(x => x.Username, new MongoDB.Bson.BsonRegularExpression(term, "i")),
                Builders<SocialProfileDocument>.Filter.Regex(x => x.DisplayName, new MongoDB.Bson.BsonRegularExpression(term, "i")),
                Builders<SocialProfileDocument>.Filter.Regex(x => x.Bio, new MongoDB.Bson.BsonRegularExpression(term, "i")));
        }

        var profiles = await _mongo.Profiles.Find(filter)
            .SortByDescending(x => x.UpdatedAt)
            .Skip(Math.Max(0, pageNumber - 1) * Math.Clamp(pageSize, 1, 100))
            .Limit(Math.Clamp(pageSize, 1, 100))
            .ToListAsync(cancellationToken);
        return profiles.Select(Map).ToList();
    }

    public async Task<AdminSocialProfileDetailDto?> GetAdminProfileDetailsAsync(string profileIdOrUserIdOrUsername, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(profileIdOrUserIdOrUsername))
        {
            return null;
        }

        var value = profileIdOrUserIdOrUsername.Trim();
        SocialProfileDocument? profile;
        if (Guid.TryParse(value, out var userId))
        {
            profile = await _mongo.Profiles.Find(x => x.UserId == userId).FirstOrDefaultAsync(cancellationToken);
        }
        else
        {
            profile = await _mongo.Profiles.Find(x => x.Id == value || x.Username == NormalizeUsername(value)).FirstOrDefaultAsync(cancellationToken);
        }

        if (profile == null)
        {
            return null;
        }

        var posts = await _mongo.Posts.Find(Builders<SocialPostDocument>.Filter.Eq("Author.UserId", profile.UserId))
            .SortByDescending(x => x.CreatedAt)
            .Limit(50)
            .ToListAsync(cancellationToken);
        var comments = await _mongo.Comments.Find(Builders<SocialCommentDocument>.Filter.Eq("Author.UserId", profile.UserId))
            .SortByDescending(x => x.CreatedAt)
            .Limit(50)
            .ToListAsync(cancellationToken);
        var conversations = await _mongo.Conversations.Find(x => x.ParticipantUserIds.Contains(profile.UserId))
            .SortByDescending(x => x.UpdatedAt)
            .Limit(50)
            .ToListAsync(cancellationToken);
        var conversationDtos = new List<AdminSocialConversationDto>();
        foreach (var conversation in conversations)
        {
            conversationDtos.Add(await MapAdminConversationAsync(conversation, cancellationToken));
        }

        return new AdminSocialProfileDetailDto(
            Map(profile),
            posts.Select(MapAdmin).ToList(),
            comments.Select(MapAdmin).ToList(),
            conversationDtos);
    }

    public async Task<List<AdminSocialConversationDto>> GetAdminConversationsAsync(string? status, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var filter = string.IsNullOrWhiteSpace(status) || string.Equals(status, "all", StringComparison.OrdinalIgnoreCase)
            ? Builders<SocialConversationDocument>.Filter.Empty
            : Builders<SocialConversationDocument>.Filter.Eq(x => x.Status, status.Trim());
        var conversations = await _mongo.Conversations.Find(filter)
            .SortByDescending(x => x.UpdatedAt)
            .Skip(Math.Max(0, pageNumber - 1) * Math.Clamp(pageSize, 1, 100))
            .Limit(Math.Clamp(pageSize, 1, 100))
            .ToListAsync(cancellationToken);
        var items = new List<AdminSocialConversationDto>();
        foreach (var conversation in conversations)
        {
            items.Add(await MapAdminConversationAsync(conversation, cancellationToken));
        }

        return items;
    }

    public async Task<List<SocialMessageDto>> GetAdminMessagesAsync(string conversationId, CancellationToken cancellationToken)
    {
        var conversation = await _mongo.Conversations.Find(x => x.Id == conversationId).FirstOrDefaultAsync(cancellationToken);
        if (conversation == null)
        {
            throw new InvalidOperationException("Conversation not found.");
        }

        var messages = await _mongo.Messages.Find(x => x.ConversationId == conversationId).SortBy(x => x.CreatedAt).ToListAsync(cancellationToken);
        var profiles = await GetParticipantProfilesAsync(conversation, cancellationToken);
        return messages.Select(message => Map(message, conversation, profiles)).ToList();
    }

    public async Task<SocialReportDto?> UpdateReportStatusAsync(string reportId, ModerateReportRequest request, CancellationToken cancellationToken)
    {
        var status = string.IsNullOrWhiteSpace(request.Status) ? "Reviewed" : request.Status.Trim();
        await _mongo.Reports.UpdateOneAsync(x => x.Id == reportId,
            Builders<SocialReportDocument>.Update.Set(x => x.Status, status),
            cancellationToken: cancellationToken);
        var report = await _mongo.Reports.Find(x => x.Id == reportId).FirstOrDefaultAsync(cancellationToken);
        return report == null ? null : Map(report);
    }

    private async Task<SocialMessageDto> SendMessageInternalAsync(Guid senderUserId, SocialConversationDocument conversation, string text, List<SocialMediaAssetDto> media, bool allowPending, CancellationToken cancellationToken)
    {
        if (!allowPending && conversation.Status != "Accepted")
        {
            throw new InvalidOperationException("This interaction must be accepted first.");
        }

        var message = new SocialMessageDocument
        {
            ConversationId = conversation.Id ?? string.Empty,
            SenderUserId = senderUserId,
            Sender = await GetAuthorAsync(senderUserId, cancellationToken),
            Text = text.Trim(),
            Media = media.Select(Map).ToList(),
            RequestMetadata = CaptureRequestMetadata(),
            CreatedAt = DateTime.UtcNow
        };
        await _mongo.Messages.InsertOneAsync(message, cancellationToken: cancellationToken);
        await _mongo.Conversations.UpdateOneAsync(x => x.Id == conversation.Id,
            Builders<SocialConversationDocument>.Update.Set(x => x.LastMessagePreview, string.IsNullOrWhiteSpace(text) ? "Media attachment" : text.Trim()[..Math.Min(120, text.Trim().Length)]).Set(x => x.UpdatedAt, DateTime.UtcNow),
            cancellationToken: cancellationToken);
        var profiles = await GetParticipantProfilesAsync(conversation, cancellationToken);
        return Map(message, conversation, profiles);
    }

    private async Task<SocialConversationDocument> GetOwnedConversationAsync(Guid userId, string conversationId, CancellationToken cancellationToken)
    {
        var conversation = await _mongo.Conversations.Find(x => x.Id == conversationId && x.ParticipantUserIds.Contains(userId)).FirstOrDefaultAsync(cancellationToken);
        return conversation ?? throw new InvalidOperationException("Conversation not found.");
    }

    private async Task NotifyConversationRecipientAsync(SocialConversationDocument conversation, Guid senderUserId, string title, string message, CancellationToken cancellationToken)
    {
        var recipientIds = conversation.ParticipantUserIds.Where(id => id != senderUserId).Distinct().ToList();
        foreach (var recipientId in recipientIds)
        {
            await NotifySpecificParticipantAsync(recipientId, title, message, cancellationToken);
        }
    }

    private async Task NotifySpecificParticipantAsync(Guid userId, string title, string message, CancellationToken cancellationToken)
    {
        await _notifications.NotifyAsync(
            userId,
            "SocialMessage",
            title,
            message.Length > 160 ? $"{message[..157]}..." : message,
            "/feed?messages=open",
            "SocialConversation",
            null,
            cancellationToken);
    }

    private async Task<List<SocialProfileDocument>> GetParticipantProfilesAsync(SocialConversationDocument conversation, CancellationToken cancellationToken)
    {
        var participantIds = conversation.ParticipantUserIds.Distinct().ToList();
        return participantIds.Count == 0
            ? []
            : await _mongo.Profiles.Find(x => participantIds.Contains(x.UserId)).ToListAsync(cancellationToken);
    }

    private async Task<Dictionary<Guid, SocialProfileDocument>> GetProfilesForUsersAsync(IEnumerable<Guid> userIds, CancellationToken cancellationToken)
    {
        var ids = userIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return [];
        }

        var profiles = await _mongo.Profiles.Find(x => ids.Contains(x.UserId)).ToListAsync(cancellationToken);
        return profiles.ToDictionary(x => x.UserId, x => x);
    }

    private SocialRequestMetadata CaptureRequestMetadata()
    {
        var context = _httpContextAccessor.HttpContext;
        var request = context?.Request;
        return new SocialRequestMetadata
        {
            DeviceId = request?.Headers["X-MedSocial-Device-Id"].FirstOrDefault() ?? string.Empty,
            UserAgent = request?.Headers.UserAgent.FirstOrDefault() ?? string.Empty,
            IpAddress = context?.Connection.RemoteIpAddress?.ToString() ?? string.Empty,
            Source = request?.Headers["X-MedSocial-Client"].FirstOrDefault() ?? "web"
        };
    }

    private async Task<SocialConversationDto> MapConversationAsync(SocialConversationDocument entity, Guid viewerUserId, CancellationToken cancellationToken)
    {
        var unreadCount = await _mongo.Messages.CountDocumentsAsync(
            x => x.ConversationId == entity.Id && x.SenderUserId != viewerUserId && !x.IsRead,
            cancellationToken: cancellationToken);
        return Map(entity, unreadCount);
    }

    private async Task<AdminSocialConversationDto> MapAdminConversationAsync(SocialConversationDocument entity, CancellationToken cancellationToken)
    {
        var messageCount = await _mongo.Messages.CountDocumentsAsync(x => x.ConversationId == entity.Id, cancellationToken: cancellationToken);
        var unreadCount = await _mongo.Messages.CountDocumentsAsync(x => x.ConversationId == entity.Id && !x.IsRead, cancellationToken: cancellationToken);
        return MapAdmin(entity, messageCount, unreadCount);
    }

    private async Task<SocialAuthorSnapshot> GetAuthorAsync(Guid userId, CancellationToken cancellationToken)
    {
        var profile = await _mongo.Profiles.Find(x => x.UserId == userId).FirstOrDefaultAsync(cancellationToken);
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == userId, cancellationToken)
            ?? throw new InvalidOperationException("User not found.");
        var role = user.UserType.ToString();
        return new SocialAuthorSnapshot
        {
            UserId = userId,
            Username = profile?.Username ?? GenerateDefaultUsername(user),
            DisplayName = profile?.DisplayName ?? $"{user.FirstName} {user.LastName}".Trim(),
            Role = role,
            AvatarUrl = profile?.AvatarUrl ?? string.Empty,
            IsOrganization = user.UserType is UserType.Employer or UserType.Recruiter,
            GuestTag = string.Empty
        };
    }

    private async Task EnsureConversationInitiationAllowedAsync(Guid senderUserId, Guid recipientUserId, CancellationToken cancellationToken)
    {
        var users = await _db.Users.AsNoTracking()
            .Where(x => x.Id == senderUserId || x.Id == recipientUserId)
            .ToListAsync(cancellationToken);
        var sender = users.FirstOrDefault(x => x.Id == senderUserId)
            ?? throw new InvalidOperationException("Sender not found.");
        var recipient = users.FirstOrDefault(x => x.Id == recipientUserId)
            ?? throw new InvalidOperationException("Recipient not found.");

        if (IsAdminUser(sender.UserType))
        {
            if (recipient.UserType is UserType.Professional or UserType.Employer or UserType.Recruiter)
            {
                return;
            }
        }

        if (sender.UserType is UserType.Employer or UserType.Recruiter && recipient.UserType == UserType.Professional)
        {
            return;
        }

        throw new InvalidOperationException("Conversation requests can be initiated by employers to professionals, or by admins to employers and professionals.");
    }

    private static List<UserType> GetAllowedDirectoryTargets(UserType requesterType, string? role)
    {
        var requested = ParseUserType(role);
        if (IsAdminUser(requesterType))
        {
            var allowed = new[] { UserType.Professional, UserType.Employer, UserType.Recruiter };
            return requested.HasValue
                ? allowed.Contains(requested.Value) ? [requested.Value] : []
                : allowed.ToList();
        }

        if (requesterType is UserType.Employer or UserType.Recruiter)
        {
            return requested.HasValue && requested.Value != UserType.Professional ? [] : [UserType.Professional];
        }

        return [];
    }

    private static UserType? ParseUserType(string? role)
    {
        return Enum.TryParse<UserType>(role, true, out var parsed) ? parsed : null;
    }

    private static bool IsAdminUser(UserType userType) => userType is UserType.SuperAdmin or UserType.TenantAdmin or UserType.Auditor;

    private static string MaskEmail(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || !value.Contains('@', StringComparison.Ordinal))
        {
            return string.Empty;
        }

        var parts = value.Split('@', 2);
        var first = parts[0].Length <= 2 ? parts[0][..1] : parts[0][..2];
        return $"{first}***@{parts[1]}";
    }

    private static string MaskPhone(string? value)
    {
        var digits = new string((value ?? string.Empty).Where(char.IsDigit).ToArray());
        if (digits.Length < 4)
        {
            return string.Empty;
        }

        return $"***{digits[^4..]}";
    }

    private static string GenerateDefaultUsername(User user) => $"{user.UserType.ToString().ToLowerInvariant()}-{user.Id.ToString("N")[..8]}";
    private static string NormalizeUsername(string value) => new(value.Trim().ToLowerInvariant().Where(ch => char.IsLetterOrDigit(ch) || ch is '-' or '_' or '.').ToArray());
    private static string NormalizeStatus(string value) => string.IsNullOrWhiteSpace(value) ? "Available" : value.Trim();
    private async Task<string> CreateUniqueSlugAsync(string name, CancellationToken cancellationToken)
    {
        var baseSlug = new string(name.Trim().ToLowerInvariant().Select(ch => char.IsLetterOrDigit(ch) ? ch : '-').ToArray());
        while (baseSlug.Contains("--", StringComparison.Ordinal))
        {
            baseSlug = baseSlug.Replace("--", "-", StringComparison.Ordinal);
        }

        baseSlug = baseSlug.Trim('-');
        if (string.IsNullOrWhiteSpace(baseSlug))
        {
            baseSlug = $"channel-{Guid.NewGuid():N}"[..16];
        }

        var slug = baseSlug;
        var counter = 2;
        while (await _mongo.Channels.Find(x => x.Slug == slug).AnyAsync(cancellationToken))
        {
            slug = $"{baseSlug}-{counter++}";
        }

        return slug;
    }

    private async Task EnsureCanPostToChannelAsync(Guid userId, string channelSlug, CreatePostRequest request, CancellationToken cancellationToken)
    {
        if (string.Equals(channelSlug, "global", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var channel = await _mongo.Channels.Find(x => x.Slug == channelSlug && x.IsActive).FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("Channel is not available.");
        if (string.Equals(channel.PostingPolicy, "AdminsOnly", StringComparison.OrdinalIgnoreCase) && !channel.AdminUserIds.Contains(userId))
        {
            throw new InvalidOperationException("Only channel admins can post in this channel.");
        }

        var hasMedia = request.Media?.Count > 0;
        if (hasMedia && !channel.AllowedMediaTypes.Any(x => request.Media!.Any(media => string.Equals(media.MediaType, x, StringComparison.OrdinalIgnoreCase) || string.Equals(x, "file", StringComparison.OrdinalIgnoreCase))))
        {
            throw new InvalidOperationException("This channel does not allow the selected media type.");
        }

        if ((request.Links?.Count ?? 0) > 0 && !channel.AllowedMediaTypes.Contains("link", StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("This channel does not allow shared links.");
        }
    }

    private static string NormalizeChoice(string? value, string[] allowed, string fallback)
    {
        var match = allowed.FirstOrDefault(x => string.Equals(x, value, StringComparison.OrdinalIgnoreCase));
        return match ?? fallback;
    }

    private static List<string> NormalizeList(IEnumerable<string>? values, string[] fallback)
    {
        var list = values?
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList() ?? [];
        return list.Count == 0 ? fallback.ToList() : list;
    }

    private static SocialMediaAsset Map(SocialMediaAssetDto dto) => new() { Url = dto.Url, FileName = dto.FileName, ContentType = dto.ContentType, SizeBytes = dto.SizeBytes, MediaType = dto.MediaType };
    private static SocialMediaAssetDto Map(SocialMediaAsset entity) => new(entity.Url, entity.FileName, entity.ContentType, entity.SizeBytes, entity.MediaType);
    private static SocialAuthorDto Map(SocialAuthorSnapshot entity) => new(entity.UserId, entity.Username, entity.DisplayName, entity.Role, entity.AvatarUrl, entity.IsOrganization, entity.GuestTag);
    private static SocialRequestMetadataDto Map(SocialRequestMetadata? entity) => new(
        string.IsNullOrWhiteSpace(entity?.DeviceId) ? "Not captured" : entity.DeviceId,
        string.IsNullOrWhiteSpace(entity?.UserAgent) ? "Not captured" : entity.UserAgent,
        string.IsNullOrWhiteSpace(entity?.IpAddress) ? "Not captured" : entity.IpAddress,
        string.IsNullOrWhiteSpace(entity?.Source) ? "web" : entity.Source);
    private static SocialProfileDto Map(SocialProfileDocument entity) => new(entity.Id, entity.UserId, entity.Username, entity.DisplayName, entity.Role, entity.AvatarUrl, entity.Status, entity.Bio, entity.LastSeenAt);
    private static SocialChannelDto Map(SocialChannelDocument entity) => new(entity.Id, entity.Name, entity.Slug, entity.Description, entity.IsCommunity, entity.IsActive, entity.CreatedByUserId, entity.AdminUserIds, entity.JoinPolicy, entity.PostingPolicy, entity.AllowedMediaTypes, entity.VisibleToUserTypes, entity.VisibleToCategories, entity.VisibleToLocations, entity.CreatedAt);
    private static SocialPostDto Map(SocialPostDocument entity) => new(entity.Id, entity.ChannelSlug, entity.Text, entity.Links, entity.Media.Select(Map).ToList(), Map(entity.Author), entity.CommentCount, entity.LikeCount, entity.UpvoteCount, entity.ModerationStatus, entity.CreatedAt, entity.UpdatedAt);
    private static SocialCommentDto Map(SocialCommentDocument entity) => new(entity.Id, entity.PostId, entity.Text, entity.Media.Select(Map).ToList(), Map(entity.Author), entity.LikeCount, entity.UpvoteCount, entity.ModerationStatus, entity.CreatedAt);
    private static SocialConversationDto Map(SocialConversationDocument entity, long unreadCount = 0) => new(entity.Id, entity.Participants.Select(Map).ToList(), entity.Status, entity.LastMessagePreview, entity.RequestedByUserId, entity.RequestedToUserId, entity.CreatedAt, entity.UpdatedAt, unreadCount);
    private static SocialMessageDto Map(SocialMessageDocument entity, SocialConversationDocument? conversation = null, List<SocialProfileDocument>? profiles = null)
    {
        var deliveryStatus = "DeliveredOffline";
        if (entity.IsRead)
        {
            deliveryStatus = "Read";
        }
        else if (conversation != null && profiles != null)
        {
            var cutoff = DateTime.UtcNow.AddMinutes(-15);
            var recipientIds = conversation.ParticipantUserIds.Where(id => id != entity.SenderUserId).ToHashSet();
            var recipientOnline = profiles.Any(profile =>
                recipientIds.Contains(profile.UserId)
                && !string.Equals(profile.Status, "Offline", StringComparison.OrdinalIgnoreCase)
                && profile.LastSeenAt.HasValue
                && profile.LastSeenAt.Value >= cutoff);
            deliveryStatus = recipientOnline ? "DeliveredOnline" : "DeliveredOffline";
        }

        return new(entity.Id, entity.ConversationId, entity.SenderUserId, Map(entity.Sender), entity.Text, entity.Media.Select(Map).ToList(), entity.CreatedAt, entity.IsRead, entity.ReadAt, deliveryStatus);
    }
    private static SocialReportDto Map(SocialReportDocument entity) => new(entity.Id, entity.TargetType, entity.TargetId, entity.ReporterUserId, entity.Reason, entity.Status, entity.CreatedAt);
    private static AdminSocialPostDto MapAdmin(SocialPostDocument entity) => new(entity.Id, entity.ChannelSlug, entity.Text, entity.Links, entity.Media.Select(Map).ToList(), Map(entity.Author), entity.CommentCount, entity.LikeCount, entity.UpvoteCount, entity.IsHidden, entity.ModerationStatus, entity.ModerationReason, entity.CreatedAt, entity.UpdatedAt, Map(entity.RequestMetadata));
    private static AdminSocialCommentDto MapAdmin(SocialCommentDocument entity) => new(entity.Id, entity.PostId, entity.Text, entity.Media.Select(Map).ToList(), Map(entity.Author), entity.LikeCount, entity.UpvoteCount, entity.IsHidden, entity.ModerationStatus, entity.CreatedAt, Map(entity.RequestMetadata));
    private static AdminSocialReactionDto MapAdmin(SocialReactionDocument entity, IReadOnlyDictionary<Guid, SocialProfileDocument> profiles)
    {
        var user = profiles.TryGetValue(entity.UserId, out var profile)
            ? new SocialAuthorDto(profile.UserId, profile.Username, profile.DisplayName, profile.Role, profile.AvatarUrl, profile.Role is "Employer" or "Recruiter", string.Empty)
            : null;
        return new AdminSocialReactionDto(entity.Id, entity.TargetType, entity.TargetId, entity.UserId, entity.ReactionType, entity.CreatedAt, user, Map(entity.RequestMetadata));
    }
    private static AdminSocialConversationDto MapAdmin(SocialConversationDocument entity, long messageCount = 0, long unreadCount = 0) => new(entity.Id, entity.Participants.Select(Map).ToList(), entity.Status, entity.LastMessagePreview, entity.RequestedByUserId, entity.RequestedToUserId, entity.CreatedAt, entity.UpdatedAt, messageCount, unreadCount);
}
