using System.Net;
using System.Net.Mail;

namespace Capstone.Services
{
    public class EmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            var smtpSection = _config.GetSection("SmtpSettings");
            var senderEmail = smtpSection["SenderEmail"];
            var senderName = smtpSection["SenderName"];
            var password = smtpSection["Password"];
            var server = smtpSection["Server"];
            var port = int.Parse(smtpSection["Port"]);

            using var client = new SmtpClient(server, port)
            {
                Credentials = new NetworkCredential(senderEmail, password),
                EnableSsl = true
            };

            var mail = new MailMessage
            {
                From = new MailAddress(senderEmail, senderName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };

            mail.To.Add(to);

            await client.SendMailAsync(mail);
        }
    }
}
