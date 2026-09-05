using Pc.Dominio.Entities.Usuarios;
using Pc.Dominio.Enums;
using Pc.Servico.Interfaces;

namespace Pc.WebApi.Services
{
    /// <summary>
    /// Garante um admin de testes no banco (idempotente).
    /// </summary>
    public class AdminSeedHostedService : IHostedService
    {
        public const string EmailPadrao = "admin@precocerto.local";
        public const string SenhaPadrao = "Raymanorigins10";
        public const string NomePadrao = "Admin";

        private readonly IServiceScopeFactory _scopes;
        private readonly ILogger<AdminSeedHostedService> _logger;

        public AdminSeedHostedService(
            IServiceScopeFactory scopes,
            ILogger<AdminSeedHostedService> logger)
        {
            _scopes = scopes;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _ = SemearAsync(cancellationToken);
            return Task.CompletedTask;
        }

        private async Task SemearAsync(CancellationToken cancellationToken)
        {
            // Aguarda migrations do MigracaoStartupHostedService.
            await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);

            for (var tentativa = 1; tentativa <= 5; tentativa++)
            {
                try
                {
                    await using var scope = _scopes.CreateAsyncScope();
                    var admins = scope.ServiceProvider.GetRequiredService<IAdminServico>();
                    var existente = await admins.ObterPorEmailAsync(EmailPadrao);
                    if (existente is not null)
                    {
                        _logger.LogInformation("Admin seed já existe: {Email}", EmailPadrao);
                        return;
                    }

                    await admins.RegistrarAsync(new Admin
                    {
                        NomeUsuario = NomePadrao,
                        Email = EmailPadrao,
                        SenhaHash = SenhaPadrao,
                        NivelAcesso = 1,
                        Tipo = TipoUsuario.Admin
                    });

                    _logger.LogInformation("Admin seed criado: {Email}", EmailPadrao);
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Falha ao semear admin (tentativa {N}/5).", tentativa);
                    await Task.Delay(TimeSpan.FromSeconds(2 * tentativa), cancellationToken);
                }
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
