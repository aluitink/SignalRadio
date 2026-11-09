using Microsoft.AspNetCore.SignalR;

namespace SignalRadio.Api.Hubs;

public class TalkGroupHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var talkGroupId = Context.GetHttpContext()?.Request.Query["talkGroupId"];
        if (int.TryParse(talkGroupId, out var id))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"talkgroup-{id}");
        }
        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Join the group that receives a talk group's broadcasts (NewSummary).
    /// The client connects without a talkGroupId query string and joins
    /// explicitly, so it can join multiple talk groups on one connection.
    /// </summary>
    public Task JoinTalkGroup(int talkGroupId) =>
        Groups.AddToGroupAsync(Context.ConnectionId, $"talkgroup-{talkGroupId}");

    /// <summary>
    /// Join the group that receives live chat events for a specific investigation.
    /// Called by the investigation chat UI so it receives the streamed agent output
    /// (text deltas, tool calls, tool results, run finished/failed).
    /// </summary>
    public Task JoinInvestigation(long investigationId) =>
        Groups.AddToGroupAsync(Context.ConnectionId, $"investigation-{investigationId}");

    /// <summary>Leave an investigation's chat group (e.g. when the chat view unmounts).</summary>
    public Task LeaveInvestigation(long investigationId) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, $"investigation-{investigationId}");
}
