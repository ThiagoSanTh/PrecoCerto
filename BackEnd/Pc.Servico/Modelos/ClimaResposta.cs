namespace Pc.Servico.Modelos
{
    /// <summary>
    /// Clima normalizado, independente do provedor.
    /// Estruturado para um futuro Context Builder / LLM — não é texto livre.
    /// </summary>
    public class ClimaResposta
    {
        public ClimaLocalidade Localidade { get; set; } = new();
        public ClimaAtual Atual { get; set; } = new();
        public List<ClimaPrevisao> Horaria { get; set; } = new();
        public List<ClimaPrevisao> Diaria { get; set; } = new();
        public DateTime ObtidoEm { get; set; } = DateTime.UtcNow;
        public bool DeCache { get; set; }
        public bool Desatualizado { get; set; }
        public string Provedor { get; set; } = "open-meteo";
    }

    public class ClimaLocalidade
    {
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public string? Cidade { get; set; }
        public string? Estado { get; set; }
        public string? Pais { get; set; }
    }

    public class ClimaAtual
    {
        public decimal? Temperatura { get; set; }
        public decimal? SensacaoTermica { get; set; }
        public int? Umidade { get; set; }
        public decimal? VelocidadeVento { get; set; }
        public decimal? Precipitacao { get; set; }
        public int? ProbabilidadePrecipitacao { get; set; }
        public int? CodigoClima { get; set; }
        public string Descricao { get; set; } = string.Empty;
        public string Icone { get; set; } = "cloudy";
        public DateTime? Momento { get; set; }
    }

    public class ClimaPrevisao
    {
        public DateTime DataHora { get; set; }
        public decimal? Temperatura { get; set; }
        public decimal? TemperaturaMinima { get; set; }
        public decimal? TemperaturaMaxima { get; set; }
        public decimal? SensacaoTermica { get; set; }
        public decimal? Precipitacao { get; set; }
        public int? ProbabilidadePrecipitacao { get; set; }
        public int? CodigoClima { get; set; }
        public string Descricao { get; set; } = string.Empty;
        public string Icone { get; set; } = "cloudy";
    }
}
