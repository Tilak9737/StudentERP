namespace StudentERP.Models
{
    public class SmtpSettings
    {
        public string Host { get; set; }
        public int Port { get; set; }

        public string Username { get; set; }
        public string Password { get; set; }
        public string FromEmail { get; set; }
        public string FromName { get; set; }
        public bool EnableSSL { get; set; }

    }
}