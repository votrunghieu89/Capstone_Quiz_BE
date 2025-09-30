namespace Capstone.DTOs.Auth
{
    public class AuthRegisterStudentDTO
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;

        public AuthRegisterStudentDTO() { }
        public AuthRegisterStudentDTO(string fullName, string email, string password)
        {
            FullName = fullName;
            Email = email;
            PasswordHash = password;
        }
    }
}
