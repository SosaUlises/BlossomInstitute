using BlossomInstitute.Application.External;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;

namespace BlossomInstitute.Infraestructure.Email
{
    public class SmtpEmailService : IEmailService
    {
        private readonly EmailSettings _settings;
        private readonly ILogger<SmtpEmailService> _logger;

        public SmtpEmailService(
            IOptions<EmailSettings> options,
            ILogger<SmtpEmailService> logger)
        {
            _settings = options.Value;
            _logger = logger;
        }

        public async Task SendAsync(
            string toEmail,
            string subject,
            string htmlBody,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(toEmail))
                throw new ArgumentException("toEmail es obligatorio", nameof(toEmail));

            if (string.IsNullOrWhiteSpace(_settings.Host))
                throw new InvalidOperationException("Email:Host no está configurado.");

            if (string.IsNullOrWhiteSpace(_settings.Username))
                throw new InvalidOperationException("Email:Username no está configurado.");

            if (string.IsNullOrWhiteSpace(_settings.Password))
                throw new InvalidOperationException("Email:Password no está configurado.");

            if (string.IsNullOrWhiteSpace(_settings.FromEmail))
                throw new InvalidOperationException("Email:FromEmail no está configurado.");

            using var message = new MailMessage
            {
                From = new MailAddress(_settings.FromEmail, _settings.FromName),
                Subject = subject ?? string.Empty,
                Body = htmlBody ?? string.Empty,
                IsBodyHtml = true
            };

            message.To.Add(toEmail);

            using var client = new SmtpClient(_settings.Host, _settings.Port)
            {
                EnableSsl = _settings.UseSsl,
                Credentials = new NetworkCredential(_settings.Username, _settings.Password),
                DeliveryMethod = SmtpDeliveryMethod.Network
            };

            try
            {
                await client.SendMailAsync(message, ct);
                _logger.LogInformation("Email enviado vía SMTP a {ToEmail}", toEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enviando email SMTP a {ToEmail}", toEmail);
                throw;
            }
        }
    }
}