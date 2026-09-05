using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pc.Servico.Interfaces;
using Pc.Servico.Modelos.Rag;

namespace Pc.Servico.Implementacoes.Rag
{
    /// <summary>
    /// Dispara a indexação inicial uma vez após o startup (se habilitado e configurado).
    /// </summary>
    public class RagInitialIndexHostedService : IHostedService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly RagSettings _settings;
        private readonly ILogger<RagInitialIndexHostedService> _logger;
        private static int _jaExecutou;

        public RagInitialIndexHostedService(
            IServiceScopeFactory scopeFactory,
            IOptions<RagSettings> settings,
            ILogger<RagInitialIndexHostedService> logger)
        {
            _scopeFactory = scopeFactory;
            _settings = settings.Value;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            if (!_settings.RunInitialIndexOnStartup || !_settings.EstaConfigurado)
            {
                _logger.LogInformation("RAG indexação inicial no startup desabilitada ou sem ApiKey.");
                return Task.CompletedTask;
            }

            if (Interlocked.Exchange(ref _jaExecutou, 1) == 1)
                return Task.CompletedTask;

            _ = Task.Run(async () =>
            {
                try
                {
                    // Aguarda migrate do startup
                    await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
                    using var scope = _scopeFactory.CreateScope();
                    var indexador = scope.ServiceProvider.GetRequiredService<IRagIndexadorServico>();
                    var resultado = await indexador.ReindexarAsync(cancellationToken: cancellationToken);
                    _logger.LogInformation(
                        "RAG indexação inicial: Produtos={P} Lojas={L} Ofertas={O} Avaliacoes={A} Embeddings={E} Ignorados={I} Erros={Er} TempoMs={T}",
                        resultado.ProdutosIndexados,
                        resultado.LojasIndexadas,
                        resultado.OfertasIndexadas,
                        resultado.AvaliacoesIndexadas,
                        resultado.TotalEmbeddingsGerados,
                        resultado.IgnoradosPorHash,
                        resultado.Erros,
                        resultado.TempoMs);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Falha na indexação inicial RAG (API continua operacional).");
                }
            }, cancellationToken);

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
