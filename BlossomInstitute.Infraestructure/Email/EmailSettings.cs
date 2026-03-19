namespace BlossomInstitute.Infraestructure.Email
{
    public class EmailSettings
    {
        public string Provider { get; set; } = "Brevo";
        public string ApiKey { get; set; } = string.Empty;
        public string FromEmail { get; set; } = string.Empty;
        public string FromName { get; set; } = string.Empty;
    }
}
