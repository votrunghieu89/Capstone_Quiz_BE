using System.ComponentModel.DataAnnotations;

namespace Capstone.DTOs.Admin
{

    // All account by Role
    public class AllAccountByRoleDTO
    {
        public int AccountId { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreateAt { get; set; }
    }
}
