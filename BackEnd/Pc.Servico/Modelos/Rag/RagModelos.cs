using Pc.Dominio.Enums;

namespace Pc.Servico.Modelos.Rag
{
    public enum RagIndexAcao
    {
        Indexar = 0,
        Remover = 1
    }

    public sealed class RagIndexEvento
    {
        public RagDocumentoTipo Tipo { get; init; }
        public Guid EntidadeId { get; init; }
        public RagIndexAcao Acao { get; init; }
        public int Tentativas { get; set; }
    }

    public sealed class RagDocumentoConstruido
    {
        public RagDocumentoTipo Tipo { get; init; }
        public Guid EntidadeId { get; init; }
        public string Titulo { get; init; } = string.Empty;
        public string Conteudo { get; init; } = string.Empty;
        public string? Metadata { get; init; }
        public bool DeveIndexar { get; init; } = true;
    }

    public sealed class RagSearchResult
    {
        public RagDocumentoTipo Tipo { get; init; }
        public Guid EntidadeId { get; init; }
        public string Titulo { get; init; } = string.Empty;
        public string Conteudo { get; init; } = string.Empty;
        public double Score { get; init; }
        public string? Metadata { get; init; }
    }

    public sealed class RagReindexResultado
    {
        public int ProdutosIndexados { get; set; }
        public int LojasIndexadas { get; set; }
        public int OfertasIndexadas { get; set; }
        public int AvaliacoesIndexadas { get; set; }
        public int TotalDocumentos { get; set; }
        public int TotalEmbeddingsGerados { get; set; }
        public int IgnoradosPorHash { get; set; }
        public int Erros { get; set; }
        public long TempoMs { get; set; }
    }
}
