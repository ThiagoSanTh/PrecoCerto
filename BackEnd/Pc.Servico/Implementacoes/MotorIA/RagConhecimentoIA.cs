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
                return Array.Empty<RagHitIA>();
            }

            try
            {
                var hits = await _rag.BuscarAsync(consulta, limite, cancellationToken);
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
                return Array.Empty<RagHitIA>();
            }
        }
    }
}
