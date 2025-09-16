using Capstone.Database;
using Capstone.DTOs.Auth;
using Capstone.Repositories;
using Capstone.Security;
using Capstone.Services;
using Microsoft.AspNetCore.Mvc;
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
        public AuthController(IAuthRepository authRepository, Redis redis, ILogger<AuthController> logger, EmailService service)
        {
            _authRepository = authRepository;
            _redis = redis;
            _logger = logger;
            _emailService = service;
        }
        public static string GenerateOTP(int length = 6)
        {
            if (length <= 0) throw new ArgumentException("Length must be positive", nameof(length));

            using var rng = RandomNumberGenerator.Create();
            var bytes = new byte[4]; // dùng để sinh số
            rng.GetBytes(bytes);
            int number = BitConverter.ToInt32(bytes, 0) & 0x7FFFFFFF; // đảm bảo số dương
            int otp = number % (int)Math.Pow(10, length);
            return otp.ToString($"D{length}"); // pad 0 nếu cần
        }
        // 1 Chuỗi API Quên mật khẩu
        [HttpPost("checkEmail")]
        public async Task<ActionResult> isEmailExist([FromBody] string email)
        {
            try
            {
                var isExist = await _authRepository.isEmailExist(email); // trả về accountId
                if (isExist == 0)
                {
                    return NotFound(new { message = "Email không tồn tại trong hệ thống." });
                }
               
                var otp = GenerateOTP();
                var otpHash = Hash.HashPassword(otp);
                await _redis.SetStringAsync($"OTP_{isExist}" , otpHash, TimeSpan.FromMinutes(5));
                // Gửi email chứa mã OTP
                string subject = "Mã OTP của bạn";
                await _emailService.SendEmailAsync(email, subject, otp);
                return Ok(new { message = "Mã OTP đã được gửi đến email của bạn." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi kiểm tra email hoặc gửi OTP.");
                return StatusCode(500, new { message = "Đã xảy ra lỗi trong quá trình xử lý." });
            }
        }

        [HttpPost("verifyOTP")]
        public async Task<ActionResult> verifyOTP([FromBody] VerifyOTP verifyOTP)
        {
            try
            {
              bool checkOTP = await _authRepository.verifyOTP(verifyOTP.AccountId, verifyOTP.OTP);
                if (!checkOTP)
                {
                    return BadRequest(new { message = "Mã OTP không hợp lệ hoặc đã hết hạn." });
                }
                return Ok(new { message = "Xác thực OTP thành công. Bạn có thể đặt lại mật khẩu mới." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xác thực OTP.");
                return StatusCode(500, new { message = "Đã xảy ra lỗi trong quá trình xử lý." });
            }
        }

        [HttpPost("resetPassword")]
        public async Task<ActionResult> resetPassword([FromBody] ResetPasswordDTO resetPasswordDTO)
        {
            try
            {
                bool isReset = await _authRepository.updateNewPassword(resetPasswordDTO.accountId, resetPasswordDTO.PasswordReset);
                if (!isReset)
                {
                    return BadRequest(new { message = "Không thể đặt lại mật khẩu. Vui lòng thử lại." });
                }
                // Xóa OTP sau khi đặt lại mật khẩu thành công
                await _redis.DeleteKeyAsync($"OTP_{resetPasswordDTO.accountId}");
                return Ok(new { message = "Đặt lại mật khẩu thành công." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi đặt lại mật khẩu.");
                return StatusCode(500, new { message = "Đã xảy ra lỗi trong quá trình xử lý." });
            }
        }
    }
}
