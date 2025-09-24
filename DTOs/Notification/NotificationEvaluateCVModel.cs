namespace Capstone.DTOs.Notification
{
    public class NotificationEvaluateCVModel
    {
        public int AccountId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
    }
}
