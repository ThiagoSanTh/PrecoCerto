using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Pc.Servico.Interfaces;
using Pc.Servico.Interfaces.MotorIA;
using Pc.Servico.Modelos.IA;
using Pc.Servico.Modelos.MotorIA;
using Pc.WebApi.DTOs.IA;
using Pc.WebApi.Extensions;

namespace Pc.WebApi.Controllers
{
    /// <summary>
    /// Assistente Preço Certo: chat protótipo + MotorIA v2.
    /// Isolado do chat cliente↔loja.
    /// </summary>
    [ApiController]
    [Route("api/IA")]
    [AllowAnonymous]
    [EnableRateLimiting("ia")]
    public class IAController : ControllerBase
    {
        private readonly IIAServico _iaServico;
        private readonly IMotorIA _motorIA;
        private readonly ILogger<IAController> _logger;

        public IAController(IIAServico iaServico, IMotorIA motorIA, ILogger<IAController> logger)
        {
            _iaServico = iaServico;
            _motorIA = motorIA;
            _logger = logger;
        }

        [HttpPost("chat")]
        [ProducesResponseType(typeof(IAChatResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> Chat(
            [FromBody] IAChatRequestDto request,
            CancellationToken cancellationToken)
        {
            if (request is null)
                return BadRequest(new { message = "Informe a mensagem." });

            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var mensagem = request.Mensagem?.Trim() ?? string.Empty;
            if (mensagem.Length > 500)
            {
                return Ok(new IAChatResponseDto
                {
                    Sucesso = true,
                    Intencao = "NaoEntendida",
                    Resposta = "Sua pergunta é muito longa. Tente resumir sua dúvida."
                });
            }

            var sw = Stopwatch.StartNew();
            var resultado = await _iaServico.ProcessarAsync(new IAChatPedido
            {
                Mensagem = mensagem,
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                UsuarioId = User.GetUserId()
            }, cancellationToken);
            sw.Stop();

            _logger.LogInformation(
                "IA chat: Intencao={Intencao} TempoMs={TempoMs} Sucesso={Sucesso}",
                resultado.Intencao,
                sw.ElapsedMilliseconds,
                resultado.Sucesso);

            return Ok(new IAChatResponseDto
            {
                Sucesso = resultado.Sucesso,
                Intencao = resultado.Intencao.ToString(),
                Resposta = resultado.Resposta,
                Dados = resultado.Dados
            });
        }

        /// <summary>
        /// MotorIA v2: interpretação determinística, regras, pontuação e recomendação.
        /// </summary>
        [HttpPost("analisar")]
        [ProducesResponseType(typeof(IAAnalisarResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Analisar(
            [FromBody] IAAnalisarRequestDto request,
            CancellationToken cancellationToken)
        {
            if (request is null || string.IsNullOrWhiteSpace(request.Mensagem))
                return BadRequest(new { message = "Informe a mensagem." });

            var mensagem = request.Mensagem.Trim();
            if (mensagem.Length > 500)
            {
                return Ok(new IAAnalisarResponseDto
                {
                    Sucesso = true,
                    Intencao = "NaoEntendida",
                    Resposta = "Sua pergunta é muito longa. Tente resumir sua dúvida.",
                    Confianca = 0
                });
            }

            var sw = Stopwatch.StartNew();
            var resultado = await _motorIA.AnalisarAsync(new PedidoAnaliseIA
            {
                Mensagem = mensagem,
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                UsuarioId = request.UsuarioId ?? User.GetUserId()
            }, cancellationToken);
            sw.Stop();

            _logger.LogInformation(
                "MotorIA analisar: Intencao={Intencao} Confianca={Conf} Resultados={Qtd} TempoMs={Ms}",
                resultado.Intencao,
                resultado.Confianca,
                resultado.Resultados.Count,
                sw.ElapsedMilliseconds);

            return Ok(new IAAnalisarResponseDto
            {
                Sucesso = resultado.Sucesso,
                Resposta = resultado.Resposta,
                Intencao = resultado.Intencao.ToString(),
                Objetivo = resultado.Objetivo?.ToString(),
                Confianca = resultado.Confianca,
                Resultados = resultado.Resultados.Select(r => new IAAnalisarItemDto
                {
                    Tipo = r.Tipo,
                    Titulo = r.Titulo,
                    Score = r.Score,
                    Preco = r.Preco,
                    DistanciaKm = r.DistanciaKm,
                    Loja = r.Loja,
                    Produto = r.Produto,
                    Motivos = r.Motivos
                }).ToList(),
                Motivos = resultado.Motivos,
                Fallbacks = resultado.Fallbacks
            });
        }
    }
}
