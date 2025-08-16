using Microsoft.AspNetCore.SignalR;

namespace SignalRadio.Api.Hubs;

public class TalkGroupHub : Hub
{
    private readonly ILogger<TalkGroupHub> _logger;

    public TalkGroupHub(ILogger<TalkGroupHub> logger)
    {
        _logger = logger;
    }

    public async Task SubscribeToTalkGroup(string talkGroupId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"talkgroup_{talkGroupId}");
        _logger.LogInformation("Client {ConnectionId} subscribed to talk group {TalkGroupId}", 
            Context.ConnectionId, talkGroupId);
        
        await Clients.Caller.SendAsync("SubscriptionConfirmed", talkGroupId);
    }

    public async Task UnsubscribeFromTalkGroup(string talkGroupId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"talkgroup_{talkGroupId}");
        _logger.LogInformation("Client {ConnectionId} unsubscribed from talk group {TalkGroupId}", 
            Context.ConnectionId, talkGroupId);
        
        await Clients.Caller.SendAsync("UnsubscriptionConfirmed", talkGroupId);
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client {ConnectionId} connected to TalkGroupHub", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client {ConnectionId} disconnected from TalkGroupHub. Exception: {Exception}", 
            Context.ConnectionId, exception?.Message);
        await base.OnDisconnectedAsync(exception);
    }
}
