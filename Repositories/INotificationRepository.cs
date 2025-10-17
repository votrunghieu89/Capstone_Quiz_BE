using Capstone.DTOs.Notification;

namespace Capstone.Repositories
{
    public interface INotificationRepository
    {
     
        Task<bool> Mark(int notificationId);

   
        Task<bool> UnMark(int notificationId);

        Task<bool> MarkAll(int accountId);

        Task<bool> UnMarkAll(int accountId);

        Task<bool> InsertNewNotification(InsertNewNotificationDTO insertNewNotificationDTO);

      
        Task<List<GetNotificationDTO>> GetAllNotifications(int accountId);

   
        Task<bool> DeleteNotification();
    }
}
