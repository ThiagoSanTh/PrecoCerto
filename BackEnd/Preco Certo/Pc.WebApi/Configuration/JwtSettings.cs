namespace Pc.WebApi.Configuration
{
    public class JwtSettings
    {
        public string Key { get; set; } = string.Empty;
        public string Issuer { get; set; } = "PrecoCerto";
        public string Audience { get; set; } = "PrecoCertoMobile";
        public int ExpirationHours { get; set; } = 24;
    }
}
