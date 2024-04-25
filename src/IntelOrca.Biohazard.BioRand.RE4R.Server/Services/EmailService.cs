using System.Threading.Tasks;
using Swan.Logging;

namespace IntelOrca.Biohazard.BioRand.RE4R.Server.Services
{
    internal class EmailService
    {
        public Task SendEmailAsync(string email, string body)
        {
            $"Sending e-mail to {email}: {body}".Log(typeof(EmailService), LogLevel.Info);
            return Task.CompletedTask;
        }
    }
}
