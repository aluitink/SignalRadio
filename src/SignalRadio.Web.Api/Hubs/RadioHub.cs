using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace SignalRadio.Web.Api.Hubs
{
    public class RadioHub : Hub
    {
        public override async Task OnConnectedAsync() 
        {
            await Task.Delay(10);
        }
        public async Task Send(string name, string message)
        {
            // Call the broadcastMessage method to update clients.
            await Clients.All.SendAsync("broadcastMessage", name, message);
        }
    }
}