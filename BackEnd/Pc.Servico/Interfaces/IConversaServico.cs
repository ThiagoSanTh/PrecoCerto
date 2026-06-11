using Pc.Dominio.Entities.Interacoes;
using Pc.Dominio.Enums;

namespace Pc.Servico.Interfaces
{
    public interface IConversaServico
    {
        Task<List<Conversa>> ListarDoUsuarioAsync(Guid usuarioId, PapelUsuario papel, Guid? lojaId);
        Task<Conversa> AbrirComLojaAsync(Guid clienteId, Guid lojaId);
        Task<Conversa?> ObterPorIdAsync(Guid conversaId);
        Task<List<Mensagem>> ListarMensagensAsync(
            Guid conversaId, Guid usuarioId, PapelUsuario papel, Guid? lojaId,
            DateTime? apos, DateTime? antes = null, int pageSize = 50);
        Task<Mensagem> EnviarMensagemAsync(Guid conversaId, Guid remetenteId, PapelUsuario remetentePapel, string texto);
        Task MarcarComoLidasAsync(Guid conversaId, Guid leitorId);
        Task<int> ContarNaoLidasAsync(Guid usuarioId, PapelUsuario papel, Guid? lojaId);
        Task<Dictionary<Guid, int>> ContarNaoLidasPorConversasAsync(Guid usuarioId, PapelUsuario papel, Guid? lojaId);
        Task<bool> UsuarioPodeAcessarAsync(Guid conversaId, Guid usuarioId, PapelUsuario papel, Guid? lojaId);
    }
}
