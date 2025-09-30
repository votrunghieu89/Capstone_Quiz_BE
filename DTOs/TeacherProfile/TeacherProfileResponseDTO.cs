namespace Capstone.DTOs.TeacherProfile
{
    public class TeacherProfileResponseDTO
    {
        public string FullName { get; set; } = string.Empty;
        public string? AvatarURL { get; set; }
        public string? oldAvatar { get; set; }
    }
}
