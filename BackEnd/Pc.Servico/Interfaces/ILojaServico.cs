using Pc.Dominio.Comum;
using Pc.Dominio.Entities.Estabelecimentos;

namespace Pc.Servico.Interfaces
{
    public interface ILojaServico
    {
        Task<Loja> AdicionarAsync(Loja loja);
        Task<Loja?> ObterPorIdAsync(Guid id);
        Task<Loja?> ObterPorUsuarioIdAsync(Guid usuarioId);
        Task<List<Loja>> ListarAsync();
        Task<PaginacaoResultado<Loja>> ListarPaginadoAsync(PaginacaoParametros paginacao);
        Task<List<Loja>> BuscarPorNomeAsync(string nome);
        Task<PaginacaoResultado<Loja>> BuscarPorNomePaginadoAsync(string nome, PaginacaoParametros paginacao);
        Task<List<Loja>> ListarPorProximidadeAsync(decimal latitude, decimal longitude, decimal raioKm, int limite = 500);
        Task AtualizarAsync(Loja loja);
        Task RemoverAsync(Guid id);
    }
}
