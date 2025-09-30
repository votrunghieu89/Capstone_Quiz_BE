using System.ComponentModel.DataAnnotations.Schema;

namespace Capstone.DTOs.StudentProfile
{
    public class StudentProfileResponseDTO
    {
        public string FullName { get; set; } = string.Empty;
        public string? AvatarURL { get; set; }

        public string? oldAvatar { get; set; }

    }
}
