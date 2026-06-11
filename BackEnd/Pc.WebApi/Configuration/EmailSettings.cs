namespace Pc.WebApi.Configuration
{
    public class EmailSettings
    {
        public const string SectionName = "Email";

        public string Host { get; set; } = string.Empty;
        public int Port { get; set; } = 587;
        public bool EnableSsl { get; set; } = true;
        public string User { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string From { get; set; } = string.Empty;
        public string FromName { get; set; } = "Preço Certo";

        /// <summary>URL pública base da API, usada para montar o link de confirmação.</summary>
        public string AppBaseUrl { get; set; } = "http://localhost:5132";

        public bool Configurado => !string.IsNullOrWhiteSpace(Host) && !string.IsNullOrWhiteSpace(From);
    }
}
