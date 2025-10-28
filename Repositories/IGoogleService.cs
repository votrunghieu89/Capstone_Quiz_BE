using Capstone.DTOs.Auth;

namespace Capstone.Repositories
{
    public interface IGoogleService
    {
        Task<GoogleResponse> checkIdToken(string idToken);
    }
}
