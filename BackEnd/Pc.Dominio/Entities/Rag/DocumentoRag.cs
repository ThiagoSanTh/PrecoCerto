using System.ComponentModel.DataAnnotations.Schema;
using Pc.Dominio.Enums;
using Pgvector;

namespace Pc.Dominio.Entities.Rag
{
    /// <summary>
    /// Documento indexado para busca semântica (pgvector).
    /// UsuarioId null = conhecimento global; preenchido = memória futura por usuário.
    /// </summary>
    public class DocumentoRag
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public RagDocumentoTipo Tipo { get; set; }
        public Guid EntidadeId { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string Conteudo { get; set; } = string.Empty;

        [Column(TypeName = "vector(1536)")]
        public Vector? Embedding { get; set; }

        public string HashConteudo { get; set; } = string.Empty;
        public string? Metadata { get; set; }

        /// <summary>Null = RAG global. Futuro: memória semântica por usuário.</summary>
        public Guid? UsuarioId { get; set; }

        public bool Ativo { get; set; } = true;
        public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
        public DateTime? DataAtualizacao { get; set; }
    }
}
