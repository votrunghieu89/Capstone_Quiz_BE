namespace Capstone.DTOs.Dashboard
{
    public class DashboardAccountDTO
    {
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string Role { get; set; } = string.Empty;
    }
}
