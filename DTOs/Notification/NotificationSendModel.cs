namespace Capstone.DTOs.Notification
{
    public class NotificationSendModel
    {
        public int receiverId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string message { get; set; } = string.Empty;

        public NotificationSendModel() { }
        public NotificationSendModel(int receiverId, string title, string message)
        {
            this.receiverId = receiverId;
            Title = title ?? string.Empty;
            this.message = message ?? string.Empty;
        }
    }
}
