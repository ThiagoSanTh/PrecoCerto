using Pc.Dominio.Entities.Estabelecimentos;

namespace Pc.Servico.Interfaces
{
    public interface ILojaServico
    {
        Task<Loja> AdicionarAsync(Loja loja);
        Task<Loja?> ObterPorIdAsync(Guid id);
        Task<List<Loja>> ListarAsync();
        Task<List<Loja>> BuscarPorNomeAsync(string nome);
        Task AtualizarAsync(Loja loja);
        Task RemoverAsync(Guid id);
    }
}
