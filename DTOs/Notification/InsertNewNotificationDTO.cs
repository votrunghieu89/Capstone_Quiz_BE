namespace Capstone.DTOs.Notification
{
    public class InsertNewNotificationDTO
    {
        public int SenderId { get; set; }
        public int ReceiverId { get; set; }
        public string Message { get; set; }

    }
}
