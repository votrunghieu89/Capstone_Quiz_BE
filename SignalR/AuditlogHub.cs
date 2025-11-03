using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace Capstone.SignalR
{
    public class AuditlogHub : Hub
    {
        private readonly ILogger<AuditlogHub> _logger;
        private static readonly ConcurrentDictionary<string, List<string>> _UserConnection = new();

        public AuditlogHub(ILogger<AuditlogHub> logger)
        {
            _logger = logger;
        }

        public override Task OnConnectedAsync()
        {
            var accountId = Context.UserIdentifier; 
            if (!string.IsNullOrEmpty(accountId))
            {
                var connections = _UserConnection.GetOrAdd(accountId, _ => new List<string>());
                lock (connections)
                {
                    connections.Add(Context.ConnectionId);
                }
            }
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            var accountId = Context.UserIdentifier; // <- dùng QueryStringUserIdProvider
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
