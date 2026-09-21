using Pc.Dominio.Comum;
using Pc.Dominio.Entities.Catalogo;
using Pc.Dominio.Enums;

namespace Pc.Repositorio.Interfaces
{
    public interface IProdutoRepositorio : IRepositorio<Produto>
    {
        Task<List<Produto>> BuscarPorNomeAsync(string nome, Guid? lojaId = null);
        /// <summary>Busca unindo resultados de vários termos (token/sinônimo), ordenados por relevância.</summary>
        Task<List<Produto>> BuscarPorTermosAsync(IEnumerable<string> termos, Guid? lojaId = null);
        Task<PaginacaoResultado<Produto>> BuscarPorNomePaginadoAsync(
            string nome, PaginacaoParametros paginacao, Guid? lojaId = null);
        Task<List<Produto>> ListarPorLojaAsync(Guid? lojaId = null);
        Task<List<Guid>> ListarIdsPorLojaAsync(Guid lojaId, int limite = 500);
        Task<PaginacaoResultado<Produto>> ListarPorLojaPaginadoAsync(
            PaginacaoParametros paginacao, Guid? lojaId = null, CategoriaProduto? categoria = null);
        Task<PaginacaoResultado<FeedProdutoLinha>> ListarFeedPaginadoAsync(
            PaginacaoParametros paginacao,
            string? termo = null,
            CategoriaProduto? categoria = null,
            Guid? lojaId = null);
        Task<bool> AtualizarCamposAsync(Produto produto);
        Task<bool> RemoverPorIdAsync(Guid id);
    }
}
