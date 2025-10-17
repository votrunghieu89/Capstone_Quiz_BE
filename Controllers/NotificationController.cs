using Capstone.DTOs.Notification;
using Capstone.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Capstone.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationController : ControllerBase
    {
        private readonly ILogger<NotificationController> _logger;
        private readonly INotificationRepository _notificationService;

        public NotificationController(ILogger<NotificationController> logger, INotificationRepository notificationService)
        {
            _logger = logger;
            _notificationService = notificationService;
        }
        [HttpGet("latest/{accountId:int}")]
        public async Task<IActionResult> GetAllNotifications(int accountId)
        {
            try
            {
                if (accountId <= 0)
                {
                    _logger.LogWarning("Invalid accountId provided: {AccountId}", accountId);
                    return BadRequest(new { message = "Invalid account ID" });
                }

                var notification = await _notificationService.GetAllNotifications(accountId);

                if (notification == null)
                {
                    _logger.LogInformation("No notifications found for account: AccountId={AccountId}", accountId);
                    return NotFound(new { message = "No notifications found" });
                }

                return Ok(notification);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving notification: AccountId={AccountId}", accountId);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }
        [HttpPut("mark-read/{notificationId:int}")]
        public async Task<IActionResult> MarkAsRead(int notificationId)
        {
            try
            {
                if (notificationId <= 0)
                {
                    _logger.LogWarning("Invalid notificationId provided: {NotificationId}", notificationId);
                    return BadRequest(new { message = "Invalid notification ID" });
                }

                var success = await _notificationService.Mark(notificationId);

                if (success)
                {
                    _logger.LogInformation("Successfully marked notification as read: NotificationId={NotificationId}", notificationId);
                    return Ok(new { message = "Notification marked as read" });
                }
                else
                {
                    _logger.LogWarning("Failed to mark notification as read: NotificationId={NotificationId}", notificationId);
                    return NotFound(new { message = "Notification not found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking notification as read: NotificationId={NotificationId}", notificationId);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Mark a notification as unread
        /// </summary>
        [HttpPut("mark-unread/{notificationId:int}")]
        public async Task<IActionResult> MarkAsUnread(int notificationId)
        {
            try
            {
                if (notificationId <= 0)
                {
                    _logger.LogWarning("Invalid notificationId provided: {NotificationId}", notificationId);
                    return BadRequest(new { message = "Invalid notification ID" });
                }

                var success = await _notificationService.UnMark(notificationId);

                if (success)
                {
                    _logger.LogInformation("Successfully marked notification as unread: NotificationId={NotificationId}", notificationId);
                    return Ok(new { message = "Notification marked as unread" });
                }
                else
                {
                    _logger.LogWarning("Failed to mark notification as unread: NotificationId={NotificationId}", notificationId);
                    return NotFound(new { message = "Notification not found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking notification as unread: NotificationId={NotificationId}", notificationId);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Mark all notifications as read for a specific account
        /// </summary>
        [HttpPut("mark-all-read/{accountId:int}")]
        public async Task<IActionResult> MarkAllAsRead(int accountId)
        {
            try
            {
                if (accountId <= 0)
                {
                    _logger.LogWarning("Invalid accountId provided: {AccountId}", accountId);
                    return BadRequest(new { message = "Invalid account ID" });
                }

                var success = await _notificationService.MarkAll(accountId);

                if (success)
                {
                    _logger.LogInformation("Successfully marked all notifications as read: AccountId={AccountId}", accountId);
                    return Ok(new { message = "All notifications marked as read" });
                }
                else
                {
                    _logger.LogWarning("Failed to mark all notifications as read: AccountId={AccountId}", accountId);
                    return BadRequest(new { message = "Failed to mark notifications as read" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking all notifications as read: AccountId={AccountId}", accountId);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Mark all notifications as unread for a specific account
        /// </summary>
        [HttpPut("mark-all-unread/{accountId:int}")]
        public async Task<IActionResult> MarkAllAsUnread(int accountId)
        {
            try
            {
                if (accountId <= 0)
                {
                    _logger.LogWarning("Invalid accountId provided: {AccountId}", accountId);
                    return BadRequest(new { message = "Invalid account ID" });
                }

                var success = await _notificationService.UnMarkAll(accountId);

                if (success)
                {
                    _logger.LogInformation("Successfully marked all notifications as unread: AccountId={AccountId}", accountId);
                    return Ok(new { message = "All notifications marked as unread" });
                }
                else
                {
                    _logger.LogWarning("Failed to mark all notifications as unread: AccountId={AccountId}", accountId);
                    return BadRequest(new { message = "Failed to mark notifications as unread" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking all notifications as unread: AccountId={AccountId}", accountId);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        // ===== DELETE METHODS =====

        /// <summary>
        /// Delete all read notifications globally
        /// </summary>
        [HttpDelete("delete-all-read")]
        public async Task<IActionResult> DeleteAllReadNotifications()
        {
            try
            {
                var success = await _notificationService.DeleteNotification();

                if (success)
                {
                    _logger.LogInformation("Successfully deleted all read notifications");
                    return Ok(new { message = "All read notifications deleted successfully" });
                }
                else
                {
                    _logger.LogWarning("Failed to delete read notifications");
                    return BadRequest(new { message = "Failed to delete notifications" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting all read notifications");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }
    }
}
