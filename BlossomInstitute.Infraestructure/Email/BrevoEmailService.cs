using BlossomInstitute.Application.External;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace BlossomInstitute.Infraestructure.Email
{
    public class BrevoEmailService : IEmailService
    {
        private const string BrevoSendEmailUrl = "https://api.brevo.com/v3/smtp/email";

        private readonly HttpClient _httpClient;
        private readonly EmailSettings _settings;
        private readonly ILogger<BrevoEmailService> _logger;

        public BrevoEmailService(
            HttpClient httpClient,
            IOptions<EmailSettings> options, // leer confg de appsettings
            ILogger<BrevoEmailService> logger)
        {
            _httpClient = httpClient;
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

            if (string.IsNullOrWhiteSpace(_settings.ApiKey))
                throw new InvalidOperationException("Email:ApiKey no está configurado.");

            if (string.IsNullOrWhiteSpace(_settings.FromEmail))
                throw new InvalidOperationException("Email:FromEmail no está configurado.");

            if (string.IsNullOrWhiteSpace(_settings.FromName))
                throw new InvalidOperationException("Email:FromName no está configurado.");

            // carga que Brevo espera    
            var payload = new
            {
                sender = new
                {
                    name = _settings.FromName,
                    email = _settings.FromEmail
                },
                to = new[]
                {
                    new
                    {
                        email = toEmail
                    }
                },
                subject = subject ?? string.Empty,
                htmlContent = htmlBody ?? string.Empty
            };

            var json = JsonSerializer.Serialize(payload);

            // crear request
            using var request = new HttpRequestMessage(HttpMethod.Post, BrevoSendEmailUrl);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.Add("api-key", _settings.ApiKey);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            using var response = await _httpClient.SendAsync(request, ct);
            var responseBody = await response.Content.ReadAsStringAsync(ct);

            // validar respuesta
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "Error enviando email con Brevo a {ToEmail}. Status: {StatusCode}. Response: {ResponseBody}",
                    toEmail,
                    (int)response.StatusCode,
                    responseBody);

                throw new InvalidOperationException(
                    $"Brevo devolvió {(int)response.StatusCode}: {responseBody}");
            }

            _logger.LogInformation("Email enviado con Brevo API a {ToEmail}", toEmail);
        }
    }
}
