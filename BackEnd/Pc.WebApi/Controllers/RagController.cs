using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Pc.Dominio.Enums;
using Pc.Servico.Interfaces;
using Pc.Servico.Modelos.Rag;
using Pc.WebApi.DTOs.Rag;

namespace Pc.WebApi.Controllers
{
    [ApiController]
    [Route("api/Rag")]
    [Authorize(Roles = "Admin")]
    [EnableRateLimiting("rag")]
    public class RagController : ControllerBase
    {
        private readonly IRagServico _ragServico;
        private readonly IRagIndexadorServico _indexador;
        private readonly RagSettings _settings;
        private readonly ILogger<RagController> _logger;

        public RagController(
            IRagServico ragServico,
            IRagIndexadorServico indexador,
            IOptions<RagSettings> settings,
            ILogger<RagController> logger)
        {
            _ragServico = ragServico;
            _indexador = indexador;
            _settings = settings.Value;
            _logger = logger;
        }

        [HttpPost("search")]
        [ProducesResponseType(typeof(RagSearchResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        public async Task<IActionResult> Search(
            [FromBody] RagSearchRequestDto request,
            CancellationToken cancellationToken)
        {
            if (request is null || string.IsNullOrWhiteSpace(request.Consulta))
                return BadRequest(new { message = "Informe a consulta." });

            if (!_settings.EstaConfigurado)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new RagSearchResponseDto
                {
                    Sucesso = false,
                    Mensagem = "RAG não configurado. Defina Rag__ApiKey."
                });
            }

            var resultados = await _ragServico.BuscarAsync(request.Consulta, request.Limite, cancellationToken);
            return Ok(new RagSearchResponseDto
            {
                Sucesso = true,
                Resultados = resultados.Select(r => new RagSearchItemDto
                {
                    Tipo = r.Tipo.ToString(),
                    EntidadeId = r.EntidadeId,
                    Titulo = r.Titulo,
                    Conteudo = r.Conteudo,
                    Score = r.Score,
                    Metadata = r.Metadata
                }).ToList()
            });
        }

        [HttpPost("reindex")]
        [ProducesResponseType(typeof(RagReindexResponseDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> Reindex(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Reindexação RAG completa solicitada por admin.");
            var resultado = await _indexador.ReindexarAsync(cancellationToken: cancellationToken);
            return Ok(Mapear(resultado, "Reindexação completa concluída."));
        }

        [HttpPost("reindex/{tipo}")]
        [ProducesResponseType(typeof(RagReindexResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ReindexTipo(string tipo, CancellationToken cancellationToken)
        {
            if (!Enum.TryParse<RagDocumentoTipo>(tipo, ignoreCase: true, out var parsed)
                || parsed == RagDocumentoTipo.Markdown)
            {
                return BadRequest(new { message = "Tipo inválido. Use Produto, Loja, Oferta ou Avaliacao." });
            }

            _logger.LogInformation("Reindexação RAG parcial. Tipo={Tipo}", parsed);
            var resultado = await _indexador.ReindexarAsync(parsed, cancellationToken);
            return Ok(Mapear(resultado, $"Reindexação de {parsed} concluída."));
        }

        private static RagReindexResponseDto Mapear(RagReindexResultado r, string mensagem) => new()
        {
            Sucesso = r.Erros == 0,
            Mensagem = mensagem,
            ProdutosIndexados = r.ProdutosIndexados,
            LojasIndexadas = r.LojasIndexadas,
            OfertasIndexadas = r.OfertasIndexadas,
            AvaliacoesIndexadas = r.AvaliacoesIndexadas,
            TotalDocumentos = r.TotalDocumentos,
            TotalEmbeddingsGerados = r.TotalEmbeddingsGerados,
            IgnoradosPorHash = r.IgnoradosPorHash,
            Erros = r.Erros,
            TempoMs = r.TempoMs
        };
    }
}
