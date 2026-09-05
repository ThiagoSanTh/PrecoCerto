using System.ComponentModel.DataAnnotations;

namespace Pc.WebApi.DTOs.Rag
{
    public class RagSearchRequestDto
    {
        [Required]
        [MaxLength(500)]
        public string Consulta { get; set; } = string.Empty;

        [Range(1, 20)]
        public int? Limite { get; set; }
    }

    public class RagSearchItemDto
    {
        public string Tipo { get; set; } = string.Empty;
        public Guid EntidadeId { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string Conteudo { get; set; } = string.Empty;
        public double Score { get; set; }
        public string? Metadata { get; set; }
    }

    public class RagSearchResponseDto
    {
        public bool Sucesso { get; set; }
        public IReadOnlyList<RagSearchItemDto> Resultados { get; set; } = Array.Empty<RagSearchItemDto>();
        public string? Mensagem { get; set; }
    }

    public class RagReindexResponseDto
    {
        public bool Sucesso { get; set; }
        public string Mensagem { get; set; } = string.Empty;
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
