using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pc.Servico.Interfaces;
using Pc.Servico.Modelos.Rag;

namespace Pc.Servico.Implementacoes.Rag
{
    public class RagIndexWorker : BackgroundService
    {
        private readonly IRagIndexFila _fila;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly RagSettings _settings;
        private readonly ILogger<RagIndexWorker> _logger;

        public RagIndexWorker(
            IRagIndexFila fila,
            IServiceScopeFactory scopeFactory,
            IOptions<RagSettings> settings,
            ILogger<RagIndexWorker> logger)
        {
            _fila = fila;
            _scopeFactory = scopeFactory;
            _settings = settings.Value;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("RAG Index Worker iniciado.");

            await foreach (var evento in _fila.Reader.ReadAllAsync(stoppingToken))
            {
                try
                {
                    await ProcessarComRetryAsync(evento, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro não tratado no worker RAG. Tipo={Tipo} Id={Id}",
                        evento.Tipo, evento.EntidadeId);
                    await TentarRegistrarDlqAsync(evento, ex.ToString(), stoppingToken);
                }
                finally
                {
                    _fila.Liberar(evento);
                }
            }
        }

        private async Task ProcessarComRetryAsync(RagIndexEvento evento, CancellationToken ct)
        {
            var max = Math.Clamp(_settings.MaxRetries, 1, 10);
            Exception? ultima = null;

            for (var tentativa = 0; tentativa < max; tentativa++)
            {
                evento.Tentativas = tentativa + 1;
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var indexador = scope.ServiceProvider.GetRequiredService<IRagIndexadorServico>();
                    await indexador.ProcessarEventoAsync(evento, ct);
                    _logger.LogInformation(
                        "Evento processado. Tipo={Tipo} Id={Id} Tentativa={Tentativa}",
                        evento.Tipo, evento.EntidadeId, evento.Tentativas);
                    return;
                }
                catch (RagEmbeddingAuthException ex)
                {
                    // Não faz sentido retry em 401/403 — key inválida.
                    ultima = ex;
                    _logger.LogError(
                        ex,
                        "RAG auth falhou; sem retry. Tipo={Tipo} Id={Id}",
                        evento.Tipo, evento.EntidadeId);
                    break;
                }
                catch (Exception ex) when (tentativa < max - 1)
                {
                    ultima = ex;
                    var delay = TimeSpan.FromSeconds(Math.Pow(2, tentativa));
                    _logger.LogWarning(
                        ex,
                        "Retry executado. Tipo={Tipo} Id={Id} Tentativa={Tentativa} DelaySec={Delay}",
                        evento.Tipo, evento.EntidadeId, evento.Tentativas, delay.TotalSeconds);
                    await Task.Delay(delay, ct);
                }
                catch (Exception ex)
                {
                    ultima = ex;
                    _logger.LogError(
                        ex,
                        "Erro no provider / indexação após retries. Tipo={Tipo} Id={Id}",
                        evento.Tipo, evento.EntidadeId);
                }
            }

            if (ultima is not null)
                await TentarRegistrarDlqAsync(evento, ultima.ToString(), ct);
        }

        private async Task TentarRegistrarDlqAsync(RagIndexEvento evento, string erro, CancellationToken ct)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var dlq = scope.ServiceProvider.GetRequiredService<IRagIndexDlqServico>();
                await dlq.RegistrarFalhaAsync(evento, erro, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha ao gravar RAG DLQ. Tipo={Tipo} Id={Id}", evento.Tipo, evento.EntidadeId);
            }
        }
    }
}
