using Capstone.Repositories;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace Capstone.SignalR
{
    public class NotificationHub : Hub
    {
        private readonly ILogger<NotificationHub> _logger;
        private readonly INotificationRepository _notificationRepository;

        // Lưu connectionId của từng user
        private static readonly ConcurrentDictionary<string, List<string>> _UserConnection = new();

        public NotificationHub(ILogger<NotificationHub> logger, INotificationRepository notificationRepository)
        {
            _logger = logger;
            _notificationRepository = notificationRepository;
        }

        public override Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier; // <-- đã dùng IUserIdProvider
            if (!string.IsNullOrEmpty(userId))
            {
                var connections = _UserConnection.GetOrAdd(userId, _ => new List<string>());
                lock (connections) connections.Add(Context.ConnectionId);
            }


            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.UserIdentifier;
            if (!string.IsNullOrEmpty(userId) && _UserConnection.TryGetValue(userId, out var connections))
            {
                lock (connections)
                {
                    connections.Remove(Context.ConnectionId);
                    if (connections.Count == 0) _UserConnection.TryRemove(userId, out _);
                }
            }
           
            return base.OnDisconnectedAsync(exception);
        }

        // Test gửi thông báo tới Account 4
        public async Task TestSendToUser4()
        {
            string targetUserId = "4";
            string message = $"[TEST] Hello User {targetUserId}! Time: {DateTime.Now:HH:mm:ss}";

            try
            {
                await Clients.User(targetUserId).SendAsync("GroupNotification", message);
                Console.WriteLine($"📤 Đã gửi test GroupNotification tới User {targetUserId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Lỗi khi gửi test: {ex.Message}");
            }
        }
    }
}
