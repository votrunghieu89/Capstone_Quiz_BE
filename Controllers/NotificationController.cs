using Capstone.DTOs.Notification;
using Capstone.ENUMs;
using Capstone.Model;
using Capstone.Model.Others;
using Capstone.Notification;
using Capstone.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace Capstone.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationController : ControllerBase
    {
        private readonly ILogger<NotificationController> _logger;
        private readonly INotificationRepository _notificationRepository;
        private readonly IHubContext<NotificationHub> _hub;
        private readonly ISupportRepository _supportRepository;
        public NotificationController(ILogger<NotificationController> logger,
            INotificationRepository notificationRepository, IHubContext<NotificationHub> hub, ISupportRepository supportRepository)
        {
            _logger = logger;
            _notificationRepository = notificationRepository;
            _hub = hub;
            _supportRepository = supportRepository;
        }
        // Get all notifications for an account
        [HttpGet("account/{accountId}")]
        [ProducesResponseType(typeof(List<NotificationsModel>), 200)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetNotificationsByAccountId(int accountId)
        {
            try
            {
                var check = await _notificationRepository.checkConnection();
                if (!check)
                {
                    _logger.LogError("Database connection failed.");
                    return StatusCode(500, "Database connection failed");
                }
                var notifications = await _notificationRepository.GetNotificationsByAccountId(accountId);
                return Ok(notifications);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving notifications for accountId: {AccountId}", accountId);
                return StatusCode(500, "Internal server error");
            }
        }

        // Get favourite notifications for an account
        [HttpGet("account/{accountId}/favourites")]
        [ProducesResponseType(typeof(List<NotificationsModel>), 200)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetFavouriteNotifications([FromRoute] int accountId)
        {
            try
            {
                var check = await _notificationRepository.checkConnection();
                if (!check)
                {
                    _logger.LogError("Database connection failed.");
                    return StatusCode(500, "Database connection failed");
                }
                var notifications = await _notificationRepository.GetFavouriteNotifications(accountId);
                return Ok(notifications);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving favourite notifications for accountId: {AccountId}", accountId);
                return StatusCode(500, "Internal server error");
            }
        }



        // Mark all notifications as read
        [HttpPost("account/{accountId}/mark-all-read")]
        [ProducesResponseType(200)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> MarkAllAsRead(int accountId)
        {
            try
            {
                var check = await _notificationRepository.checkConnection();
                if (!check)
                {
                    _logger.LogError("Database connection failed.");
                    return StatusCode(500, "Database connection failed");
                }
                var result = await _notificationRepository.MarkAllAsRead(accountId);
                if (result)
                {
                    return Ok(new { message = "Mark all" });
                }
                else
                {
                    return StatusCode(500, "Failed to mark all notifications as read");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking all notifications as read for accountId: {AccountId}", accountId);
                return StatusCode(500, "Internal server error");
            }
        }



        // Mark one notification as read
        [HttpPost("account/{accountId}/mark-read/{notificationId}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> MarkAsRead(int accountId, int notificationId)
        {
            try
            {
                var check = await _notificationRepository.checkConnection();
                if (!check)
                {
                    _logger.LogError("Database connection failed.");
                    return StatusCode(500, "Database connection failed");
                }
                var result = await _notificationRepository.MarkAsRead(accountId, notificationId);
                if (result)
                {
                    return Ok(new {message = "Mark" });
                }
                else
                {
                    return StatusCode(500, "Failed to mark the notification as read");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking notification as read for accountId: {AccountId}, notificationId: {NotificationId}", accountId, notificationId);
                return StatusCode(500, "Internal server error");
            }
        }

        // Unmark one notification
        [HttpPut("account/{accountId}/unmark/{notificationId}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> Unmark(int accountId, int notificationId)
        {
            try
            {
                var check = await _notificationRepository.checkConnection();
                if (!check)
                {
                    _logger.LogError("Database connection failed.");
                    return StatusCode(500, "Database connection failed");
                }
                var result = await _notificationRepository.Unmark(accountId, notificationId);
                if (result)
                {
                    return Ok(new { message = "Unmark" });
                }
                else
                {
                    return StatusCode(500, "Failed to unmark the notification");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unmarking notification for accountId: {AccountId}, notificationId: {NotificationId}", accountId, notificationId);
                return StatusCode(500, "Internal server error");
            }
        }

        // Unmark all notifications
        [HttpPut("account/{accountId}/unmark-all")]
        [ProducesResponseType(200)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> UnmarkAll(int accountId)
        {
            try
            {
                var check = await _notificationRepository.checkConnection();
                if (!check)
                {
                    _logger.LogError("Database connection failed.");
                    return StatusCode(500, "Database connection failed");
                }
                var result = await _notificationRepository.UnmarkAll(accountId);
                if (result)
                {
                    return Ok(new { message = "Unmark all" });
                }
                else
                {
                    return StatusCode(500, "Failed to unmark all notifications");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unmarking all notifications for accountId: {AccountId}", accountId);
                return StatusCode(500, "Internal server error");
            }
        }
        // Add to favourites
        [HttpPost("account/{accountId}/favourite/{notificationId}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> AddToFavourite(int accountId, int notificationId)
        {
            try
            {
                var check = await _notificationRepository.checkConnection();
                if (!check)
                {
                    _logger.LogError("Database connection failed.");
                    return StatusCode(500, "Database connection failed");
                }
                var result = await _notificationRepository.AddToFavourite(accountId, notificationId);
                if (result)
                {
                    return Ok(new { message = "Favourite" });
                }
                else
                {
                    return StatusCode(500, "Failed to add the notification to favourites");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding notification to favourites for accountId: {AccountId}, notificationId: {NotificationId}", accountId, notificationId);
                return StatusCode(500, "Internal server error");
            }
        }

        // Remove from favourites
        [HttpPut("account/{accountId}/favourite/{notificationId}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> RemoveFromFavourite(int accountId, int notificationId)
        {
            try
            {
                var check = await _notificationRepository.checkConnection();
                if (!check)
                {
                    _logger.LogError("Database connection failed.");
                    return StatusCode(500, "Database connection failed");
                }
                var result = await _notificationRepository.RemoveFromFavourite(accountId, notificationId);
                if (result)
                {
                    return Ok(new { message = "Favouritede" });
                }
                else
                {
                    return StatusCode(500, "Failed to remove the notification from favourites");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing notification from favourites for accountId: {AccountId}, notificationId: {NotificationId}", accountId, notificationId);
                return StatusCode(500, "Internal server error");
            }
        }

        // Delete all read notifications
        [HttpDelete("delete-read")]
        [ProducesResponseType(200)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> DeleteReadNotifications()
        {
            try
            {
                var check = await _notificationRepository.checkConnection();
                if (!check)
                {
                    _logger.LogError("Database connection failed.");
                    return StatusCode(500, "Database connection failed");
                }
                var result = await _notificationRepository.DeleteReadNotifications();
                if (result)
                {
                    return Ok(new {message = "Delete notifications successfully"});
                }
                else
                {
                    return NotFound(new { message = "No read notifications to delete" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting read notifications");
                return StatusCode(500, "Internal server error");
            }
        }

        // Count unread notifications
        [HttpGet("account/{accountId}/unread-count")]
        [ProducesResponseType(typeof(int), 200)]
        [ProducesResponseType(200)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> UnreadCount(int accountId)
        {
            try
            {
                var check = await _notificationRepository.checkConnection();
                if (!check)
                {
                    _logger.LogError("Database connection failed.");
                    return StatusCode(500, "Database connection failed");
                }
                var count = await _notificationRepository.UnreadCount(accountId);
                return Ok(count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error counting unread notifications for accountId: {AccountId}", accountId);
                return StatusCode(500, "Internal server error");
            }
        }


        // SubmitCV
        [HttpPost("submit-cv")]
        [ProducesResponseType(200)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> SubmitCV([FromBody] SubmitCVDTO submitCV)
        {
            try
            {
                var check = await _notificationRepository.checkConnection();
                if (!check)
                {
                    _logger.LogError("Database connection failed.");
                    return StatusCode(500, "Database connection failed");
                }

                CV_JD_ApplyModel cV_JD_ApplyModel = new CV_JD_ApplyModel
                {
                    CVId = submitCV.CVId,
                    JDId = submitCV.JDId,
                    Status = "Pending",
                    ReviewedDate = null,
                    CreatedAt = DateTime.Now,
                };

                var result = await _notificationRepository.SubmitCV(cV_JD_ApplyModel);
                switch (result)
                {
                    case SubmitResult.Success:
                        var companyInfo = await _supportRepository.NotificationEvaluateCVModel(submitCV.JDId); // trả về AccountId, Title và CompanyName
                        var candidateInfo = await _supportRepository.NotificationSubmitCVModel(submitCV.CVId); // trả về AccountId và FullName
                        var saveNotify = new NotificationSaveModel
                        {
                           Title = "New CV Submission",
                           Message = $"A new CV from {candidateInfo.FullName} has been submitted for your job posting {companyInfo.Title}.",
                           Type = "SubmitCV",
                           IsFavourite = 0,
                           IsRead = 0,
                           SenderId = candidateInfo.AccountId,
                           ReceiverId = companyInfo.AccountId,
                        };
                        var isSaveNotify = await _notificationRepository.SaveNotification(saveNotify);
                        if (!isSaveNotify)
                        {
                            _logger.LogError("Failed to save notification to database for companyId: {CompanyId}", companyInfo.AccountId);
                            return StatusCode(500, "Failed to save notification to database");
                        }
                        await _hub.Clients.User(saveNotify.ReceiverId.ToString()).SendAsync(
                            "ReceiveNotification",saveNotify.Title, saveNotify.Message);

                        return Ok(new { message = "CV submitted successfully" });

                    case SubmitResult.AlreadyPending:
                        return Conflict(new { message = "You already have a pending CV for this JD" });

                    case SubmitResult.AlreadyApproved:
                        return Conflict(new { message = "Your CV for this JD has already been approved" });

                    case SubmitResult.Error:
                    default:
                        return StatusCode(500, new { message = "Failed to submit CV" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting CV for JDId: {JDId}, ApplicantId: {ApplicantId}", submitCV.JDId, submitCV.CVId);
                return StatusCode(500, "Internal server error");
            }
        }

        // RejectCV
        [HttpPut("reject-cv")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> RejectCV([FromBody] SubmitCVDTO rejectCV)
        {
            try
            {
                var check = await _notificationRepository.checkConnection();
                if (!check)
                {
                    _logger.LogError("Database connection failed.");
                    return StatusCode(500, "Database connection failed");
                }
                CV_JD_ApplyModel cV_JD_ApplyModel = new CV_JD_ApplyModel
                {
                    CVId = rejectCV.CVId,
                    JDId = rejectCV.JDId,
                    Status = "Reject",
                    ReviewedDate = DateTime.Now,
                };
                var result = await _notificationRepository.RejectCV(cV_JD_ApplyModel);
                if (result)
                {
                    var companyInfo = await _supportRepository.NotificationEvaluateCVModel(rejectCV.JDId); // trả về AccountId, Title và CompanyName
                    var candidateInfo = await _supportRepository.NotificationSubmitCVModel(rejectCV.CVId); // trả về AccountId và FullName
                    var saveNotify = new NotificationSaveModel
                    {
                        Title = "The employer has reviewed your profile.",
                        Message = $"Your CV for the job posting {companyInfo.Title} from {companyInfo.CompanyName} has been rejected.",
                        Type = "Result",
                        IsFavourite = 0,
                        IsRead = 0,
                        SenderId = companyInfo.AccountId,
                        ReceiverId = candidateInfo.AccountId,
                    };
                    var isSaveNotify = await _notificationRepository.SaveNotification(saveNotify);
                    if (!isSaveNotify)
                    {
                        _logger.LogError("Failed to save notification to database for companyId: {CompanyId}", companyInfo.AccountId);
                        return StatusCode(500, "Failed to save notification to database");
                    }
                    await _hub.Clients.User(saveNotify.ReceiverId.ToString()).SendAsync(
                        "ReceiveNotification", saveNotify.Title,saveNotify.Message);
                    return Ok(new { message = "CV rejected successfully" });
                }
                else
                {
                    return StatusCode(500, new { message = "Failed to reject CV" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting CV for JDId: {JDId}, ApplicantId: {ApplicantId}", rejectCV.JDId, rejectCV.CVId);
                return StatusCode(500, "Internal server error");
            }
        }

        // Approve CV
        [HttpPut("approve-cv")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> ApproveCV([FromBody] SubmitCVDTO approveCV)
        {
            try
            {
                var check = await _notificationRepository.checkConnection();
                if (!check)
                {
                    _logger.LogError("Database connection failed.");
                    return StatusCode(500, "Database connection failed");
                }
                CV_JD_ApplyModel cV_JD_ApplyModel = new CV_JD_ApplyModel
                {
                    CVId = approveCV.CVId,
                    JDId = approveCV.JDId,
                    Status = "Approve",
                    ReviewedDate = DateTime.Now,
                };
                var result = await _notificationRepository.ApproveCV(cV_JD_ApplyModel);
                if (result)
                {
                    var companyInfo = await _supportRepository.NotificationEvaluateCVModel(approveCV.JDId); // trả về AccountId, Title và CompanyName
                    var candidateInfo = await _supportRepository.NotificationSubmitCVModel(approveCV.CVId); // trả về AccountId và FullName
                    var saveNotify = new NotificationSaveModel
                    {
                        Title = "The employer has reviewed your profile.",
                        Message = $"Your CV for the job posting {companyInfo.Title} from {companyInfo.CompanyName} has been approved.",
                        Type = "Result",
                        IsFavourite = 0,
                        IsRead = 0,
                        SenderId = companyInfo.AccountId,
                        ReceiverId = candidateInfo.AccountId,
                    };
                    var isSaveNotify = await _notificationRepository.SaveNotification(saveNotify);
                    if (!isSaveNotify)
                    {
                        _logger.LogError("Failed to save notification to database for companyId: {CompanyId}", companyInfo.AccountId);
                        return StatusCode(500, "Failed to save notification to database");
                    }
                    // Sẽ có 1 hàm lấy tittle JD từ JDId
                    await _hub.Clients.User(saveNotify.ReceiverId.ToString()).SendAsync(
                        "ReceiveNotification", saveNotify.Title, saveNotify.Message);
                    return Ok(new { message = "CV approved successfully" });
                }
                else
                {
                    return StatusCode(500, new { message = "Failed to approve CV" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving CV for JDId: {JDId}, ApplicantId: {ApplicantId}", approveCV.JDId, approveCV.CVId);
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
