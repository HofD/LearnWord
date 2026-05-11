using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;

namespace IdentityService.Authorization.Services
{
    public class EmailService
    {
        private readonly IConfiguration configuration;

        public EmailService(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public async Task SendRegistrationEmailAsync(string email, string subject, string message)
        {
            var emailMessage = new MimeMessage();

            var fromName = configuration["Smtp:FromName"] ?? "Learn Word Online";
            var fromEmail = configuration["Smtp:FromEmail"] ?? "register@learnword.online";
            var host = configuration["Smtp:Host"] ?? "mail.learnword.online";
            var port = int.TryParse(configuration["Smtp:Port"], out var configuredPort) ? configuredPort : 465;
            var useSsl = !bool.TryParse(configuration["Smtp:UseSsl"], out var configuredUseSsl) || configuredUseSsl;
            var username = configuration["Smtp:Username"];
            var password = configuration["Smtp:Password"];

            emailMessage.From.Add(new MailboxAddress(fromName, fromEmail));
            emailMessage.To.Add(new MailboxAddress("", email));
            emailMessage.Subject = subject;
            emailMessage.Body = new TextPart(MimeKit.Text.TextFormat.Html)
            {
                Text = message
            };

            using (var client = new SmtpClient())
            {
                // TODO: remove when have production SSL certificate
                client.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => { return true; };
                await client.ConnectAsync(host, port, useSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTlsWhenAvailable);

                if (!string.IsNullOrWhiteSpace(username))
                {
                    await client.AuthenticateAsync(username, password);
                }

                await client.SendAsync(emailMessage);

                await client.DisconnectAsync(true);
            }
        }
    }
}
