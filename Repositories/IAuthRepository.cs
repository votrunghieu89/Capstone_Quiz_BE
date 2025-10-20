using Capstone.DTOs.Auth;
using Capstone.ENUMs;

namespace Capstone.Repositories
{
    public interface IAuthRepository
    {
        Task<int> isEmailExist(string email);
        Task<bool> RegisterStudent(AuthRegisterStudentDTO authRegisterDTO);
        Task<bool> RegisterTeacher(AuthRegisterTeacherDTO authRegisterDTO);
        Task<AuthLoginResultDTO> Login(AuthLoginDTO authLoginDTO);
        Task<bool> ChangePassword(AuthChangePasswordDTO changePasswordDTO);
        Task<bool> Logout(int accountId);
        Task<bool> verifyOTP(string Email, string otp);
        Task<bool> updateNewPassword(int accountId, string newPassword);
        Task<string> getNewAccessToken(GetNewAccessTokenDTO tokenDTO);
        Task<AuthLoginResultDTO> LoginGoogleforStudent(string email);
        Task<AuthLoginResultDTO> LoginGoogleforTeacher(string email);
    }
}
