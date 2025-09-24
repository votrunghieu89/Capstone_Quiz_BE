using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Capstone.DTOs.Notification
{
    public class NotificationSaveModel
    {
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public int? IsFavourite { get; set; }
        public int? IsRead { get; set; } 
        public int SenderId { get; set; }
        public int ReceiverId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; } = DateTime.Now;
        public NotificationSaveModel() { }
        public NotificationSaveModel(string tittle, string message, string type, int? isFavourite, int? isRead, int senderId, int receiverId, DateTime createdAt, DateTime? updatedAt)
        {
            Title = tittle ?? string.Empty;
            Message = message ?? string.Empty;
            Type = type ?? string.Empty;
            IsFavourite = isFavourite;
            IsRead = isRead;
            SenderId = senderId;
            ReceiverId = receiverId;
            CreatedAt = createdAt;
            UpdatedAt = updatedAt;
        }
    }
}
