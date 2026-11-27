using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Pc.Servico.Interfaces.MotorIA;
using Pc.Servico.Modelos.MotorIA;
using Pc.WebApi.Authorization;
using Pc.WebApi.DTOs.IA;
using Pc.WebApi.Extensions;

namespace Pc.WebApi.Controllers
{
    /// <summary>
    /// Assistente Preço Certo: MotorIA.
    /// Isolado do chat cliente↔loja.
    /// </summary>
    [ApiController]
    [Route("api/IA")]
    [Authorize]
    [EnableRateLimiting("ia")]
    public class IAController : ControllerBase
    {
        private readonly IMotorIA _motorIA;
        private readonly ILogger<IAController> _logger;

        public IAController(IMotorIA motorIA, ILogger<IAController> logger)
        {
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
            var resultado = await _motorIA.AnalisarAsync(new PedidoAnaliseIA
            {
                Mensagem = mensagem,
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                UsuarioId = User.GetUserId(),
                SessaoId = request.SessaoId
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
                SessaoId = resultado.SessaoId,
                Intencao = resultado.Intencao.ToString(),
                Resposta = resultado.Resposta,
                ProvedorLlm = resultado.ProvedorLlm,
                Fallbacks = resultado.Fallbacks,
                Dados = resultado.Resultados
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

            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

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

            var modo = request.ModoOperacional?.Trim().ToLowerInvariant();
            Guid? lojaAlvo = null;
            if (modo == "loja" || request.LojaIdUsuario.HasValue)
            {
                lojaAlvo = request.LojaIdUsuario ?? User.GetLojaId();
                if (!lojaAlvo.HasValue || !Authz.OwnsLoja(this, lojaAlvo.Value))
                    return Forbid();
            }

            var historicoSanitizado = request.Historico?
                .Where(h => h is not null && !string.IsNullOrWhiteSpace(h.Texto))
                .TakeLast(8)
                .Select(h =>
                {
                    var limpo = h.Texto.Replace("\0", string.Empty).Trim();
                    return new MensagemHistoricoIA
                    {
                        Papel = string.Equals(h.Papel, "user", StringComparison.OrdinalIgnoreCase) ? "user" : "ia",
                        Texto = limpo.Length > 500 ? limpo[..500] : limpo
                    };
                })
                .ToList() ?? new List<MensagemHistoricoIA>();

            var sw = Stopwatch.StartNew();
            var resultado = await _motorIA.AnalisarAsync(new PedidoAnaliseIA
            {
                Mensagem = mensagem,
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                UsuarioId = User.GetUserId(),
                SessaoId = request.SessaoId,
                LojaIdUsuario = lojaAlvo,
                ModoOperacional = modo == "loja" ? "loja" : "cliente",
                Historico = historicoSanitizado
            }, cancellationToken);
            sw.Stop();

            _logger.LogInformation(
                "MotorIA analisar: Intencao={Intencao} Confianca={Conf} Resultados={Qtd} Provedor={Prov} TempoMs={Ms}",
                resultado.Intencao,
                resultado.Confianca,
                resultado.Resultados.Count,
                resultado.ProvedorLlm,
                sw.ElapsedMilliseconds);

            return Ok(new IAAnalisarResponseDto
            {
                Sucesso = resultado.Sucesso,
                SessaoId = resultado.SessaoId,
                Resposta = resultado.Resposta,
                Intencao = resultado.Intencao.ToString(),
                Objetivo = resultado.Objetivo?.ToString(),
                Confianca = resultado.Confianca,
                ProvedorLlm = resultado.ProvedorLlm,
                Resultados = resultado.Resultados.Select(r => new IAAnalisarItemDto
                {
                    Tipo = r.Tipo,
                    Titulo = r.Titulo,
                    ProdutoId = r.ProdutoId,
                    LojaId = r.LojaId,
                    Score = r.Score,
                    Preco = r.Preco,
                    EmPromocao = r.EmPromocao,
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
