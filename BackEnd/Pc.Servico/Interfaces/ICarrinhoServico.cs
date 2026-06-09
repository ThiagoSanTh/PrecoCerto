using Pc.Dominio.Entities.Interacoes;

namespace Pc.Servico.Interfaces
{
    public interface ICarrinhoServico
    {
        /// <summary>Obtém o carrinho do cliente, criando um vazio se não existir.</summary>
        Task<Carrinho> ObterOuCriarAsync(Guid clienteId);

        /// <summary>Adiciona um item (ou soma quantidade se o produto já estiver no carrinho).</summary>
        Task<Carrinho> AdicionarItemAsync(Guid clienteId, Guid produtoId, int quantidade, decimal precoUnitario, Guid? ofertaId);

        /// <summary>Atualiza a quantidade de um item; quantidade &lt;= 0 remove o item.</summary>
        Task<Carrinho> AtualizarQuantidadeAsync(Guid clienteId, Guid itemId, int quantidade);

        /// <summary>Remove um item do carrinho.</summary>
        Task<Carrinho> RemoverItemAsync(Guid clienteId, Guid itemId);

        /// <summary>Esvazia o carrinho do cliente.</summary>
        Task LimparAsync(Guid clienteId);
    }
}
