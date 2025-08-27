using Microsoft.AspNetCore.SignalR;

namespace Notifico.Hubs
{
    public class NotificationHub : Hub
    {
        
        public async Task SendOrderNotification(string message)
        {
            await Clients.All.SendAsync("ReceiveOrderNotification",message);
        }

    }
}
