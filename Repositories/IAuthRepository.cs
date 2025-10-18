using Capstone.DTOs.Auth;

namespace Capstone.Repositories
{
    public interface IAuthRepository
    {
        Task<int> isEmailExist(string email);
        Task<bool> RegisterStudent(AuthRegisterStudentDTO authRegisterDTO);
        Task<bool> RegisterTeacher(AuthRegisterTeacherDTO authRegisterDTO);
        Task<AuthLoginResponse> Login(AuthLoginDTO authLoginDTO);
        Task<bool> ChangePassword(AuthChangePasswordDTO changePasswordDTO);
        Task<bool> Logout(int accountId);
        Task<bool> verifyOTP(string Email, string otp);
        Task<bool> updateNewPassword(int accountId, string newPassword);
        Task<string> getNewAccessToken(GetNewAccessTokenDTO tokenDTO);
        Task<AuthLoginResponse> LoginGoogleforStudent(string email);
        Task<AuthLoginResponse> LoginGoogleforTeacher(string email);
    }
}
