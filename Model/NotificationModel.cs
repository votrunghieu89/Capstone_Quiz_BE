using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Capstone.Model
{
    [Table("Notifications")]
    public class NotificationModel
    {
        [Key]
        [Column("NotificationId")]
        public int NotificationId { get; set; }

        [Required]
        [Column("SenderId")]
        public int SenderId { get; set; }

        [Required]
        [Column("ReceiverId")]
        public int ReceiverId { get; set; }

        [Required]
        [Column("Message")]
        public string Message { get; set; }

        [Required]
        [Column("IsRead")]
        public bool IsRead { get; set; }

        [Column("CreateAt")]
        public DateTime CreateAt { get; set; }
    }
}
