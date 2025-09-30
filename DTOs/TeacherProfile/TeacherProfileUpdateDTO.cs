using Microsoft.AspNetCore.Http;

namespace Capstone.DTOs.TeacherProfile
{
    public class TeacherProfileUpdateDTO
    {
        public int TeacherId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? OrganizationName { get; set; }
        public string? OrganizationAddress { get; set; }
        public IFormFile? FormFile { get; set; }
    }
}
