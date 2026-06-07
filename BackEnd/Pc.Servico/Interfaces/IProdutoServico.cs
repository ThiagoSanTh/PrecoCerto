using Pc.Dominio.Entities.Catalogo;

namespace Pc.Servico.Interfaces
{
    public interface IProdutoServico
    {
        Task<Produto> AdicionarAsync(Produto produto);
        Task<Produto?> ObterPorIdAsync(Guid id);
        Task<List<Produto>> ListarProdutosAsync(Guid? lojaId = null);
        Task<List<Produto>> BuscarPorNomeAsync(string nome, Guid? lojaId = null);
        Task AtualizarAsync(Produto produto);
        Task RemoverAsync(Guid id);

        /// <summary>Atualiza produto somente se pertencer à loja informada.</summary>
        Task AtualizarPorLojaAsync(Guid id, Produto dados, Guid lojaId);

        /// <summary>Remove produto somente se pertencer à loja informada.</summary>
        Task RemoverPorLojaAsync(Guid id, Guid lojaId);
    }
}