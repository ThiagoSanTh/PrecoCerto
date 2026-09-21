using Pc.Dominio.Entities.Rag;
using Pc.Dominio.Enums;
using Pgvector;

namespace Pc.Repositorio.Interfaces
{
    public interface IDocumentoRagRepositorio
    {
        Task<DocumentoRag?> ObterGlobalAsync(RagDocumentoTipo tipo, Guid entidadeId, CancellationToken ct = default);
        Task UpsertAsync(DocumentoRag documento, CancellationToken ct = default);
        Task SoftDeleteAsync(RagDocumentoTipo tipo, Guid entidadeId, CancellationToken ct = default);
        Task<IReadOnlyList<(DocumentoRag Doc, double Distancia)>> BuscarPorSimilaridadeAsync(
            Vector embedding,
            int limite,
            double maxDistancia,
            CancellationToken ct = default);
        Task<int> ContarAtivosAsync(CancellationToken ct = default);
    }
}
