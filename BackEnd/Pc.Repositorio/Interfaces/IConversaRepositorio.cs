using Pc.Dominio.Entities.Interacoes;

namespace Pc.Repositorio.Interfaces
{
    public interface IConversaRepositorio
    {
        Task<Conversa?> ObterPorIdAsync(Guid id);
        Task<Conversa?> ObterPorClienteELojaAsync(Guid clienteId, Guid lojaId);
        Task<List<Conversa>> ListarPorClienteAsync(Guid clienteId);
        Task<List<Conversa>> ListarPorLojaAsync(Guid lojaId);
        Task<Conversa> AdicionarAsync(Conversa conversa);
        Task AtualizarAsync(Conversa conversa);
        Task<List<Mensagem>> ListarMensagensAsync(Guid conversaId, DateTime? apos, DateTime? antes = null, int pageSize = 50);
        Task<Dictionary<Guid, int>> ContarNaoLidasPorConversasAsync(Guid usuarioId, bool ehLojista, Guid? lojaId);
        Task<Mensagem> AdicionarMensagemAsync(Mensagem mensagem);
        Task MarcarMensagensComoLidasAsync(Guid conversaId, Guid leitorId);
        Task MarcarMensagensComoRecebidasAsync(Guid conversaId, Guid leitorId);
        Task<int> ContarNaoLidasAsync(Guid usuarioId, bool ehLojista, Guid? lojaId);
        Task<bool> TemNaoLidasAsync(Guid usuarioId, bool ehLojista, Guid? lojaId, DateTime? desde = null);
    }
}
