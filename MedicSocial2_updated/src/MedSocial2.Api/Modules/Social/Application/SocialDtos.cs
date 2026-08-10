namespace MedSocial2.Api.Modules.Social.Application;

public record SocialProfileDto(string? Id, Guid UserId, string Username, string DisplayName, string Role, string AvatarUrl, string Status, string Bio, DateTime? LastSeenAt);
public record SocialChannelDto(
    string? Id,
    string Name,
    string Slug,
    string Description,
    bool IsCommunity,
    bool IsActive,
    Guid CreatedByUserId,
    List<Guid> AdminUserIds,
    string JoinPolicy,
    string PostingPolicy,
    List<string> AllowedMediaTypes,
    List<string> VisibleToUserTypes,
    List<string> VisibleToCategories,
    List<string> VisibleToLocations,
    DateTime CreatedAt);
public record SocialMediaAssetDto(string Url, string FileName, string ContentType, long SizeBytes, string MediaType);
public record SocialAuthorDto(Guid? UserId, string Username, string DisplayName, string Role, string AvatarUrl, bool IsOrganization, string GuestTag);
public record SocialRequestMetadataDto(string DeviceId, string UserAgent, string IpAddress, string Source);
public record SocialPostDto(string? Id, string ChannelSlug, string Text, List<string> Links, List<SocialMediaAssetDto> Media, SocialAuthorDto Author, int CommentCount, int LikeCount, int UpvoteCount, string ModerationStatus, DateTime CreatedAt, DateTime UpdatedAt);
public record SocialCommentDto(string? Id, string PostId, string Text, List<SocialMediaAssetDto> Media, SocialAuthorDto Author, int LikeCount, int UpvoteCount, string ModerationStatus, DateTime CreatedAt);
public record SocialConversationDto(string? Id, List<SocialAuthorDto> Participants, string Status, string LastMessagePreview, Guid RequestedByUserId, Guid RequestedToUserId, DateTime CreatedAt, DateTime UpdatedAt, long UnreadCount = 0);
public record SocialMessageDto(string? Id, string ConversationId, Guid SenderUserId, SocialAuthorDto Sender, string Text, List<SocialMediaAssetDto> Media, DateTime CreatedAt, bool IsRead, DateTime? ReadAt = null, string DeliveryStatus = "DeliveredOffline");
public record SocialDirectoryUserDto(Guid UserId, string DisplayName, string UserType, string? Email, string? PhoneNumber, string Username, string AvatarUrl, string Status);
public record SocialReportDto(string? Id, string TargetType, string TargetId, Guid? ReporterUserId, string Reason, string Status, DateTime CreatedAt);
public record AdminSocialOverviewDto(
    long Profiles,
    long OnlineProfiles,
    long Channels,
    long ActiveChannels,
    long Posts,
    long HiddenPosts,
    long Comments,
    long HiddenComments,
    long OpenReports,
    long Conversations,
    long PendingConversations,
    DateTime GeneratedAt);
public record AdminSocialPostDto(
    string? Id,
    string ChannelSlug,
    string Text,
    List<string> Links,
    List<SocialMediaAssetDto> Media,
    SocialAuthorDto Author,
    int CommentCount,
    int LikeCount,
    int UpvoteCount,
    bool IsHidden,
    string ModerationStatus,
    string? ModerationReason,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    SocialRequestMetadataDto RequestMetadata);
public record AdminSocialCommentDto(
    string? Id,
    string PostId,
    string Text,
    List<SocialMediaAssetDto> Media,
    SocialAuthorDto Author,
    int LikeCount,
    int UpvoteCount,
    bool IsHidden,
    string ModerationStatus,
    DateTime CreatedAt,
    SocialRequestMetadataDto RequestMetadata);
public record AdminSocialReactionDto(
    string? Id,
    string TargetType,
    string TargetId,
    Guid UserId,
    string ReactionType,
    DateTime CreatedAt,
    SocialAuthorDto? User,
    SocialRequestMetadataDto RequestMetadata);
public record AdminSocialPostDetailDto(
    AdminSocialPostDto Post,
    SocialProfileDto? AuthorProfile,
    List<AdminSocialCommentDto> Comments,
    List<AdminSocialReactionDto> Reactions,
    List<SocialReportDto> Reports);
public record AdminSocialProfileDetailDto(
    SocialProfileDto Profile,
    List<AdminSocialPostDto> Posts,
    List<AdminSocialCommentDto> Comments,
    List<AdminSocialConversationDto> Conversations);
public record AdminSocialConversationDto(
    string? Id,
    List<SocialAuthorDto> Participants,
    string Status,
    string LastMessagePreview,
    Guid RequestedByUserId,
    Guid RequestedToUserId,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    long MessageCount = 0,
    long UnreadCount = 0);

public record UpsertSocialProfileRequest(string Username, string DisplayName, string AvatarUrl, string Status, string Bio);
public record CreateSocialChannelRequest(
    string Name,
    string Description,
    string JoinPolicy,
    string PostingPolicy,
    List<string>? AllowedMediaTypes,
    List<string>? VisibleToUserTypes,
    List<string>? VisibleToCategories,
    List<string>? VisibleToLocations);
public record CreatePostRequest(string ChannelSlug, string Text, List<string>? Links, List<SocialMediaAssetDto>? Media);
public record CreateCommentRequest(string Text, List<SocialMediaAssetDto>? Media);
public record ReactRequest(string ReactionType);
public record StartConversationRequest(Guid RecipientUserId, string Text, List<SocialMediaAssetDto>? Media);
public record SendMessageRequest(string Text, List<SocialMediaAssetDto>? Media);
public record ReportContentRequest(string TargetType, string TargetId, string Reason);
public record ModerationRequest(string Status, string? Reason);
public record UpdateSocialChannelRequest(
    string Name,
    string Description,
    bool IsActive,
    string JoinPolicy,
    string PostingPolicy,
    List<string>? AllowedMediaTypes,
    List<string>? VisibleToUserTypes,
    List<string>? VisibleToCategories,
    List<string>? VisibleToLocations);
public record ModerateReportRequest(string Status);
