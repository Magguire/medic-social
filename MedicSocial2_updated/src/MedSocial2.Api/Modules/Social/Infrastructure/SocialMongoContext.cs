using MedSocial2.Api.Modules.Social.Domain;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace MedSocial2.Api.Modules.Social.Infrastructure;

public class SocialMongoOptions
{
    public string ConnectionString { get; set; } = "mongodb://localhost:27017";
    public string DatabaseName { get; set; } = "MedSocial2Social";
    public string PublicMediaBaseUrl { get; set; } = "";
}

public class SocialMongoContext
{
    private readonly IMongoDatabase _database;

    public SocialMongoContext(IOptions<SocialMongoOptions> options)
    {
        var settings = options.Value;
        var client = new MongoClient(settings.ConnectionString);
        _database = client.GetDatabase(settings.DatabaseName);
    }

    public IMongoCollection<SocialProfileDocument> Profiles => _database.GetCollection<SocialProfileDocument>("social_profiles");
    public IMongoCollection<SocialChannelDocument> Channels => _database.GetCollection<SocialChannelDocument>("social_channels");
    public IMongoCollection<SocialPostDocument> Posts => _database.GetCollection<SocialPostDocument>("social_posts");
    public IMongoCollection<SocialCommentDocument> Comments => _database.GetCollection<SocialCommentDocument>("social_comments");
    public IMongoCollection<SocialReactionDocument> Reactions => _database.GetCollection<SocialReactionDocument>("social_reactions");
    public IMongoCollection<SocialConversationDocument> Conversations => _database.GetCollection<SocialConversationDocument>("social_conversations");
    public IMongoCollection<SocialMessageDocument> Messages => _database.GetCollection<SocialMessageDocument>("social_messages");
    public IMongoCollection<SocialReportDocument> Reports => _database.GetCollection<SocialReportDocument>("social_reports");

    public async Task EnsureCreatedAsync()
    {
        await Profiles.Indexes.CreateOneAsync(new CreateIndexModel<SocialProfileDocument>(
            Builders<SocialProfileDocument>.IndexKeys.Ascending(x => x.Username),
            new CreateIndexOptions { Unique = true }));
        await Profiles.Indexes.CreateOneAsync(new CreateIndexModel<SocialProfileDocument>(
            Builders<SocialProfileDocument>.IndexKeys.Ascending(x => x.UserId),
            new CreateIndexOptions { Unique = true }));
        await Channels.Indexes.CreateOneAsync(new CreateIndexModel<SocialChannelDocument>(
            Builders<SocialChannelDocument>.IndexKeys.Ascending(x => x.Slug),
            new CreateIndexOptions { Unique = true }));
        await Posts.Indexes.CreateOneAsync(new CreateIndexModel<SocialPostDocument>(
            Builders<SocialPostDocument>.IndexKeys.Descending(x => x.CreatedAt)));
        await Posts.Indexes.CreateOneAsync(new CreateIndexModel<SocialPostDocument>(
            Builders<SocialPostDocument>.IndexKeys.Ascending(x => x.ChannelSlug).Descending(x => x.CreatedAt)));
        await Comments.Indexes.CreateOneAsync(new CreateIndexModel<SocialCommentDocument>(
            Builders<SocialCommentDocument>.IndexKeys.Ascending(x => x.PostId).Descending(x => x.CreatedAt)));
        await Reactions.Indexes.CreateOneAsync(new CreateIndexModel<SocialReactionDocument>(
            Builders<SocialReactionDocument>.IndexKeys.Ascending(x => x.TargetType).Ascending(x => x.TargetId).Ascending(x => x.UserId).Ascending(x => x.ReactionType),
            new CreateIndexOptions { Unique = true }));
        await Conversations.Indexes.CreateOneAsync(new CreateIndexModel<SocialConversationDocument>(
            Builders<SocialConversationDocument>.IndexKeys.Ascending(x => x.ParticipantUserIds).Descending(x => x.UpdatedAt)));
        await Messages.Indexes.CreateOneAsync(new CreateIndexModel<SocialMessageDocument>(
            Builders<SocialMessageDocument>.IndexKeys.Ascending(x => x.ConversationId).Descending(x => x.CreatedAt)));
        await Reports.Indexes.CreateOneAsync(new CreateIndexModel<SocialReportDocument>(
            Builders<SocialReportDocument>.IndexKeys.Ascending(x => x.Status).Descending(x => x.CreatedAt)));
    }
}
