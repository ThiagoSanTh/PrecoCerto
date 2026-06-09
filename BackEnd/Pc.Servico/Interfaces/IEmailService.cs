namespace Pc.Servico.Interfaces
{
    /// <summary>
    /// Abstração de envio de e-mail. A implementação concreta (SMTP) fica na WebApi.
    /// </summary>
    public interface IEmailService
    {
        /// <summary>
        /// Envia o e-mail de confirmação de cadastro com o link/token.
        /// Implementações devem ser tolerantes a falhas (não quebrar o cadastro).
        /// </summary>
        /// <param name="destinatario">E-mail do usuário.</param>
        /// <param name="nome">Nome de exibição.</param>
        /// <param name="token">Token de confirmação.</param>
        /// <param name="tipo">Tipo de usuário (cliente/lojista).</param>
        Task EnviarConfirmacaoEmailAsync(string destinatario, string nome, string token, string tipo);
    }
}
