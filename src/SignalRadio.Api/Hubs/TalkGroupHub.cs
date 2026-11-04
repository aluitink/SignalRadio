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
}
