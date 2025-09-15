namespace Capstone.DTOs.Auth
{
    public class AuthRegisterDTO
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;

        public AuthRegisterDTO() { }
        public AuthRegisterDTO(string fullName, string email, string password)
        {
            FullName = fullName;
            Email = email;
            Password = password;
        }
    }
}
