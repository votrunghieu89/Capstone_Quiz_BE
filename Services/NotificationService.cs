using Capstone.Database;
using Capstone.DTOs.Notification;
using Capstone.Model;
using Capstone.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Capstone.Services
{
    public class NotificationService : INotificationRepository
    {
        private readonly ILogger<NotificationService> _logger;
        private readonly AppDbContext _dbContext;

        public NotificationService(ILogger<NotificationService> logger, AppDbContext dbContext)
        {
            _logger = logger;
            _dbContext = dbContext;
        }
        public async Task<bool> Mark(int notificationId)
        {
            try
            {
                int updatedRows = await _dbContext.notifications
                    .Where(n => n.NotificationId == notificationId)
                    .ExecuteUpdateAsync(n => n.SetProperty(x => x.IsRead, true));

                if (updatedRows > 0)
                {
                    _logger.LogInformation("Successfully marked notification as read: NotificationId={NotificationId}", notificationId);
                    return true;
                }
                else
                {
                    _logger.LogWarning("Notification not found for marking as read: NotificationId={NotificationId}", notificationId);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking notification as read: NotificationId={NotificationId}", notificationId);
                return false;
            }
        }
        public async Task<bool> UnMark(int notificationId)
        {
            try
            {
                int updatedRows = await _dbContext.notifications
                    .Where(n => n.NotificationId == notificationId)
                    .ExecuteUpdateAsync(n => n.SetProperty(x => x.IsRead, false));

                if (updatedRows > 0)
                {
                    _logger.LogInformation("Successfully marked notification as unread: NotificationId={NotificationId}", notificationId);
                    return true;
                }
                else
                {
                    _logger.LogWarning("Notification not found for marking as unread: NotificationId={NotificationId}", notificationId);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking notification as unread: NotificationId={NotificationId}", notificationId);
                return false;
            }
        }
        public async Task<bool> MarkAll(int accountId)
        {
            try
            {
                int updatedRows = await _dbContext.notifications
                    .Where(n => n.ReceiverId == accountId && n.IsRead == false)
                    .ExecuteUpdateAsync(n => n.SetProperty(x => x.IsRead, true));

                _logger.LogInformation("Marked {Count} notifications as read for account: AccountId={AccountId}",
                    updatedRows, accountId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking all notifications as read: AccountId={AccountId}", accountId);
                return false;
            }
        }
        public async Task<bool> UnMarkAll(int accountId)
        {
            try
            {
                int updatedRows = await _dbContext.notifications
                    .Where(n => n.ReceiverId == accountId && n.IsRead == true)
                    .ExecuteUpdateAsync(n => n.SetProperty(x => x.IsRead, false));

                _logger.LogInformation("Marked {Count} notifications as unread for account: AccountId={AccountId}",
                    updatedRows, accountId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking all notifications as unread: AccountId={AccountId}", accountId);
                return false;
            }
        }
        public async Task<bool> InsertNewNotification(InsertNewNotificationDTO insertNewNotificationDTO)
        {
            try
            {
                var notification = new NotificationModel
                {
                    SenderId = insertNewNotificationDTO.SenderId,
                    ReceiverId = insertNewNotificationDTO.ReceiverId,
                    Message = insertNewNotificationDTO.Message,
                    IsRead = false,
                    CreateAt = DateTime.Now
                };

                await _dbContext.notifications.AddAsync(notification);
                int result = await _dbContext.SaveChangesAsync();

                if (result > 0)
                {
                    _logger.LogInformation("Successfully created notification: NotificationId={NotificationId}, SenderId={SenderId}, ReceiverId={ReceiverId}",
                        notification.NotificationId, insertNewNotificationDTO.SenderId, insertNewNotificationDTO.ReceiverId);
                    return true;
                }
                else
                {
                    _logger.LogWarning("Failed to create notification - no rows affected: SenderId={SenderId}, ReceiverId={ReceiverId}",
                        insertNewNotificationDTO.SenderId, insertNewNotificationDTO.ReceiverId);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating notification: SenderId={SenderId}, ReceiverId={ReceiverId}",
                    insertNewNotificationDTO?.SenderId, insertNewNotificationDTO?.ReceiverId);
                return false;
            }
        }
        public async Task<GetNotificationDTO> GetAllNotifications(int accountId)
        {
            try
            {
                var notification = await _dbContext.notifications
                    .Where(n => n.ReceiverId == accountId)
                    .OrderByDescending(n => n.CreateAt)
                    .Select(n => new GetNotificationDTO
                    {
                        NotificationId = n.NotificationId,
                        SenderId = n.SenderId,
                        Message = n.Message,
                        IsRead = n.IsRead,
                        CreateAt = n.CreateAt
                    })
                    .FirstOrDefaultAsync();

                if (notification != null)
                {
                    _logger.LogInformation("Retrieved notification for account: AccountId={AccountId}, NotificationId={NotificationId}",
                        accountId, notification.NotificationId);
                }
                else
                {
                    _logger.LogInformation("No notifications found for account: AccountId={AccountId}", accountId);
                }

                return notification;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving notification: AccountId={AccountId}", accountId);
                return null;
            }
        }
        public async Task<bool> DeleteNotification()
        {
            try
            {
                int deletedRows = await _dbContext.notifications
                    .Where(n => n.IsRead == true)
                    .ExecuteDeleteAsync();

                if (deletedRows > 0)
                {
                    _logger.LogInformation("Successfully deleted {Count} read notifications", deletedRows);
                    return true;
                }
                else
                {
                    _logger.LogInformation("No read notifications found to delete");
                    return true; // Return true because there's nothing to delete
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting read notifications");
                return false;
            }
        }

    }
}
