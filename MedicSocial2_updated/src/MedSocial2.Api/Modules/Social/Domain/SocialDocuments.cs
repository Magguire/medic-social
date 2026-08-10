using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MedSocial2.Api.Modules.Social.Domain;

public class SocialProfileDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }
    [BsonGuidRepresentation(GuidRepresentation.Standard)]
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;
    public string Status { get; set; } = "Offline";
    public string Bio { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastSeenAt { get; set; }
}

public class SocialChannelDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsCommunity { get; set; }
    public bool IsActive { get; set; } = true;
    [BsonGuidRepresentation(GuidRepresentation.Standard)]
    public Guid CreatedByUserId { get; set; }
    [BsonGuidRepresentation(GuidRepresentation.Standard)]
    public List<Guid> AdminUserIds { get; set; } = [];
    public string JoinPolicy { get; set; } = "Anyone";
    public string PostingPolicy { get; set; } = "Anyone";
    public List<string> AllowedMediaTypes { get; set; } = ["text", "image", "video", "file", "link"];
    public List<string> VisibleToUserTypes { get; set; } = [];
    public List<string> VisibleToCategories { get; set; } = [];
    public List<string> VisibleToLocations { get; set; } = [];
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class SocialMediaAsset
{
    public string Url { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string MediaType { get; set; } = "file";
}

public class SocialAuthorSnapshot
{
    [BsonGuidRepresentation(GuidRepresentation.Standard)]
    public Guid? UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Role { get; set; } = "Guest";
    public string AvatarUrl { get; set; } = string.Empty;
    public bool IsOrganization { get; set; }
    public string GuestTag { get; set; } = string.Empty;
}

public class SocialRequestMetadata
{
    public string DeviceId { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string Source { get; set; } = "web";
}

public class SocialPostDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }
    public string ChannelSlug { get; set; } = "global";
    public string Text { get; set; } = string.Empty;
    public List<string> Links { get; set; } = [];
    public List<SocialMediaAsset> Media { get; set; } = [];
    public SocialAuthorSnapshot Author { get; set; } = new();
    public SocialRequestMetadata RequestMetadata { get; set; } = new();
    public int CommentCount { get; set; }
    public int LikeCount { get; set; }
    public int UpvoteCount { get; set; }
    public bool IsHidden { get; set; }
    public string ModerationStatus { get; set; } = "Visible";
    public string? ModerationReason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class SocialCommentDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }
    public string PostId { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public List<SocialMediaAsset> Media { get; set; } = [];
    public SocialAuthorSnapshot Author { get; set; } = new();
    public SocialRequestMetadata RequestMetadata { get; set; } = new();
    public int LikeCount { get; set; }
    public int UpvoteCount { get; set; }
    public bool IsHidden { get; set; }
    public string ModerationStatus { get; set; } = "Visible";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class SocialReactionDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }
    public string TargetType { get; set; } = string.Empty;
    public string TargetId { get; set; } = string.Empty;
    [BsonGuidRepresentation(GuidRepresentation.Standard)]
    public Guid UserId { get; set; }
    public string ReactionType { get; set; } = string.Empty;
    public SocialRequestMetadata RequestMetadata { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class SocialConversationDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }
    [BsonGuidRepresentation(GuidRepresentation.Standard)]
    public List<Guid> ParticipantUserIds { get; set; } = [];
    public List<SocialAuthorSnapshot> Participants { get; set; } = [];
    [BsonGuidRepresentation(GuidRepresentation.Standard)]
    public Guid RequestedByUserId { get; set; }
    [BsonGuidRepresentation(GuidRepresentation.Standard)]
    public Guid RequestedToUserId { get; set; }
    public string Status { get; set; } = "Pending";
    public string LastMessagePreview { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? AcceptedAt { get; set; }
}

public class SocialMessageDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }
    public string ConversationId { get; set; } = string.Empty;
    [BsonGuidRepresentation(GuidRepresentation.Standard)]
    public Guid SenderUserId { get; set; }
    public SocialAuthorSnapshot Sender { get; set; } = new();
    public string Text { get; set; } = string.Empty;
    public List<SocialMediaAsset> Media { get; set; } = [];
    public SocialRequestMetadata RequestMetadata { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
}

public class SocialReportDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }
    public string TargetType { get; set; } = string.Empty;
    public string TargetId { get; set; } = string.Empty;
    [BsonGuidRepresentation(GuidRepresentation.Standard)]
    public Guid? ReporterUserId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = "Open";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
