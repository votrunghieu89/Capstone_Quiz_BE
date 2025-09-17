namespace Capstone.DTOs.Auth
{
    public class GetNewATDTO
    {
        public int AccountId { get; set; }
        public string RefreshToken { get; set; } = string.Empty;
    }
}
