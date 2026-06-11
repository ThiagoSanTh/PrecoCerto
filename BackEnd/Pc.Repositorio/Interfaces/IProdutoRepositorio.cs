using Pc.Dominio.Comum;
using Pc.Dominio.Entities.Catalogo;
using Pc.Dominio.Enums;

namespace Pc.Repositorio.Interfaces
{
    public interface IProdutoRepositorio : IRepositorio<Produto>
    {
        Task<List<Produto>> BuscarPorNomeAsync(string nome, Guid? lojaId = null);
        Task<PaginacaoResultado<Produto>> BuscarPorNomePaginadoAsync(
            string nome, PaginacaoParametros paginacao, Guid? lojaId = null);
        Task<List<Produto>> ListarPorLojaAsync(Guid? lojaId = null);
        Task<PaginacaoResultado<Produto>> ListarPorLojaPaginadoAsync(
            PaginacaoParametros paginacao, Guid? lojaId = null, CategoriaProduto? categoria = null);
        Task<bool> AtualizarCamposAsync(Produto produto);
        Task<bool> RemoverPorIdAsync(Guid id);
    }
}
