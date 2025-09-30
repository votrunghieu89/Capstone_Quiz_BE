namespace Capstone.DTOs.Auth
{
    public class GetNewAccessTokenDTO
    {
        public int AccountId { get; set; }
        public string RefreshToken { get; set; } = string.Empty;
    }
}
