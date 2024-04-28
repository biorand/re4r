using System;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Swan.Logging;
using MailMessage = System.Net.Mail.MailMessage;

namespace IntelOrca.Biohazard.BioRand.RE4R.Server.Services
{
    internal class EmailService(EmailConfig? config)
    {
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

                $"Sending e-mail to {to}: {body}".Log(typeof(EmailService), LogLevel.Info);
                await smtpClient.SendAsync(message);
            }
            catch (Exception ex)
            {
                ex.Log(typeof(EmailService));
            }
        }
    }
}
