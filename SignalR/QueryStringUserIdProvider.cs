using Microsoft.AspNetCore.SignalR;

namespace Capstone.SignalR
{
    public class QueryStringUserIdProvider
    {
        public string? GetUserId(HubConnectionContext connection)
        {
            var accountId = connection.GetHttpContext()?.Request.Query["AccountId"].ToString();
            return accountId ?? connection.ConnectionId;
        }
    }
}
