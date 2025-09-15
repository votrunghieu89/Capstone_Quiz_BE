namespace Capstone.DTOs.Auth
{
    public class AuthLoginDTO
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public AuthLoginDTO() { }
        public AuthLoginDTO(string email, string password)
        {
            Email = email;
            Password = password;
        }
    }
}
