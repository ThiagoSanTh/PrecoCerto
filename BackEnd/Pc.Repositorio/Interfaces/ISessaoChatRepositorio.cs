using Pc.Dominio.Entities.MotorIA;

namespace Pc.Repositorio.Interfaces
{
    public interface ISessaoChatRepositorio
    {
        Task<SessaoChat> ObterOuCriarAsync(Guid? sessaoId, Guid? usuarioId, CancellationToken ct = default);
        Task<List<MensagemChat>> ListarUltimasMensagensAsync(Guid sessaoId, int limite = 10, CancellationToken ct = default);
        Task AdicionarTurnoAsync(Guid sessaoId, string textoUsuario, string textoAssistente, CancellationToken ct = default);
    }
}
