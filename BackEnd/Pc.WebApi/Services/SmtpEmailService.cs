using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using Pc.Servico.Interfaces;
using Pc.WebApi.Configuration;

namespace Pc.WebApi.Services
{
    /// <summary>
    /// Implementação SMTP do envio de e-mail. É tolerante a falhas: se o SMTP
    /// não estiver configurado ou ocorrer erro, apenas registra um log e não
    /// interrompe o fluxo de cadastro.
    /// </summary>
    public class SmtpEmailService : IEmailService
    {
        private readonly EmailSettings _settings;
        private readonly ILogger<SmtpEmailService> _logger;

        public SmtpEmailService(IOptions<EmailSettings> settings, ILogger<SmtpEmailService> logger)
        {
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task EnviarConfirmacaoEmailAsync(string destinatario, string nome, string token, string tipo)
        {
            var link = $"{_settings.AppBaseUrl.TrimEnd('/')}/api/auth/confirmar-email?token={token}&tipo={tipo}";

            if (!_settings.Configurado)
            {
                _logger.LogWarning(
                    "SMTP não configurado. E-mail de confirmação para {Email} não enviado. Link: {Link}",
                    destinatario, link);
                return;
            }

            var corpo =
                $"Olá {nome},\n\n" +
                "Obrigado por se cadastrar no Preço Certo!\n" +
                "Confirme seu e-mail acessando o link abaixo:\n\n" +
                link + "\n\n" +
                "Se você não criou esta conta, ignore esta mensagem.";

            try
            {
                using var mensagem = new MailMessage
                {
                    From = new MailAddress(_settings.From, _settings.FromName),
                    Subject = "Confirme seu e-mail - Preço Certo",
                    Body = corpo,
                    IsBodyHtml = false
                };
                mensagem.To.Add(destinatario);

                using var client = new SmtpClient(_settings.Host, _settings.Port)
                {
                    EnableSsl = _settings.EnableSsl,
                    Credentials = new NetworkCredential(_settings.User, _settings.Password)
                };

                await client.SendMailAsync(mensagem);
                _logger.LogInformation("E-mail de confirmação enviado para {Email}.", destinatario);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha ao enviar e-mail de confirmação para {Email}.", destinatario);
            }
        }
    }
}
