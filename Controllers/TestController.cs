using Capstone.Database;
using Capstone.Model.Others;
using Capstone.Notification;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace Capstone.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
    {
        private readonly ILogger<TestController> _logger;
        private readonly IHubContext<NotificationHub> _hub;
        private readonly AppDbContext _conText;
        public TestController(ILogger<TestController> logger, IHubContext<NotificationHub> hub, AppDbContext context)
        {
            _logger = logger;
            _hub = hub;
            _conText = context;
        }
        [Authorize]
        [HttpGet("send-notification")]
        public async Task<IActionResult> SendNotification(string userId, string title, string message)
        {
            // Lấy SenderId từ JWT
            long senderId = Convert.ToInt64(User.FindFirst("AccountId")?.Value);

            // Tạo notification model để lưu DB
            NotificationsModel newNotification = new NotificationsModel
            {
                Title = title,
                Message = message,
                Type = "Info",
                IsFavourite = 0,
                IsRead = 0,
                SenderId = Convert.ToInt32(senderId),
                ReceiverId = int.Parse(userId), // trong DB lưu kiểu số
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _hub.Clients.User(userId).SendAsync("ReceiveNotification", title, message);
            return Ok(new { Message = newNotification });
        }
    }
}
