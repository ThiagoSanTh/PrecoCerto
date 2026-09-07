using Pc.Dominio.Comum;
using Pc.Dominio.Entities.Catalogo;
using Pc.Dominio.Enums;

namespace Pc.Servico.Interfaces
{
    public interface IProdutoServico
    {
        Task<Produto> AdicionarAsync(Produto produto);
        Task<Produto?> ObterPorIdAsync(Guid id);
        Task<List<Produto>> ListarProdutosAsync(Guid? lojaId = null);
        Task<PaginacaoResultado<Produto>> ListarProdutosPaginadoAsync(
            PaginacaoParametros paginacao, Guid? lojaId = null, CategoriaProduto? categoria = null);
        Task<List<Produto>> BuscarPorNomeAsync(string nome, Guid? lojaId = null);
        Task<List<Produto>> BuscarPorTermosAsync(IEnumerable<string> termos, Guid? lojaId = null);
        Task<PaginacaoResultado<Produto>> BuscarPorNomePaginadoAsync(
            string nome, PaginacaoParametros paginacao, Guid? lojaId = null);
        Task AtualizarAsync(Produto produto);
        Task RemoverAsync(Guid id);
        Task AtualizarPorLojaAsync(Guid id, Produto dados, Guid lojaId);
        Task RemoverPorLojaAsync(Guid id, Guid lojaId);
    }
}