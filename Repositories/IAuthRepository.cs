using Capstone.DTOs.Auth;

namespace Capstone.Repositories
{
    public interface IAuthRepository
    {
        Task<bool> isEmailExist(string email);
        Task<bool> RegisterCandidate(AuthRegisterDTO authRegisterDTO);
        Task<bool> RegisterRecruiter(AuthRegisterRecruiterDTO authRegisterDTO);
        Task<AuthLoginResponse> LoginResponse(AuthLoginDTO authLoginDTO);
        Task<bool> ChangePassword(AuthChangePasswordDTO changePasswordDTO);
        Task<bool> Logout(int accountId);
        Task<bool> ForgotPassword(string email);
    }
}
