namespace Pc.Servico.Modelos.Rag
{
    public class RagSettings
    {
        public const string SectionName = "Rag";

        public bool Enabled { get; set; } = true;

        /// <summary>OpenAI | Gemini</summary>
        public string Provider { get; set; } = "Gemini";

        public string EmbeddingModel { get; set; } = "gemini-embedding-001";
        public int EmbeddingDimension { get; set; } = 1536;
        public string ApiKey { get; set; } = string.Empty;
        public string ApiBaseUrl { get; set; } = "https://generativelanguage.googleapis.com/v1beta";
        public int BatchSize { get; set; } = 50;
        public int MaxResults { get; set; } = 5;
        public double SimilarityThreshold { get; set; } = 0.70;
        public int MaxRetries { get; set; } = 5;
        public bool RunInitialIndexOnStartup { get; set; } = false;
        public int TimeoutSegundos { get; set; } = 30;

        /// <summary>
        /// Pausa entre embeddings no reindex (evita 429 do Gemini). 0 = sem pausa.
        /// Free tier Gemini costuma precisar de 800–1500ms.
        /// </summary>
        public int DelayEntreEmbeddingsMs { get; set; } = 800;

        public bool EhGemini =>
            string.Equals(Provider, "Gemini", StringComparison.OrdinalIgnoreCase)
            || string.Equals(Provider, "Google", StringComparison.OrdinalIgnoreCase);

        public bool EhOpenAi =>
            string.Equals(Provider, "OpenAI", StringComparison.OrdinalIgnoreCase);

        public bool EstaConfigurado =>
            Enabled
            && !string.IsNullOrWhiteSpace(ApiKey)
            && EmbeddingDimension == 1536;
    }
}
