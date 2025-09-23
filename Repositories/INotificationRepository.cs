using Capstone.DTOs.Notification;
using Capstone.ENUMs;
using Capstone.Model;
using Capstone.Model.Others;

namespace Capstone.Repositories
{
    public interface INotificationRepository
    {
        Task<bool> checkConnection();
        Task<bool> SaveNotification(NotificationSaveModel notificationSaveModel);
        Task<List<NotificationsModel>> GetNotificationsByAccountId(int accountId);
        Task<List<NotificationsModel>> GetFavouriteNotifications(int accountId);
        Task<int> UnreadCount(int accountId);
        Task<bool> MarkAllAsRead(int accountId);
        Task<bool> MarkAsRead(int accountId, int notificationId);
        Task<bool> Unmark(int accountId, int notificationId);
        Task<bool> UnmarkAll(int accountId);

        Task<bool> AddToFavourite(int accountId, int notificationId);
        Task<bool> RemoveFromFavourite(int accountId, int notificationId);
        Task<bool> DeleteReadNotifications();


        // Tác vụ để nhận thông báo
        Task<SubmitResult> SubmitCV(CV_JD_ApplyModel cV_JD_ApplyModel);
        Task<bool> ApproveCV(CV_JD_ApplyModel cV_JD_ApplyModel);
        Task<bool> RejectCV(CV_JD_ApplyModel cV_JD_ApplyModel);
    }
}
