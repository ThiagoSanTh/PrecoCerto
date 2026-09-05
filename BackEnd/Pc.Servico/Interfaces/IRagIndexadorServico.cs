using Pc.Dominio.Enums;
using Pc.Servico.Modelos.Rag;

namespace Pc.Servico.Interfaces
{
    public interface IRagIndexadorServico
    {
        Task ProcessarEventoAsync(RagIndexEvento evento, CancellationToken cancellationToken = default);
        Task<RagReindexResultado> ReindexarAsync(
            RagDocumentoTipo? apenasTipo = null,
            CancellationToken cancellationToken = default);
    }
}
