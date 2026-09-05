namespace Pc.Servico.Modelos.Rag
{
    public class RagSettings
    {
        public const string SectionName = "Rag";

        public bool Enabled { get; set; } = true;
        public string Provider { get; set; } = "OpenAI";
        public string EmbeddingModel { get; set; } = "text-embedding-3-small";
        public int EmbeddingDimension { get; set; } = 1536;
        public string ApiKey { get; set; } = string.Empty;
        public string ApiBaseUrl { get; set; } = "https://api.openai.com/v1";
        public int BatchSize { get; set; } = 50;
        public int MaxResults { get; set; } = 5;
        public double SimilarityThreshold { get; set; } = 0.70;
        public int MaxRetries { get; set; } = 5;
        public bool RunInitialIndexOnStartup { get; set; } = true;
        public int TimeoutSegundos { get; set; } = 30;

        public bool EstaConfigurado =>
            Enabled
            && !string.IsNullOrWhiteSpace(ApiKey)
            && EmbeddingDimension == 1536;
    }
}
