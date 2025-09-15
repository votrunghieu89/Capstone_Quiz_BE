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
        public AuthModel() { }
        public AuthModel(int accountId, string email, string password, string role)
        {
            AccountId = accountId;
            Email = email;
            Password = password;
            Role = role;
        }
    }
}
