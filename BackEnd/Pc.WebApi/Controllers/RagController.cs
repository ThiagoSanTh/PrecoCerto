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
        private readonly IRagIndexDlqServico _dlq;
        private readonly RagSettings _settings;
        private readonly ILogger<RagController> _logger;

        public RagController(
            IRagServico ragServico,
            IRagIndexadorServico indexador,
            IRagIndexDlqServico dlq,
            IOptions<RagSettings> settings,
            ILogger<RagController> logger)
        {
            _ragServico = ragServico;
            _indexador = indexador;
            _dlq = dlq;
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
        public async Task<IActionResult> Reindex(
            [FromQuery] bool somentePendentes = false,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation(
                "Reindexação RAG completa solicitada por admin. SomentePendentes={Pend}",
                somentePendentes);
            var resultado = await _indexador.ReindexarAsync(
                apenasTipo: null,
                somentePendentes: somentePendentes,
                cancellationToken: cancellationToken);
            return Ok(Mapear(resultado, somentePendentes
                ? "Reindexação dos pendentes concluída."
                : "Reindexação completa concluída."));
        }

        [HttpPost("reindex/{tipo}")]
        [ProducesResponseType(typeof(RagReindexResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ReindexTipo(
            string tipo,
            [FromQuery] bool somentePendentes = false,
            CancellationToken cancellationToken = default)
        {
            if (!Enum.TryParse<RagDocumentoTipo>(tipo, ignoreCase: true, out var parsed)
                || parsed == RagDocumentoTipo.Markdown)
            {
                return BadRequest(new { message = "Tipo inválido. Use Produto, Loja, Oferta ou Avaliacao." });
            }

            _logger.LogInformation(
                "Reindexação RAG parcial. Tipo={Tipo} SomentePendentes={Pend}",
                parsed,
                somentePendentes);
            var resultado = await _indexador.ReindexarAsync(parsed, somentePendentes, cancellationToken);
            return Ok(Mapear(resultado, somentePendentes
                ? $"Reindexação pendente de {parsed} concluída."
                : $"Reindexação de {parsed} concluída."));
        }

        [HttpGet("dlq")]
        [ProducesResponseType(typeof(RagDlqListResponseDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> ListarDlq([FromQuery] int limite = 50, CancellationToken cancellationToken = default)
        {
            var itens = await _dlq.ListarPendentesAsync(limite, cancellationToken);
            return Ok(new RagDlqListResponseDto
            {
                Sucesso = true,
                Total = itens.Count,
                Itens = itens.Select(i => new RagDlqItemResponseDto
                {
                    Id = i.Id,
                    Tipo = i.Tipo,
                    EntidadeId = i.EntidadeId,
                    Acao = i.Acao,
                    Tentativas = i.Tentativas,
                    UltimoErro = i.UltimoErro,
                    Status = i.Status,
                    CriadoEmUtc = i.CriadoEmUtc,
                    ReprocessadoEmUtc = i.ReprocessadoEmUtc
                }).ToList()
            });
        }

        [HttpPost("dlq/{id:guid}/retry")]
        public async Task<IActionResult> RetryDlq(Guid id, CancellationToken cancellationToken)
        {
            var ok = await _dlq.ReprocessarAsync(id, cancellationToken);
            if (!ok)
                return NotFound(new { message = "Item DLQ não encontrado ou já processado." });
            return Ok(new { sucesso = true, mensagem = "Evento reenfileirado." });
        }

        [HttpPost("dlq/retry-all")]
        public async Task<IActionResult> RetryAllDlq([FromQuery] int limite = 20, CancellationToken cancellationToken = default)
        {
            var n = await _dlq.ReprocessarPendentesAsync(limite, cancellationToken);
            return Ok(new { sucesso = true, reprocessados = n });
        }

        [HttpPost("dlq/{id:guid}/discard")]
        public async Task<IActionResult> DiscardDlq(Guid id, CancellationToken cancellationToken)
        {
            var ok = await _dlq.DescartarAsync(id, cancellationToken);
            if (!ok)
                return NotFound(new { message = "Item DLQ não encontrado ou já processado." });
            return Ok(new { sucesso = true, mensagem = "Item descartado." });
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
