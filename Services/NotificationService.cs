using Capstone.Database;
using Capstone.DTOs.Notification;
using Capstone.ENUMs;
using Capstone.Model;
using Capstone.Model.Others;
using Capstone.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Capstone.Services
{
    public class NotificationService : INotificationRepository
    {
        public readonly AppDbContext _context;
        private readonly ILogger<NotificationService> _logger;
        public NotificationService(AppDbContext context, ILogger<NotificationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<bool> checkConnection()
        {
            try
            {
                return await _context.Database.CanConnectAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking database connection");
                return false;
            }
        }
        public async Task<bool> AddToFavourite(int accountId, int notificationId)
        {
            try
            {
                int updatedCount = await _context.notificationsModels
                    .Where(n => n.ReceiverId == accountId && n.NotificationId == notificationId)
                    .ExecuteUpdateAsync(n => n.SetProperty(n => n.IsFavourite, 1)
                                            .SetProperty(n => n.UpdatedAt, DateTime.UtcNow));
                if (updatedCount > 0)
                {
                    _logger.LogInformation($"Added notification ID {notificationId} to favourites for account ID {accountId}");
                    return true;
                }
                else
                {
                    _logger.LogInformation($"No notification found with ID {notificationId} for account ID {accountId}");
                    return false;
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding notification to favourites");
                return false;
            }
        }

        public async Task<bool> DeleteReadNotifications()
        {
            try
            {
                int deletedCouynt = await _context.notificationsModels
                    .Where(n => n.IsRead == 1 && n.IsFavourite == 0)
                    .ExecuteDeleteAsync();

                if (deletedCouynt > 0)
                {
                    _logger.LogInformation($"Deleted {deletedCouynt} read notifications");
                    return true;
                }
                else
                {
                    _logger.LogInformation("No read notifications to delete");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting read notifications");
                return false;
            }
        }

        public async Task<List<NotificationsModel>> GetNotificationsByAccountId(int accountId)
        {
            try
            {
                List<NotificationsModel> notifications = await _context.notificationsModels
                    .Where(n => n.ReceiverId == accountId)
                    .OrderByDescending(n => n.CreatedAt)
                    .ToListAsync();
                if (notifications.Count > 0)
                {
                    _logger.LogInformation($"Retrieved {notifications.Count} notifications for account ID {accountId}");
                    return notifications;
                }
                else
                {
                    _logger.LogInformation($"No notifications found for account ID {accountId}");
                    return new List<NotificationsModel>();
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting notifications by account ID");
                return new List<NotificationsModel>();
            }
        }

        public async Task<bool> MarkAllAsRead(int accountId)
        {
            try
            {
                int updatedCount = await _context.notificationsModels
                    .Where(n => n.ReceiverId == accountId)
                    .ExecuteUpdateAsync(n => n.SetProperty(n => n.IsRead, 1)
                                            .SetProperty(n => n.UpdatedAt, DateTime.UtcNow));
                if (updatedCount > 0)
                {
                    _logger.LogInformation($"Marked {updatedCount} notifications as read for account ID {accountId}");
                    return true;
                }
                else
                {
                    _logger.LogInformation($"No unread notifications to mark as read for account ID {accountId}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking all notifications as read");
                return false;
            }
        }

        public async Task<bool> MarkAsRead(int accountId, int notificationId)
        {
            try
            {
                int updatedCount = await _context.notificationsModels
                    .Where(n => n.ReceiverId == accountId && n.NotificationId == notificationId)
                    .ExecuteUpdateAsync(n => n.SetProperty(n => n.IsRead, 1)
                                            .SetProperty(n => n.UpdatedAt, DateTime.UtcNow));
                if (updatedCount > 0)
                {
                    _logger.LogInformation($"Marked notification ID {notificationId} as read for account ID {accountId}");
                    return true;
                }
                else
                {
                    _logger.LogInformation($"No unread notification found with ID {notificationId} for account ID {accountId}");
                    return false;
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking notification as read");
                return false;
            }
        }

        public async Task<bool> RemoveFromFavourite(int accountId, int notificationId)
        {
            try
            {
                int updatedCount = await _context.notificationsModels
                    .Where(n => n.ReceiverId == accountId && n.NotificationId == notificationId)
                    .ExecuteUpdateAsync(n => n.SetProperty(n => n.IsFavourite, 0)
                                            .SetProperty(n => n.UpdatedAt, DateTime.UtcNow));
                if (updatedCount > 0)
                {
                    _logger.LogInformation($"Removed notification ID {notificationId} from favourites for account ID {accountId}");
                    return true;
                }
                else
                {
                    _logger.LogInformation($"No favourite notification found with ID {notificationId} for account ID {accountId}");
                    return false;
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing notification from favourites");
                return false;
            }
        }

        public async Task<bool> SaveNotification(NotificationSaveModel notificationSaveModel)
        {
            try
            {
                await _context.notificationsModels.AddAsync(new NotificationsModel
                {
                    Title = notificationSaveModel.Title,
                    Message = notificationSaveModel.Message,
                    Type = notificationSaveModel.Type,
                    IsFavourite = 0,
                    IsRead = 0,
                    SenderId = notificationSaveModel.SenderId,
                    ReceiverId = notificationSaveModel.ReceiverId, // trong DB lưu kiểu số
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
                int result = await _context.SaveChangesAsync();
                if (result > 0)
                {
                    _logger.LogInformation("Notification saved successfully");
                    return true;
                }
                else
                {
                    _logger.LogWarning("No changes were made to the database when saving notification");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving notification");
                return false;
            }
        }

        public async Task<List<NotificationsModel>> GetFavouriteNotifications(int accountId)
        {
            try
            {
                List<NotificationsModel> favouriteNotifications = await _context.notificationsModels
                    .Where(n => n.ReceiverId == accountId && n.IsFavourite == 1)
                    .OrderByDescending(n => n.CreatedAt)
                    .ToListAsync();
                if (favouriteNotifications.Count > 0)
                {
                    _logger.LogInformation($"Retrieved {favouriteNotifications.Count} favourite notifications for account ID {accountId}");
                    return favouriteNotifications;
                }
                else
                {
                    _logger.LogInformation($"No favourite notifications found for account ID {accountId}");
                    return new List<NotificationsModel>();
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting favourite notifications");
                return new List<NotificationsModel>();

            }
        }

        public async Task<int> UnreadCount(int accountId)
        {
            try
            {
                int count = await _context.notificationsModels
                    .Where(n => n.ReceiverId == accountId && n.IsRead == 0)
                    .CountAsync();
                _logger.LogInformation($"Account ID {accountId} has {count} unread notifications");
                return count;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error counting unread notifications");
                return 0;
            }
        }

        public async Task<SubmitResult> SubmitCV(CV_JD_ApplyModel cV_JD_ApplyModel)
        {
            try
            {
                var existingApply = await _context.cV_JD_Applies
                                   .Where(x => x.CVId == cV_JD_ApplyModel.CVId && x.JDId == cV_JD_ApplyModel.JDId)
                                   .OrderByDescending(x => x.CreatedAt)
                                   .FirstOrDefaultAsync();
                if (existingApply.Status == "Pending")
                {
                    _logger.LogInformation($"Cannot reject CV with Apply ID {cV_JD_ApplyModel.ApplyId} and CV ID {cV_JD_ApplyModel.CVId} because it is still pending");
                    return SubmitResult.AlreadyPending;
                }

                if (existingApply.Status == "Approve")
                {
                    _logger.LogInformation($"Cannot reject CV with Apply ID {cV_JD_ApplyModel.ApplyId} and CV ID {cV_JD_ApplyModel.CVId} because it has been approved");
                    return SubmitResult.AlreadyApproved;
                }
                await _context.cV_JD_Applies.AddAsync(cV_JD_ApplyModel);
                int result = await _context.SaveChangesAsync();
                if (result > 0)
                {
                    _logger.LogInformation("CV submitted successfully");
                    return SubmitResult.Success;
                }
                else
                {
                    _logger.LogWarning("No changes were made to the database when submitting CV");
                    return SubmitResult.Error;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting CV");
                return SubmitResult.Error;
            }
        }

        public async Task<bool> ApproveCV(CV_JD_ApplyModel cV_JD_ApplyModel)
        {
            try
            {
                int updatedCount = await _context.cV_JD_Applies
                    .Where(c => c.ApplyId == cV_JD_ApplyModel.ApplyId && c.CVId == cV_JD_ApplyModel.CVId)
                    .ExecuteUpdateAsync(c => c.SetProperty(c => c.Status, "Approve")
                                              .SetProperty(c => c.ReviewedDate, DateTime.UtcNow));
                if (updatedCount > 0)
                {
                    _logger.LogInformation($"Approved CV with Apply ID {cV_JD_ApplyModel.ApplyId} and CV ID {cV_JD_ApplyModel.CVId}");
                    return true;
                }
                else
                {
                    _logger.LogInformation($"No CV found with Apply ID {cV_JD_ApplyModel.ApplyId} and CV ID {cV_JD_ApplyModel.CVId} to approve");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving CV");
                return false;
            }
        }

        public async Task<bool> RejectCV(CV_JD_ApplyModel cV_JD_ApplyModel)
        {
            try
            {

                int updatedCount = await _context.cV_JD_Applies
                    .Where(c => c.ApplyId == cV_JD_ApplyModel.ApplyId && c.CVId == cV_JD_ApplyModel.CVId)
                    .ExecuteUpdateAsync(c => c.SetProperty(c => c.Status, "Reject")
                                              .SetProperty(c => c.ReviewedDate, DateTime.UtcNow));
                if (updatedCount > 0)
                {
                    _logger.LogInformation($"Rejected CV with Apply ID {cV_JD_ApplyModel.ApplyId} and CV ID {cV_JD_ApplyModel.CVId}");
                    return true;
                }
                else
                {
                    _logger.LogInformation($"No CV found with Apply ID {cV_JD_ApplyModel.ApplyId} and CV ID {cV_JD_ApplyModel.CVId} to reject");
                    return false;
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting CV");
                return false;
            }
        }

        public async Task<bool> Unmark(int accountId, int notificationId)
        {
            try
            {
                int updatedCount = await _context.notificationsModels
                    .Where(n => n.ReceiverId == accountId && n.NotificationId == notificationId)
                    .ExecuteUpdateAsync(n => n.SetProperty(n => n.IsRead, 0)
                                            .SetProperty(n => n.UpdatedAt, DateTime.UtcNow));
                if (updatedCount > 0)
                {
                    _logger.LogInformation($"Unmarked notification ID {notificationId} as read for account ID {accountId}");
                    return true;
                }
                else
                {
                    _logger.LogInformation($"No read notification found with ID {notificationId} for account ID {accountId}");
                    return false;
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unmarking notification");
                return false;
            }
        }

        public async Task<bool> UnmarkAll(int accountId)
        {
            try
            {
                int updatedCount = await _context.notificationsModels
                    .Where(n => n.ReceiverId == accountId)
                    .ExecuteUpdateAsync(n => n.SetProperty(n => n.IsRead, 0)
                                            .SetProperty(n => n.UpdatedAt, DateTime.UtcNow));
                if (updatedCount > 0)
                {
                    _logger.LogInformation($"Unmarked {updatedCount} notifications as read for account ID {accountId}");
                    return true;
                }
                else
                {
                    _logger.LogInformation($"No read notifications to unmark for account ID {accountId}");
                    return false;
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unmarking all notifications");
                return false;
            }
        }
    }
}
