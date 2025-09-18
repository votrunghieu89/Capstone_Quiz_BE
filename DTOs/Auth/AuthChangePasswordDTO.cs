namespace Capstone.DTOs.Auth
{
    public class AuthChangePasswordDTO
    {
        public string Email { get; set; } = string.Empty;
        public string oldPassword { get; set; } = string.Empty;
        public string newPassword { get; set; } = string.Empty;
     
        public AuthChangePasswordDTO() { }
        public AuthChangePasswordDTO(string email, string oldPassword, string newPassword)
        {
            Email = email;
            this.oldPassword = oldPassword;
            this.newPassword = newPassword;
      
        }
    }
}
