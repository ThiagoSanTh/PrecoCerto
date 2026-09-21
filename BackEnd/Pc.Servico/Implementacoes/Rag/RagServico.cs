using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pc.Repositorio.Interfaces;
using Pc.Servico.Interfaces;
using Pc.Servico.Modelos.Rag;
using Pgvector;

namespace Pc.Servico.Implementacoes.Rag
{
    public class RagServico : IRagServico
    {
        private readonly IDocumentoRagRepositorio _docs;
        private readonly IEmbeddingService _embeddings;
        private readonly RagSettings _settings;
        private readonly ILogger<RagServico> _logger;

        public RagServico(
            IDocumentoRagRepositorio docs,
            IEmbeddingService embeddings,
            IOptions<RagSettings> settings,
            ILogger<RagServico> logger)
        {
            _docs = docs;
            _embeddings = embeddings;
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task<IReadOnlyList<RagSearchResult>> BuscarAsync(
            string consulta,
            int? limite = null,
            CancellationToken cancellationToken = default)
        {
            if (!_settings.EstaConfigurado)
            {
                _logger.LogWarning("RAG busca indisponível: não configurado.");
                return Array.Empty<RagSearchResult>();
            }

            if (string.IsNullOrWhiteSpace(consulta))
                return Array.Empty<RagSearchResult>();

            var top = Math.Clamp(limite ?? _settings.MaxResults, 1, 20);
            var embedding = await _embeddings.GerarEmbeddingAsync(consulta.Trim(), cancellationToken);
            // cosine distance = 1 - similarity; threshold 0.70 => max distance 0.30
            var maxDist = Math.Clamp(1.0 - _settings.SimilarityThreshold, 0.01, 1.0);

            var rows = await _docs.BuscarPorSimilaridadeAsync(
                new Vector(embedding),
                top,
                maxDist,
                cancellationToken);

            var resultados = rows.Select(r => new RagSearchResult
            {
                Tipo = r.Doc.Tipo,
                EntidadeId = r.Doc.EntidadeId,
                Titulo = r.Doc.Titulo,
                Conteudo = r.Doc.Conteudo,
                Score = Math.Round(1.0 - r.Distancia, 4),
                Metadata = r.Doc.Metadata
            }).ToList();

            _logger.LogInformation(
                "RAG busca. ConsultaLen={Len} Resultados={Qtd}",
                consulta.Length,
                resultados.Count);

            return resultados;
        }
    }
}
