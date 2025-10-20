using Capstone.ENUMs;

namespace Capstone.DTOs.Auth
{
    public class AuthLoginResultDTO
    {
        public AuthEnum.Login Status { get; set; }
        public AuthLoginResponse? AuthLoginResponse { get; set; }
    }
}
