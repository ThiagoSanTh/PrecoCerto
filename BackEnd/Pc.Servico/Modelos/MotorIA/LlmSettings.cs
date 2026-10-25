namespace Pc.Servico.Modelos.MotorIA
{
    public class LlmSettings
    {
        public const string SectionName = "Llm";

        public string GeminiApiKey { get; set; } = string.Empty;
        public string GeminiModel { get; set; } = "gemini-2.5-flash";
        public string GeminiBaseUrl { get; set; } = "https://generativelanguage.googleapis.com/v1beta";

        public string NvidiaApiKey { get; set; } = string.Empty;
        public string NvidiaModel { get; set; } = "meta/llama-3.3-70b-instruct";
        public string NvidiaBaseUrl { get; set; } = "https://integrate.api.nvidia.com/v1";

        public int TimeoutMs { get; set; } = 3500;
        public int CooldownSegundos { get; set; } = 300;
    }

    public class LlmInterpretacaoResultado
    {
        public string Provedor { get; set; } = "Deterministico";
        public string? Intencao { get; set; }
        public string? Objetivo { get; set; }
        public List<string> ItensBusca { get; set; } = new();
        public string? ProdutoTermo { get; set; }
        public string? LojaTermo { get; set; }
        public string? Categoria { get; set; }
        public decimal? OrcamentoMax { get; set; }
        public decimal? DistanciaMaxKm { get; set; }
        public bool UrgenciaAlta { get; set; }
        public bool NecessitaEntrega { get; set; }
    }
}
