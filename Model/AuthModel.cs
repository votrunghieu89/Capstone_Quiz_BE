using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Capstone.Model
{
    [Table("Accounts")]
    public class AuthModel
    {
        [Key]
        [Column("AccountId")]
        public int AccountId { get; set; }
        [Column("Email")]
        public string Email { get; set; } = string.Empty;
        [Column("Password")]
        public string Password { get; set; } = string.Empty;
        [Column("Role")]
        public string Role { get; set; } = string.Empty;
        [Column("CreateAt")]
        public DateTime CreateAt { get; set; } = DateTime.Now;

        [Column("UpdateAt")]
        public DateTime UpdateAt { get; set; } = DateTime.Now;

        public AuthModel() { }
        public AuthModel(int accountId, string email, string password, string role, DateTime createAt, DateTime updateAt)
        {
            AccountId = accountId;
            Email = email;
            Password = password;
            Role = role;
            CreateAt = createAt;
            UpdateAt = updateAt;
        }
    }
}
