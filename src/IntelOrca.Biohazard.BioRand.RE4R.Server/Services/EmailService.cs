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
    internal class EmailService(EmailConfig? config)
    {
        private readonly ILogger _logger = Log.ForContext<EmailService>();

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            if (string.IsNullOrEmpty(config?.From))
                return;

            try
            {
                using var smtpClient = new SmtpClient();
                await smtpClient.ConnectAsync(config.Host, config.Port, SecureSocketOptions.None);
                await smtpClient.AuthenticateAsync(config.Username, config.Password);

                var message = MimeMessage.CreateFromMailMessage(new MailMessage(
                    config.From,
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
