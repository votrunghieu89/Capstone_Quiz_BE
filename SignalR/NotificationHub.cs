using Capstone.DTOs.Notification;
using Capstone.Repositories;
using Capstone.Repositories.Groups;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace Capstone.SignalR
{
    public class NotificationHub : Hub
    {
        private readonly ILogger<NotificationHub> _logger;
        private static readonly ConcurrentDictionary<string, List<string>> _UserConnection = new ConcurrentDictionary<string, List<string>>();
        private readonly INotificationRepository _notificationRepository;
 
        public NotificationHub(ILogger<NotificationHub> logger, INotificationRepository notificationRepository)
        {
            _logger = logger;
            _notificationRepository = notificationRepository;
           
        }

        public override Task OnConnectedAsync()
        {
            var accountId = Context.GetHttpContext()?.Request.Query["AccountId"].ToString();
            if (!string.IsNullOrEmpty(accountId))
            {
                //if (!_UserConnection.ContainsKey(accountId)) { 
                //        _UserConnection[accountId] = new List<string>();
                //}

                var connections = _UserConnection.GetOrAdd(accountId, _ => new List<string>());

                // 2 hàm trên  giống logic nhưng dưới an toàn cho multi-threading hơn
                lock (connections) { connections.Add(Context.ConnectionId); }
            }
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            var accountId = Context.GetHttpContext()?.Request.Query["AccountId"].ToString();
            if (!string.IsNullOrEmpty(accountId) && _UserConnection.TryGetValue(accountId, out var connections))
            {
                lock (connections)
                {
                    connections.Remove(Context.ConnectionId);

                    if (connections.Count == 0)
                    {
                        _UserConnection.TryRemove(accountId, out _);
                    }

                }
            }
            return base.OnDisconnectedAsync(exception);
        }
    }
}
