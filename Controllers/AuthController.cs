using Capstone.Database;
using Capstone.DTOs.Auth;
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

        // 1. Forgot Password APIs
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
                    return NotFound(new { message = "Email does not exist in the system." });
                }

                var otp = GenerateOTP();
                var otpHash = Hash.HashPassword(otp);
                await _redis.SetStringAsync($"OTP_{isExist}", otpHash, TimeSpan.FromMinutes(5));

                string subject = "Your OTP Code";
                await _emailService.SendEmailAsync(email, subject, otp);
                return Ok(new { message = "OTP has been sent to your email." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while checking email or sending OTP.");
                return StatusCode(500, new { message = "An error occurred while processing your request." });
            }
        }

        [HttpPost("verifyOTP")]
        public async Task<ActionResult> verifyOTP([FromBody] VerifyOTP verifyOTP)
        {
            try
            {
                //if (verifyOTP == null)
                //    return BadRequest(new { message = "Request body is required." });

                //if (verifyOTP.AccountId <= 0)
                //    return BadRequest(new { message = "AccountId is required." });

                if (string.IsNullOrWhiteSpace(verifyOTP.OTP))
                    return BadRequest(new { message = "OTP is required." });

                bool checkOTP = await _authRepository.verifyOTP(verifyOTP.AccountId, verifyOTP.OTP);
                if (!checkOTP)
                {
                    return BadRequest(new { message = "Invalid or expired OTP." });
                }
                return Ok(new { message = "OTP verification successful. You can now reset your password." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while verifying OTP.");
                return StatusCode(500, new { message = "An error occurred while processing your request." });
            }
        }

        [HttpPost("resetPasswordGoogle")]
        public async Task<ActionResult> resetPassword([FromBody] ResetPasswordDTO resetPasswordDTO)
        {
            try
            {
                //if (resetPasswordDTO == null)
                //    return BadRequest(new { message = "Request body is required." });

                //if (resetPasswordDTO.accountId <= 0)
                //    return BadRequest(new { message = "AccountId is required." });

                if (string.IsNullOrWhiteSpace(resetPasswordDTO.PasswordReset))
                    return BadRequest(new { message = "PasswordReset is required." });

                bool isReset = await _authRepository.updateNewPassword(resetPasswordDTO.accountId, resetPasswordDTO.PasswordReset);
                if (!isReset)
                {
                    return BadRequest(new { message = "Failed to reset password. Please try again." });
                }
                await _redis.DeleteKeyAsync($"OTP_{resetPasswordDTO.accountId}");
                return Ok(new { message = "Password has been reset successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while resetting password.");
                return StatusCode(500, new { message = "An error occurred while processing your request." });
            }
        }

        // Login API
        [HttpPost("login")]
        public async Task<ActionResult> Login([FromBody] AuthLoginDTO authLoginDTO)
        {
            try
            {
                //if (authLoginDTO == null)
                //    return BadRequest(new { message = "Request body is required." });

                if (string.IsNullOrWhiteSpace(authLoginDTO.Email) || string.IsNullOrWhiteSpace(authLoginDTO.Password))
                    return BadRequest(new { message = "Email and Password are required." });

                var loginResponse = await _authRepository.Login(authLoginDTO);
                if (loginResponse == null)
                {
                    return Unauthorized(new { message = "Invalid email or password." });
                }
                return Ok(loginResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while logging in.");
                return StatusCode(500, new { message = "An error occurred while processing your request." });
            }
        }

        // Register Candidate API
        [HttpPost("registerCandidate")]
        public async Task<ActionResult> RegisterCandidate([FromBody] AuthRegisterDTO authRegisterDTO)
        {
            try
            {
                //if (authRegisterDTO == null)
                //    return BadRequest(new { message = "Request body is required." });

                if (string.IsNullOrWhiteSpace(authRegisterDTO.Email) || string.IsNullOrWhiteSpace(authRegisterDTO.Password))
                    return BadRequest(new { message = "Email and Password are required." });
                if(string.IsNullOrWhiteSpace( authRegisterDTO.FullName))
                {
                   return BadRequest(new { message = "FullName is required." });
                }
                int checkEmail = await _authRepository.isEmailExist(authRegisterDTO.Email);
                if (checkEmail != 0)
                {
                    return BadRequest(new { message = "Email already exists. Please use a different email." });
                }
                var isRegistered = await _authRepository.RegisterCandidate(authRegisterDTO);
                if (!isRegistered)
                {
                    return BadRequest(new { message = "Registration failed. Please try again." });
                }
                return Ok(new { message = "Candidate registered successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while registering candidate.");
                return StatusCode(500, new { message = "An error occurred while processing your request." });
            }
        }

        // Register Recruiter API
        [HttpPost("registerRecruiter")]
        public async Task<ActionResult> RegisterRecruiter([FromBody] AuthRegisterRecruiterDTO authRegisterDTO)
        {
            try
            {
                //if (authRegisterDTO == null)
                //    return BadRequest(new { message = "Request body is required." });

                if (string.IsNullOrWhiteSpace(authRegisterDTO.Email) || string.IsNullOrWhiteSpace(authRegisterDTO.Password)
                    || string.IsNullOrWhiteSpace(authRegisterDTO.CompanyName) || string.IsNullOrWhiteSpace(authRegisterDTO.CompanyAddress))
                {
                    return BadRequest(new { message = "Email, Password, CompanyName and CompanyAddress are required." });
                }

                int checkEmail = await _authRepository.isEmailExist(authRegisterDTO.Email);
                if (checkEmail != 0)
                {
                    return BadRequest(new { message = "Email already exists. Please use a different email." });
                }
                var isRegistered = await _authRepository.RegisterRecruiter(authRegisterDTO);
                if (!isRegistered)
                {
                    return BadRequest(new { message = "Registration failed. Please try again." });
                }
                return Ok(new { message = "Recruiter registered successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while registering recruiter.");
                return StatusCode(500, new { message = "An error occurred while processing your request." });
            }
        }

        // Change Password API
        [HttpPost("changePassword")]
        public async Task<ActionResult> ChangePassword([FromBody] AuthChangePasswordDTO changePasswordDTO)
        {
            try
            {
                //if (changePasswordDTO == null)
                //    return BadRequest(new { message = "Request body is required." });

                if (string.IsNullOrWhiteSpace(changePasswordDTO.Email)
                    || string.IsNullOrWhiteSpace(changePasswordDTO.oldPassword)
                    || string.IsNullOrWhiteSpace(changePasswordDTO.newPassword))
                {
                    return BadRequest(new { message = "Email, oldPassword and newPassword are required." });
                }

                var isChanged = await _authRepository.ChangePassword(changePasswordDTO);
                if (!isChanged)
                {
                    return BadRequest(new { message = "Failed to change password. Please try again." });
                }
                return Ok(new { message = "Password changed successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while changing password.");
                return StatusCode(500, new { message = "An error occurred while processing your request." });
            }
        }

        // Logout API
        [HttpPost("logout/{accountId}")]
        public async Task<ActionResult> Logout(int accountId)
        {
            try
            {
                //if (accountId <= 0)
                //    return BadRequest(new { message = "AccountId is required." });

                var isLoggedOut = await _authRepository.Logout(accountId);
                if (!isLoggedOut)
                {
                    return BadRequest(new { message = "Failed to logout. Please try again." });
                }
                return Ok(new { message = "Logout successful." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while logging out.");
                return StatusCode(500, new { message = "An error occurred while processing your request." });
            }
        }

        // Get New Access Token API
        [HttpPost("accessToken")]
        public async Task<ActionResult> GetNewAccessToken([FromBody] GetNewATDTO tokenDTO)
        {
            try
            {
                //if (tokenDTO == null)
                //    return BadRequest(new { message = "Request body is required." });

                //if (tokenDTO.AccountId <= 0 || string.IsNullOrWhiteSpace(tokenDTO.RefreshToken))
                //    return BadRequest(new { message = "AccountId and RefreshToken are required." });

                var newAccessToken = await _authRepository.getNewAccessToken(tokenDTO);
                if (newAccessToken == null)
                {
                    return BadRequest(new { message = "Failed to generate a new Access Token. Please try again." });
                }
                return Ok(new { AccessToken = newAccessToken });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while generating new Access Token.");
                return StatusCode(500, new { message = "An error occurred while processing your request." });
            }
        }

        // Google Login API
        [HttpPost("googleLogin")]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginDto request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.IdToken))
            {
                return BadRequest(new { message = "IdToken is required." });
            }
            try
            {
                var googleResponse = await _googleService.checkIdToken(request.IdToken); // return Email, name, and avartarURL
                if (googleResponse == null)
                {
                    return BadRequest(new { message = "Invalid IdToken." });
                }
                var checkIsEmail =  await _authRepository.isEmailExist(googleResponse.Email);
                if(checkIsEmail == 0)
                {
                    var newAccout = new AuthRegisterDTO
                    {
                        Email = googleResponse.Email,
                        Password = Guid.NewGuid().ToString(),
                        FullName = googleResponse.Name
                    };
                    var isRegistered = await _authRepository.RegisterCandidate(newAccout);
                }
                var loginResponse = await _authRepository.LoginGoogle(googleResponse.Email); // có trả về accessTOken và refreshToken
                if (loginResponse == null)
                {
                    return Unauthorized(new { message = "Login failed. Please try again." });
                }
                return Ok(loginResponse);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}