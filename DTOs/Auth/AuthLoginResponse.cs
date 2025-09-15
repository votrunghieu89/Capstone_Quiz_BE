namespace Capstone.DTOs.Auth
{
    public class AuthLoginResponse
    {
        public int AccountId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string AccesToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;    
        public AuthLoginResponse() { }
        public AuthLoginResponse(int accountId, string email, string role, string accessToken, string refreshToken)
        {
            AccountId = accountId;
            Email = email;
            Role = role;
            AccesToken = accessToken;
            RefreshToken = refreshToken;
        }
    }
}
