using Microsoft.EntityFrameworkCore;
using Pc.Infraestrutura;

namespace Pc.WebApi.Services
{
    /// <summary>
    /// Aplica migrations depois que o Kestrel já está escutando.
    /// No Railway o healthcheck mata o deploy se o start bloquear no Postgres.
    /// </summary>
    public class MigracaoStartupHostedService : IHostedService
    {
        private readonly IServiceScopeFactory _scopes;
        private readonly ILogger<MigracaoStartupHostedService> _logger;

        public MigracaoStartupHostedService(
            IServiceScopeFactory scopes,
            ILogger<MigracaoStartupHostedService> logger)
        {
            _scopes = scopes;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _ = AplicarAsync(cancellationToken);
            return Task.CompletedTask;
        }

        private async Task AplicarAsync(CancellationToken cancellationToken)
        {
            try
            {
                await using var scope = _scopes.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                await db.Database.MigrateAsync(cancellationToken);
                _logger.LogInformation("Migrations aplicadas com sucesso.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha ao aplicar migrations no banco. A API sobe mesmo assim para o healthcheck.");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
