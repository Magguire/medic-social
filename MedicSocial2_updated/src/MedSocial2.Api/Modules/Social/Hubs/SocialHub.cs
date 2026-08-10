using System.Security.Claims;
using MedSocial2.Api.Modules.Social.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Shared.Features;

namespace MedSocial2.Api.Modules.Social.Hubs;

public class SocialHub : Hub
{
    private readonly ISocialService _social;
    private readonly IPlatformFeatureService _features;

    public SocialHub(ISocialService social, IPlatformFeatureService features)
    {
        _social = social;
        _features = features;
    }

    public override async Task OnConnectedAsync()
    {
        if (!await _features.IsEnabledAsync("social", Context.ConnectionAborted))
        {
            Context.Abort();
            return;
        }

        if (Guid.TryParse(Context.User?.FindFirst("UserId")?.Value ?? Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{userId}");
            await _social.SetPresenceAsync(userId, "Online", Context.ConnectionAborted);
            await Clients.All.SendAsync("presenceChanged", new { userId, status = "Online" }, Context.ConnectionAborted);
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (Guid.TryParse(Context.User?.FindFirst("UserId")?.Value ?? Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId))
        {
            await _social.SetPresenceAsync(userId, "Offline", Context.ConnectionAborted);
            await Clients.All.SendAsync("presenceChanged", new { userId, status = "Offline" }, Context.ConnectionAborted);
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task JoinFeed(string channelSlug)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"feed:{(string.IsNullOrWhiteSpace(channelSlug) ? "global" : channelSlug)}");
    }

    public async Task JoinConversation(string conversationId)
    {
        if (!string.IsNullOrWhiteSpace(conversationId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"conversation:{conversationId}");
        }
    }

    [Authorize]
    public async Task SetPresence(string status)
    {
        if (Guid.TryParse(Context.User?.FindFirst("UserId")?.Value ?? Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId))
        {
            await _social.SetPresenceAsync(userId, status, Context.ConnectionAborted);
            await Clients.All.SendAsync("presenceChanged", new { userId, status }, Context.ConnectionAborted);
        }
    }
}
