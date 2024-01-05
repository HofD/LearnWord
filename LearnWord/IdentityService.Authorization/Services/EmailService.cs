using MailKit.Net.Smtp;
using MimeKit;

namespace IdentityService.Authorization.Services
{
    public class EmailService
    {
        public async Task SendRegistrationEmailAsync(string email, string subject, string message)
        {
            var emailMessage = new MimeMessage();

            emailMessage.From.Add(new MailboxAddress("Learn Word Online", "register@learnword.online"));
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
                await client.ConnectAsync("mail.learnword.online", 465, true);
                await client.AuthenticateAsync("register@learnword.online", "770sFwNkpbI9h3iuSTOX");
                await client.SendAsync(emailMessage);

                await client.DisconnectAsync(true);
            }
        }
    }
}
