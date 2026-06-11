using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using Pc.Servico.Interfaces;
using Pc.WebApi.Configuration;

namespace Pc.WebApi.Services
{
    public class SmtpEmailService : IEmailService
    {
        private readonly EmailSettings _settings;
        private readonly ILogger<SmtpEmailService> _logger;

        public SmtpEmailService(IOptions<EmailSettings> settings, ILogger<SmtpEmailService> logger)
        {
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task EnviarBoasVindasAsync(string destinatario, string nome)
        {
            var corpo =
                $"Olá {nome},\n\n" +
                "Seja bem-vindo(a) ao Preço Certo!\n\n" +
                "Sua conta foi criada com sucesso. Agora você pode comparar preços, " +
                "encontrar ofertas perto de você e favoritar produtos e lojas.\n\n" +
                "Obrigado por fazer parte da nossa comunidade.\n\n" +
                "Equipe Preço Certo";

            await EnviarAsync(destinatario, "Bem-vindo(a) ao Preço Certo!", corpo, "boas-vindas");
        }

        public async Task EnviarRecuperacaoSenhaAsync(string destinatario, string token)
        {
            var link = $"{_settings.AppBaseUrl.TrimEnd('/')}/redefinir-senha?token={token}";
            var corpo =
                "Recebemos uma solicitação para redefinir sua senha no Preço Certo.\n\n" +
                $"Use o token abaixo no aplicativo (válido por 1 hora):\n\n{token}\n\n" +
                $"Ou acesse: {link}\n\n" +
                "Se você não solicitou, ignore este e-mail.";

            await EnviarAsync(destinatario, "Redefinição de senha - Preço Certo", corpo, "recuperação-senha");
        }

        public async Task EnviarNotificacaoAlteracaoEmailAsync(string destinatarioAntigo, string novoEmail, string nome)
        {
            var corpo =
                $"Olá {nome},\n\n" +
                $"O e-mail da sua conta no Preço Certo foi alterado para {novoEmail}.\n\n" +
                "Se você não fez esta alteração, entre em contato conosco imediatamente.";

            await EnviarAsync(destinatarioAntigo, "Alteração de e-mail - Preço Certo", corpo, "alteração-e-mail");
        }

        private async Task EnviarAsync(string destinatario, string assunto, string corpo, string tipoLog)
        {
            if (!_settings.Configurado)
            {
                _logger.LogWarning(
                    "SMTP não configurado. E-mail de {Tipo} para {Email} não enviado.",
                    tipoLog, destinatario);
                return;
            }

            try
            {
                using var mensagem = new MailMessage
                {
                    From = new MailAddress(_settings.From, _settings.FromName),
                    Subject = assunto,
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
                _logger.LogInformation("E-mail de {Tipo} enviado para {Email}.", tipoLog, destinatario);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha ao enviar e-mail de {Tipo} para {Email}.", tipoLog, destinatario);
            }
        }
    }
}
