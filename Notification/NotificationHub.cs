using Microsoft.AspNetCore.SignalR;

namespace Capstone.Notification
{
    public class NotificationHub : Hub
    {
        private readonly ILogger<NotificationHub> _logger;
        public NotificationHub(ILogger<NotificationHub> logger)
        {
            _logger = logger;
        }
        public override Task OnConnectedAsync()
        {
            _logger.LogInformation($"Client connected: {Context.ConnectionId}");
            return base.OnConnectedAsync();
        }
        public override Task OnDisconnectedAsync(Exception? exception)
        {
            _logger.LogInformation($"Client disconnected: {Context.ConnectionId}");
            return base.OnDisconnectedAsync(exception);
        }

        [HubMethodName("SendNotificationToUser")]
        public async Task SendNotificationToUser(string userId, string tittle, string message)
        {
            await Clients.User(userId).SendAsync("ReceiveNotification", tittle ,message);
        }
    }
}
