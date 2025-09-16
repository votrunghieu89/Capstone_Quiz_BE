using Capstone.Services;
using Microsoft.AspNetCore.Mvc;

namespace Capstone.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
    {
        private readonly EmailService _emailService;

        public TestController(EmailService emailService)
        {
            _emailService = emailService;
        }

        [HttpPost("send")]
        public async Task<IActionResult> SendMail(string toEmail)
        {
            string subject = "Mã OTP của bạn";
            string body = "OTP của bạn là: <b>123456</b>";

            await _emailService.SendEmailAsync(toEmail, subject, body);

            return Ok(new { message = "Đã gửi mail thành công!" });
        }
    }
}
