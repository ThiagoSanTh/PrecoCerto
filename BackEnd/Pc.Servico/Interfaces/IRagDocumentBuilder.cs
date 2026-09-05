using Pc.Dominio.Enums;
using Pc.Servico.Modelos.Rag;

namespace Pc.Servico.Interfaces
{
    public interface IRagDocumentBuilder
    {
        Task<RagDocumentoConstruido?> ConstruirAsync(
            RagDocumentoTipo tipo,
            Guid entidadeId,
            CancellationToken cancellationToken = default);
    }
}
