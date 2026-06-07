namespace Pc.WebApi.Configuration
{
    public class JwtSettings
    {
        public const string SectionName = "Jwt";

        public string Secret { get; set; } = string.Empty;
        public string Issuer { get; set; } = "PrecoCerto";
        public string Audience { get; set; } = "PrecoCertoApp";
        public int ExpirationHours { get; set; } = 24;
    }
}
