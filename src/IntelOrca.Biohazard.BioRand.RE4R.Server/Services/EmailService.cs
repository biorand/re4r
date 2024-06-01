using System;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Serilog;
using ILogger = Serilog.ILogger;
using MailMessage = System.Net.Mail.MailMessage;

namespace IntelOrca.Biohazard.BioRand.RE4R.Server.Services
{
    public class EmailService
    {
        private readonly EmailConfig? _config;
        private readonly ILogger _logger = Log.ForContext<EmailService>();

        public EmailService(Re4rConfiguration config)
        {
            _config = config.Email;
        }

        public async Task SendEmailAsync(string name, string email, string subject, string body)
        {
            if (string.IsNullOrEmpty(_config?.From))
                return;

            var to = $"{name} <{email}>";
            try
            {
                using var smtpClient = new SmtpClient();
                await smtpClient.ConnectAsync(_config.Host, _config.Port, SecureSocketOptions.None);
                await smtpClient.AuthenticateAsync(_config.Username, _config.Password);

                var message = MimeMessage.CreateFromMailMessage(new MailMessage(
                    _config.From,
                    to,
                    subject,
                    body));

                await smtpClient.SendAsync(message);
                _logger.Information("Email sent to {To}, Subject = {Subject}", to, subject);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to send email to {To}, Subject = {Subject}", to, subject);
            }
        }
    }
}
