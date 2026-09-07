using Pc.Dominio.Enums;
using Pc.Servico.Modelos.Rag;

namespace Pc.Servico.Interfaces
{
    /// <summary>
    /// Enfileira reindexação em cascata para manter documentos RAG coerentes
    /// quando loja/produto mudam (ofertas dependem do texto de ambos).
    /// </summary>
    public interface IRagCascadeIndexador
    {
        Task EnfileirarLojaAtualizadaAsync(Guid lojaId, CancellationToken cancellationToken = default);
        Task EnfileirarLojaRemovidaAsync(Guid lojaId, CancellationToken cancellationToken = default);
        Task EnfileirarProdutoAtualizadoAsync(Guid produtoId, CancellationToken cancellationToken = default);
        Task EnfileirarProdutoRemovidoAsync(Guid produtoId, CancellationToken cancellationToken = default);
    }

    public interface IRagIndexDlqServico
    {
        Task RegistrarFalhaAsync(RagIndexEvento evento, string erro, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<RagDlqItemDto>> ListarPendentesAsync(int limite = 50, CancellationToken cancellationToken = default);
        Task<bool> ReprocessarAsync(Guid dlqId, CancellationToken cancellationToken = default);
        Task<int> ReprocessarPendentesAsync(int limite = 20, CancellationToken cancellationToken = default);
        Task<bool> DescartarAsync(Guid dlqId, CancellationToken cancellationToken = default);
    }

    public sealed class RagDlqItemDto
    {
        public Guid Id { get; init; }
        public string Tipo { get; init; } = string.Empty;
        public Guid EntidadeId { get; init; }
        public string Acao { get; init; } = string.Empty;
        public int Tentativas { get; init; }
        public string UltimoErro { get; init; } = string.Empty;
        public string Status { get; init; } = string.Empty;
        public DateTime CriadoEmUtc { get; init; }
        public DateTime? ReprocessadoEmUtc { get; init; }
    }
}
