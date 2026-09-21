using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pc.Servico.Interfaces;
using Pc.Servico.Interfaces.MotorIA;
using Pc.Servico.Modelos.Rag;

namespace Pc.Servico.Implementacoes.MotorIA
{
    public class RagConhecimentoIA : IRagConhecimentoIA
    {
        private readonly IRagServico _rag;
        private readonly RagSettings _settings;
        private readonly ILogger<RagConhecimentoIA> _logger;

        public RagConhecimentoIA(
            IRagServico rag,
            IOptions<RagSettings> settings,
            ILogger<RagConhecimentoIA> logger)
        {
            _rag = rag;
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task<IReadOnlyList<RagHitIA>> BuscarAuxiliarAsync(
            string consulta,
            int limite = 5,
            CancellationToken cancellationToken = default)
        {
            if (!_settings.EstaConfigurado)
            {
                _logger.LogInformation("RAG auxiliar indisponível (não configurado).");
                // #region agent log
                try
                {
                    var payload = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        sessionId = "6c7c29",
                        runId = "pre-fix",
                        hypothesisId = "B",
                        location = "RagConhecimentoIA.cs:BuscarAuxiliarAsync",
                        message = "rag-nao-configurado",
                        data = new { consulta, enabled = _settings.Enabled, hasKey = !string.IsNullOrWhiteSpace(_settings.ApiKey), dim = _settings.EmbeddingDimension },
                        timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                    });
                    System.IO.File.AppendAllText(@"d:\Dev\PrecoCerto\debug-6c7c29.log", payload + "\n");
                }
                catch { /* debug */ }
                // #endregion
                return Array.Empty<RagHitIA>();
            }

            try
            {
                var hits = await _rag.BuscarAsync(consulta, limite, cancellationToken);
                // #region agent log
                try
                {
                    var payload = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        sessionId = "6c7c29",
                        runId = "pre-fix",
                        hypothesisId = "C,D",
                        location = "RagConhecimentoIA.cs:BuscarAuxiliarAsync",
                        message = "rag-busca-ok",
                        data = new { consulta, hitCount = hits.Count, provider = _settings.Provider, threshold = _settings.SimilarityThreshold },
                        timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                    });
                    System.IO.File.AppendAllText(@"d:\Dev\PrecoCerto\debug-6c7c29.log", payload + "\n");
                }
                catch { /* debug */ }
                // #endregion
                return hits.Select(h => new RagHitIA
                {
                    Tipo = h.Tipo.ToString(),
                    EntidadeId = h.EntidadeId,
                    Titulo = h.Titulo,
                    Conteudo = h.Conteudo,
                    Score = h.Score
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Falha no RAG auxiliar — degradando graciosamente.");
                // #region agent log
                try
                {
                    var payload = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        sessionId = "6c7c29",
                        runId = "pre-fix",
                        hypothesisId = "B",
                        location = "RagConhecimentoIA.cs:catch",
                        message = "rag-exception",
                        data = new { consulta, error = ex.GetType().Name, ex.Message },
                        timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                    });
                    System.IO.File.AppendAllText(@"d:\Dev\PrecoCerto\debug-6c7c29.log", payload + "\n");
                }
                catch { /* debug */ }
                // #endregion
                return Array.Empty<RagHitIA>();
            }
        }
    }
}
