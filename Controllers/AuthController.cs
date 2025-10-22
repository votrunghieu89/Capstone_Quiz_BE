using Capstone.Database;
using Capstone.DTOs.Auth;
using Capstone.ENUMs;
using Capstone.Repositories;
using Capstone.Security;
using Capstone.Services;
using Capstone.Settings;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using System.Security.Cryptography;

namespace Capstone.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        public readonly IAuthRepository _authRepository;
        public readonly Redis _redis;
        public readonly ILogger<AuthController> _logger;
        public readonly EmailService _emailService;
        private readonly GoogleService _googleService;

        public AuthController(IAuthRepository authRepository, Redis redis, ILogger<AuthController> logger, EmailService service, GoogleService googleService)
        {
            _authRepository = authRepository;
            _redis = redis;
            _logger = logger;
            _emailService = service;
            _googleService = googleService;
        }
        public static string GenerateOTP(int length = 6)
        {
            if (length <= 0) throw new ArgumentException("Length must be positive", nameof(length));

            using var rng = RandomNumberGenerator.Create();
            var bytes = new byte[4];
            rng.GetBytes(bytes);
            int number = BitConverter.ToInt32(bytes, 0) & 0x7FFFFFFF;
            int otp = number % (int)Math.Pow(10, length);
            return otp.ToString($"D{length}");
        }

        // ===== POST METHODS =====
        [HttpPost("checkEmail")]
        public async Task<ActionResult> isEmailExist([FromBody] string email)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email))
                    return BadRequest(new { message = "Email is required." });

                var isExist = await _authRepository.isEmailExist(email);
                if (isExist == 0)
                {
                    _logger.LogInformation("checkEmail: Email not found: {Email}", email);
                    return NotFound(new { message = "Email does not exist in the system." });
                }

                var otp = GenerateOTP();
                var otpHash = Hash.HashPassword(otp);
                await _redis.SetStringAsync($"OTP_{email}", otpHash, TimeSpan.FromMinutes(5));

                string subject = "Your OTP Code";
                await _emailService.SendEmailAsync(email, subject, otp);
                _logger.LogInformation("checkEmail: OTP generated and sent for AccountId={AccountId}, Email={Email}", isExist, email);
                return Ok(new { message = "OTP has been sent to your email.", AccountId = isExist});
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "checkEmail: Error processing request for Email={Email}", email);
                return StatusCode(500, new { message = "An error occurred while processing your request." });
            }
        }

        [HttpPost("verifyOTP")]
        public async Task<ActionResult> verifyOTP([FromBody] VerifyOTP verifyOTP)
        {
            try
            {
                if (verifyOTP == null)
                {
                    _logger.LogWarning("verifyOTP: Request body null");
                    return BadRequest(new { message = "Request body is required." });
                }

                if (string.IsNullOrWhiteSpace(verifyOTP.OTP))
                {
                    _logger.LogWarning("verifyOTP: OTP missing for AccountId={AccountId}", verifyOTP.Email);
                    return BadRequest(new { message = "OTP is required." });
                }

                bool checkOTP = await _authRepository.verifyOTP(verifyOTP.Email, verifyOTP.OTP);
                if (!checkOTP)
                {
                    _logger.LogInformation("verifyOTP: Invalid or expired OTP for AccountId={AccountId}", verifyOTP.Email);
                    return BadRequest(new { message = "Invalid or expired OTP." });
                }

                _logger.LogInformation("verifyOTP: OTP verified for AccountId={AccountId}", verifyOTP.Email);
                return Ok(new { message = "OTP verification successful. You can now reset your password." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "verifyOTP: Error verifying OTP for AccountId={AccountId}" , verifyOTP?.Email);
                return StatusCode(500, new { message = "An error occurred while processing your request." });
            }
        }

        [HttpPost("resetPasswordOTP")]
        public async Task<ActionResult> resetPassword([FromBody] ResetPasswordDTO resetPasswordDTO)
        {
            try
            {
                if (resetPasswordDTO == null)
                {
                    _logger.LogWarning("resetPassword: Request body null");
                    return BadRequest(new { message = "Request body is required." });
                }

                if (string.IsNullOrWhiteSpace(resetPasswordDTO.PasswordReset))
                {
                    _logger.LogWarning("resetPassword: PasswordReset missing for AccountId={AccountId}", resetPasswordDTO.accountId);
                    return BadRequest(new { message = "PasswordReset is required." });
                }

                bool isReset = await _authRepository.updateNewPassword(resetPasswordDTO.accountId, resetPasswordDTO.PasswordReset);
                if (!isReset)
                {
                    _logger.LogWarning("resetPassword: Failed to reset password for AccountId={AccountId}", resetPasswordDTO.accountId);
                    return BadRequest(new { message = "Failed to reset password. Please try again." });
                }

                await _redis.DeleteKeyAsync($"OTP_{resetPasswordDTO.accountId}");
                _logger.LogInformation("resetPassword: Password reset successful for AccountId={AccountId}", resetPasswordDTO.accountId);
                return Ok(new { message = "Password has been reset successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "resetPassword: Error while resetting password for AccountId={AccountId}", resetPasswordDTO?.accountId);
                return StatusCode(500, new { message = "An error occurred while processing your request." });
            }
        }

        [HttpPost("login")]
        public async Task<ActionResult> Login([FromBody] AuthLoginDTO authLoginDTO)
        {
            try
            {
                if (authLoginDTO == null)
                {
                    _logger.LogWarning("login: Request body null");
                    return BadRequest(new { message = "Request body is required." });
                }

                if (string.IsNullOrWhiteSpace(authLoginDTO.Email) || string.IsNullOrWhiteSpace(authLoginDTO.Password))
                {
                    _logger.LogWarning("login: Missing email or password");
                    return BadRequest(new { message = "Email and Password are required." });
                }
                var ipAddess = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault()
             ?? HttpContext.Connection.RemoteIpAddress?.ToString();
                var loginResponse = await _authRepository.Login(authLoginDTO, ipAddess);
                switch (loginResponse.Status) {
                    case AuthEnum.Login.Success:
                        return Ok(loginResponse.AuthLoginResponse);
                    case AuthEnum.Login.Error:
                        return StatusCode(500, new { message = "A server error prevented login." });
                    case AuthEnum.Login.WrongEmailOrPassword:
                        return Unauthorized(new { message = "Invalid email or password." });
                    case AuthEnum.Login.AccountHasBanned:
                        return StatusCode(403, new { message = "Your account has been suspended or banned." });
                    default:
                        _logger.LogError("login failed: Unhandled status case for Email={Email}, Status={Status}", authLoginDTO.Email, loginResponse.Status);
                        return StatusCode(500, new { message = "An unexpected error occurred." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "login: Error while logging in for Email={Email}", authLoginDTO?.Email);
                return StatusCode(500, new { message = "An error occurred while processing your request." });
            }
        }

        [HttpPost("registerStudent")]
        public async Task<ActionResult> registerStudent([FromBody] AuthRegisterStudentDTO authRegisterDTO)
        {
            try
            {
                var ipAddess = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault()
             ?? HttpContext.Connection.RemoteIpAddress?.ToString();
                if (authRegisterDTO == null)
                {
                    _logger.LogWarning("registerStudent: Request body null");
                    return BadRequest(new { message = "Request body is required." });
                }

                if (string.IsNullOrWhiteSpace(authRegisterDTO.Email) || string.IsNullOrWhiteSpace(authRegisterDTO.PasswordHash))
                {
                    _logger.LogWarning("registerStudent: Missing email or password");
                    return BadRequest(new { message = "Email and Password are required." });
                }

                if (string.IsNullOrWhiteSpace(authRegisterDTO.FullName))
                {
                    _logger.LogWarning("registerStudent: Missing FullName for Email={Email}", authRegisterDTO.Email);
                    return BadRequest(new { message = "FullName is required." });
                }

                int checkEmail = await _authRepository.isEmailExist(authRegisterDTO.Email);
                if (checkEmail != 0)
                {
                    _logger.LogInformation("registerStudent: Email already exists: {Email}", authRegisterDTO.Email);
                    return BadRequest(new { message = "Email already exists. Please use a different email." });
                }

                var isRegistered = await _authRepository.RegisterStudent(authRegisterDTO, ipAddess);
                if (!isRegistered)
                {
                    _logger.LogError("registerStudent: Registration failed for Email={Email}", authRegisterDTO.Email);
                    return BadRequest(new { message = "Registration failed. Please try again." });
                }

                _logger.LogInformation("registerStudent: Student registered successfully. Email={Email}", authRegisterDTO.Email);
                return Ok(new { message = "Candidate registered successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "registerStudent: Error while registering student for Email={Email}", authRegisterDTO?.Email);
                return StatusCode(500, new { message = "An error occurred while processing your request." });
            }
        }

        [HttpPost("registerTeacher")]
        public async Task<ActionResult> RegisterTeacher([FromBody] AuthRegisterTeacherDTO authRegisterDTO)
        {
            try
            {
                if (authRegisterDTO == null)
                {
                    _logger.LogWarning("registerTeacher: Request body null");
                    return BadRequest(new { message = "Request body is required." });
                }

                if (string.IsNullOrWhiteSpace(authRegisterDTO.Email) || string.IsNullOrWhiteSpace(authRegisterDTO.PasswordHash)
                    || string.IsNullOrWhiteSpace(authRegisterDTO.OrganizationName) || string.IsNullOrWhiteSpace(authRegisterDTO.OrganizationAddress))
                {
                    _logger.LogWarning("registerTeacher: Missing required fields for Email={Email}", authRegisterDTO.Email);
                    return BadRequest(new { message = "Email, Password, CompanyName and CompanyAddress are required." });
                }

                int checkEmail = await _authRepository.isEmailExist(authRegisterDTO.Email);
                if (checkEmail != 0)
                {
                    _logger.LogInformation("registerTeacher: Email already exists: {Email}", authRegisterDTO.Email);
                    return BadRequest(new { message = "Email already exists. Please use a different email." });
                }
                var ipAddess = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault()
             ?? HttpContext.Connection.RemoteIpAddress?.ToString();
                var isRegistered = await _authRepository.RegisterTeacher(authRegisterDTO, ipAddess);
                if (!isRegistered)
                {
                    _logger.LogError("registerTeacher: Registration failed for Email={Email}", authRegisterDTO.Email);
                    return BadRequest(new { message = "Registration failed. Please try again." });
                }
                _logger.LogInformation("registerTeacher: Teacher registered successfully. Email={Email}", authRegisterDTO.Email);
                return Ok(new { message = "Recruiter registered successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "registerTeacher: Error while registering teacher for Email={Email}", authRegisterDTO?.Email);
                return StatusCode(500, new { message = "An error occurred while processing your request." });
            }
        }

        [HttpPost("changePassword")]
        public async Task<ActionResult> ChangePassword([FromBody] AuthChangePasswordDTO changePasswordDTO)
        {
            try
            {
                if (changePasswordDTO == null)
                {
                    _logger.LogWarning("changePassword: Request body null");
                    return BadRequest(new { message = "Request body is required." });
                }

                if (string.IsNullOrWhiteSpace(changePasswordDTO.Email)
                    || string.IsNullOrWhiteSpace(changePasswordDTO.oldPassword)
                    || string.IsNullOrWhiteSpace(changePasswordDTO.newPassword))
                {
                    _logger.LogWarning("changePassword: Missing required fields for Email={Email}", changePasswordDTO?.Email);
                    return BadRequest(new { message = "Email, oldPassword and newPassword are required." });
                }

                var isChanged = await _authRepository.ChangePassword(changePasswordDTO);
                if (!isChanged)
                {
                    _logger.LogWarning("changePassword: Failed to change password for Email={Email}", changePasswordDTO.Email);
                    return BadRequest(new { message = "Failed to change password. Please try again." });
                }
                _logger.LogInformation("changePassword: Password changed for Email={Email}", changePasswordDTO.Email);
                return Ok(new { message = "Password changed successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "changePassword: Error while changing password for Email={Email}", changePasswordDTO?.Email);
                return StatusCode(500, new { message = "An error occurred while processing your request." });
            }
        }

        // Logout API
        [HttpPost("logout/{accountId}")]
        public async Task<ActionResult> Logout(int accountId)
        {
            try
            {
                var isLoggedOut = await _authRepository.Logout(accountId);
                if (!isLoggedOut)
                {
                    _logger.LogWarning("logout: Failed logout attempt for AccountId={AccountId}", accountId);
                    return BadRequest(new { message = "Failed to logout. Please try again." });
                }
                _logger.LogInformation("logout: User logged out AccountId={AccountId}", accountId);
                return Ok(new { message = "Logout successful." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "logout: Error while logging out for AccountId={AccountId}", accountId);
                return StatusCode(500, new { message = "An error occurred while processing your request." });
            }
        }

        // Get New Access Token API
        [HttpPost("accessToken")]
        public async Task<ActionResult> GetNewAccessToken([FromBody] GetNewAccessTokenDTO tokenDTO)
        {
            try
            {
                if (tokenDTO == null)
                {
                    _logger.LogWarning("getNewAccessToken: Request body null");
                    return BadRequest(new { message = "Request body is required." });
                }

                var newAccessToken = await _authRepository.getNewAccessToken(tokenDTO);
                if (newAccessToken == null)
                {
                    _logger.LogWarning("getNewAccessToken: Failed to generate new access token for AccountId={AccountId}", tokenDTO.AccountId);
                    return BadRequest(new { message = "Failed to generate a new Access Token. Please try again." });
                }
                _logger.LogInformation("getNewAccessToken: New access token generated for AccountId={AccountId}", tokenDTO.AccountId);
                return Ok(new { AccessToken = newAccessToken });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "getNewAccessToken: Error while generating new access token for AccountId={AccountId}", tokenDTO?.AccountId);
                return StatusCode(500, new { message = "An error occurred while processing your request." });
            }
        }

        // Google Login API
        [HttpPost("googleLoginStudent")]
        public async Task<IActionResult> GoogleLoginStudent([FromBody] GoogleLoginDto request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.IdToken))
            {
                _logger.LogWarning("googleLogin: Missing IdToken in request");
                return BadRequest(new { message = "IdToken is required." });
            }
            try
            {
                var ipAddress = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault()?? HttpContext.Connection.RemoteIpAddress?.ToString();
                if (ipAddress == null)
                {
                    return BadRequest(new { message = "Invalid Ip address." });
                }
                var googleResponse = await _googleService.checkIdToken(request.IdToken); // return Email, name, and avartarURL
                if (googleResponse == null)
                {
                    _logger.LogWarning("googleLogin: Invalid IdToken provided");
                    return BadRequest(new { message = "Invalid IdToken." });
                }
                var checkIsEmail = await _authRepository.isEmailExist(googleResponse.Email);
                if (checkIsEmail == 0)
                {
                    var newAccout = new AuthRegisterStudentDTO
                    {
                        Email = googleResponse.Email,
                        PasswordHash = Guid.NewGuid().ToString(),
                        FullName = googleResponse.Name
                    };
                    var isRegistered = await _authRepository.RegisterStudent(newAccout, ipAddress);
                    _logger.LogInformation("googleLogin: New student account created for Email={Email}", googleResponse.Email);
                }
                var loginResponse = await _authRepository.LoginGoogleforStudent(googleResponse.Email); // có trả về accessTOken và refreshToken
                switch (loginResponse.Status)
                {
                    case AuthEnum.Login.Success:
                        return Ok(loginResponse.AuthLoginResponse);
                    case AuthEnum.Login.Error:
                        return StatusCode(500, new { message = "A server error prevented login." });
                    case AuthEnum.Login.WrongEmailOrPassword:
                        return Unauthorized(new { message = "Invalid email or password." });
                    case AuthEnum.Login.AccountHasBanned:
                        return StatusCode(403, new { message = "Your account has been suspended or banned." });
                    default:
                        return StatusCode(500, new { message = "An unexpected error occurred." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "googleLogin: Error processing Google login");
                return StatusCode(500, new { message = "An unexpected error occurred." });
            }
        }

        [HttpPost("googleLoginTeacher")]
        public async Task<IActionResult> GoogleLoginTeacher([FromBody] GoogleLoginDto request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.IdToken))
            {
                _logger.LogWarning("googleLogin: Missing IdToken in request");
                return BadRequest(new { message = "IdToken is required." });
            }
            try
            {
                var googleResponse = await _googleService.checkIdToken(request.IdToken); // return Email, name, and avartarURL
                if (googleResponse == null)
                {
                    _logger.LogWarning("googleLogin: Invalid IdToken provided");
                    return BadRequest(new { message = "Invalid IdToken." });
                }
                var ipAddess = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault() ?? HttpContext.Connection.RemoteIpAddress?.ToString();
                var checkIsEmail = await _authRepository.isEmailExist(googleResponse.Email);
                if (checkIsEmail == 0)
                {
                    var newAccout = new AuthRegisterTeacherDTO
                    {
                        Email = googleResponse.Email,
                        PasswordHash = Guid.NewGuid().ToString(),
                        FullName = googleResponse.Name,
                        OrganizationAddress = null,
                        OrganizationName = null
                    };
                    var isRegistered = await _authRepository.RegisterTeacher(newAccout,ipAddess);
                    _logger.LogInformation("googleLogin: New student account created for Email={Email}", googleResponse.Email);
                }
                var loginResponse = await _authRepository.LoginGoogleforTeacher(googleResponse.Email); // có trả về accessTOken và refreshToken
                switch (loginResponse.Status)
                {
                    case AuthEnum.Login.Success:
                        return Ok(loginResponse.AuthLoginResponse);
                    case AuthEnum.Login.Error:
                        return StatusCode(500, new { message = "A server error prevented login." });
                    case AuthEnum.Login.WrongEmailOrPassword:
                        return Unauthorized(new { message = "Invalid email or password." });
                    case AuthEnum.Login.AccountHasBanned:
                        return StatusCode(403, new { message = "Your account has been suspended or banned." });
                    default:
                        return StatusCode(500, new { message = "An unexpected error occurred." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "googleLogin: Error processing Google login");
                return StatusCode(500, new { message = "An unexpected error occurred." });
            }
        }
    }
}