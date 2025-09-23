using Microsoft.AspNetCore.SignalR;

namespace Capstone.Notification
{
    public class QueryStringUserIdProvider : IUserIdProvider
    {
        public string? GetUserId(HubConnectionContext connection)
        {
            var accountId = connection.GetHttpContext()?.Request.Query["AccountId"].ToString();
            return accountId ?? connection.ConnectionId;
        }
    }
}
