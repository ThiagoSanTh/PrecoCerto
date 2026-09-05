using Pc.Servico.Modelos.Rag;

namespace Pc.Servico.Interfaces
{
    public interface IRagServico
    {
        Task<IReadOnlyList<RagSearchResult>> BuscarAsync(
            string consulta,
            int? limite = null,
            CancellationToken cancellationToken = default);
    }
}
