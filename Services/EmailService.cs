using GOGOFOOD.Services;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace GOGOFOOD.Services
{
    public class EmailService
    {
        private readonly IConfiguration _configuration;
        private readonly LogService _logService;

        public EmailService(IConfiguration configuration, LogService logService)
        {
            _configuration = configuration;
            _logService = logService;
        }

        public void SendEmail(string toEmail, string subject, string body)
        {
            try
            {
                string smtpServer = _configuration["EmailSettings:SmtpServer"] ?? "";
                string portText = _configuration["EmailSettings:Port"] ?? "587";
                string senderName = _configuration["EmailSettings:SenderName"] ?? "";
                string senderEmail = _configuration["EmailSettings:SenderEmail"] ?? "";
                string username = _configuration["EmailSettings:Username"] ?? "";
                string password = _configuration["EmailSettings:Password"] ?? "";

                if (string.IsNullOrEmpty(smtpServer))
                {
                    _logService.AddLog(
                        "Email Settings Error",
                        "SMTP server is empty. The application could not read EmailSettings:SmtpServer from appsettings.json.",
                        toEmail,
                        "System"
                    );

                    return;
                }

                if (string.IsNullOrEmpty(senderEmail) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                {
                    _logService.AddLog(
                        "Email Settings Error",
                        "Sender email, username, or password is empty. Check EmailSettings in appsettings.json.",
                        toEmail,
                        "System"
                    );

                    return;
                }

                int port = int.Parse(portText);

                var email = new MimeMessage();

                email.From.Add(new MailboxAddress(senderName, senderEmail));
                email.To.Add(MailboxAddress.Parse(toEmail));
                email.Subject = subject;

                email.Body = new TextPart("html")
                {
                    Text = body
                };

                using var smtp = new SmtpClient();

                smtp.Connect(smtpServer, port, SecureSocketOptions.StartTls);

                smtp.Authenticate(username, password);

                smtp.Send(email);

                smtp.Disconnect(true);

                _logService.AddLog(
                    "Email Sent",
                    "Email sent successfully to: " + toEmail,
                    toEmail,
                    "System"
                );
            }
            catch (Exception ex)
            {
                _logService.AddLog(
                    "Email Error",
                    "Email could not be sent to " + toEmail + ". Error: " + ex.Message,
                    toEmail,
                    "System"
                );
            }
        }
    }
}