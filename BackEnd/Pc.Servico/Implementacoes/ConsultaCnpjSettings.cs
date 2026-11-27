namespace Pc.Servico.Implementacoes
{
    public class ConsultaCnpjSettings
    {
        public const string SectionName = "ConsultaCnpj";

        public string BrasilApiBaseUrl { get; set; } = "https://brasilapi.com.br/api";
        public int CacheMinutos { get; set; } = 60;
    }
}
