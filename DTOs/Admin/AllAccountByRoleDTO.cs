using System.ComponentModel.DataAnnotations;

namespace Capstone.DTOs.Admin
{

    // All account by Role
    public class AllAccountByRoleDTO
    {
        public string Email { get; set; }
        public string Role { get; set; }
        public DateTime CreateAt { get; set; }
        public DateTime? UpdateAt { get; set; }
    }
}
