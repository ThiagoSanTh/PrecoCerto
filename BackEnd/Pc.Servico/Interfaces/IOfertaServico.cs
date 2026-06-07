using Pc.Dominio.Entities.Catalogo;
using Pc.Dominio.Entities.Estabelecimentos;

namespace Pc.Servico.Interfaces
{
    public interface IOfertaServico
    {
        Task<Oferta> AdicionarAsync(Oferta oferta);
        Task<Oferta?> ObterPorIdAsync(Guid id);
        Task<List<Oferta>> ListarAsync();
        Task<List<Oferta>> ObterPorProdutoAsync(Guid produtoId);
        Task AtualizarAsync(Oferta oferta);
        Task RemoverAsync(Guid id);
    }
}