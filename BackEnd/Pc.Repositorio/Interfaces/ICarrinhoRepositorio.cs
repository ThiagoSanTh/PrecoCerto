using Pc.Dominio.Entities.Interacoes;

namespace Pc.Repositorio.Interfaces
{
    public interface ICarrinhoRepositorio : IRepositorio<Carrinho>
    {
        /// <summary>Obtém o carrinho do cliente com itens, produtos e ofertas.</summary>
        Task<Carrinho?> ObterPorClienteAsync(Guid clienteId);

        Task<ItemCarrinho?> ObterItemAsync(Guid itemId);
        Task<ItemCarrinho> AdicionarItemAsync(ItemCarrinho item);
        Task AtualizarItemAsync(ItemCarrinho item);
        Task RemoverItemAsync(ItemCarrinho item);
    }
}
