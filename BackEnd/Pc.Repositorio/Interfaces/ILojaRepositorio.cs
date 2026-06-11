using Pc.Dominio.Comum;
using Pc.Dominio.Entities.Estabelecimentos;

namespace Pc.Repositorio.Interfaces
{
    public interface ILojaRepositorio : IRepositorio<Loja>
    {
        Task<List<Loja>> BuscarPorNomeAsync(string nome);
        Task<PaginacaoResultado<Loja>> BuscarPorNomePaginadoAsync(string nome, PaginacaoParametros paginacao);
        Task<PaginacaoResultado<Loja>> ListarPaginadoAsync(PaginacaoParametros paginacao);
        Task<List<Loja>> ListarPorProximidadeAsync(decimal latitude, decimal longitude, decimal raioKm, int limite = 500);
        Task<Loja?> ObterPorUsuarioIdAsync(Guid usuarioId);
    }
}
