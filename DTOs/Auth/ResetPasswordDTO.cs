namespace Capstone.DTOs.Auth
{
    public class ResetPasswordDTO
    {
        public int accountId { get; set; }
        public string PasswordReset { get; set; } = string.Empty;   
    }
}
