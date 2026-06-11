using Pc.Servico.Interfaces;

namespace Pc.Servico.Interfaces
{
    public interface IEmailService
    {
        Task EnviarBoasVindasAsync(string destinatario, string nome);

        Task EnviarRecuperacaoSenhaAsync(string destinatario, string token);

        Task EnviarNotificacaoAlteracaoEmailAsync(string destinatarioAntigo, string novoEmail, string nome);
    }
}
