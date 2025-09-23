using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Capstone.Model.Others
{
    [Table("Notifications")]
    public class NotificationsModel
    {
        [Key]
        [Column("NotificationId")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int NotificationId { get; set; }

        [Column("Title")]
        [Required]
        [StringLength(255)]
        public string Title { get; set; } = string.Empty;


        [Column("Message")]
        [Required]
        public string Message { get; set; } = string.Empty;

        [Column("Type")]
        [Required]
        [MaxLength(50)]
        public string Type { get; set; } = string.Empty;

        [Column("IsFavourite")]
        public int? IsFavourite { get; set; }

        [Column("IsRead")]
        public int? IsRead { get; set; }

        [Column("SenderId")]
        public int SenderId { get; set; }

        [Column("ReceiverId")]
        public int ReceiverId { get; set; }

        [Column("CreatedAt")]
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("UpdatedAt")]
        public DateTime? UpdatedAt { get; set; } = DateTime.UtcNow;

        public NotificationsModel() { }
        public NotificationsModel(string tittle, string message, string type, 
            int? isFavourite, int isRead,int senderId, int receiverId, DateTime createdAt, DateTime updatedAt)
        {
            Title = tittle;
            Message = message;
            Type = type;
            IsFavourite = isFavourite;
            IsRead = isRead;
            SenderId = senderId;
            ReceiverId = receiverId;
            CreatedAt = createdAt;
            UpdatedAt = updatedAt;
          
        }
    }
}