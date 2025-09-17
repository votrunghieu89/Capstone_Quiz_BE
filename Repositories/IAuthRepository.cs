using Capstone.DTOs.Auth;

namespace Capstone.Repositories
{
    public interface IAuthRepository
    {
        Task<bool> checkConnection();
        Task<int> isEmailExist(string email);
        Task<bool> RegisterCandidate(AuthRegisterDTO authRegisterDTO);
        Task<bool> RegisterRecruiter(AuthRegisterRecruiterDTO authRegisterDTO);
        Task<AuthLoginResponse> Login(AuthLoginDTO authLoginDTO);
        Task<bool> ChangePassword(AuthChangePasswordDTO changePasswordDTO);
        Task<bool> Logout(int accountId);
        Task<bool> verifyOTP(int accountId, string otp);
        Task<bool> updateNewPassword(int accountId, string newPassword);
        Task<string> getNewAccessToken(GetNewATDTO tokenDTO);
    }
}
