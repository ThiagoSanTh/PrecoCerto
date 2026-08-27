namespace Pc.Servico.Implementacoes
{
    /// <summary>
    /// Configuração do clima. Provedor padrão: Open-Meteo
    /// (https://open-meteo.com) — cobertura Brasil, forecast horária/diária,
    /// uso comercial (CC BY 4.0), sem API key no MVP.
    /// Cidade via Nominatim. ApiKey reservada para troca futura de provedor.
    /// </summary>
    public class ClimaSettings
    {
        public const string SectionName = "Clima";

        public string ForecastBaseUrl { get; set; } = "https://api.open-meteo.com/v1/forecast";

        /// <summary>
        /// Reverse geocode (Nominatim/OSM). Open-Meteo não oferece reverse estável.
        /// </summary>
        public string GeocodingBaseUrl { get; set; } = "https://nominatim.openstreetmap.org/reverse";

        /// <summary>Reservado. Open-Meteo não exige chave.</summary>
        public string? ApiKey { get; set; }

        /// <summary>TTL do clima atual e previsões no cache em memória.</summary>
        public int CacheMinutos { get; set; } = 15;

        /// <summary>Quanto tempo um resultado antigo ainda serve de fallback se o provedor falhar.</summary>
        public int CacheStaleHoras { get; set; } = 6;

        /// <summary>Casas decimais para agrupar GPS da mesma região (~1,1 km com 2 casas).</summary>
        public int PrecisaoCoordenadas { get; set; } = 2;

        public int TimeoutSegundos { get; set; } = 8;

        public int HorasPrevisao { get; set; } = 24;
        public int DiasPrevisao { get; set; } = 7;
    }
}
