namespace Capstone.DTOs.Auth
{
    public class VerifyOTP
    {
        public int AccountId { get; set; }
        public string OTP { get; set; } = string.Empty;

    }
}
