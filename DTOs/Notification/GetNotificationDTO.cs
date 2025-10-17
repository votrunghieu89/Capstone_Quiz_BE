namespace Capstone.DTOs.Notification
{
    public class GetNotificationDTO
    {
        public int NotificationId { get; set; }
        public int SenderId { get; set; }
        public string Message { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreateAt { get; set; }
    }
}
