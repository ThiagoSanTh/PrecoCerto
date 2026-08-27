using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Pc.Servico.Excecoes;
using Pc.Servico.Implementacoes;
using Pc.Servico.Interfaces;
using Pc.Servico.Modelos;

namespace Pc.WebApi.Controllers
{
    /// <summary>
    /// Clima da região do usuário. Consome o provedor no backend — o app nunca fala com a API meteorológica.
    /// </summary>
    [ApiController]
    [Route("api/Weather")]
    [AllowAnonymous]
    [EnableRateLimiting("catalogo")]
    public class WeatherController : ControllerBase
    {
        private readonly IClimaServico _climaServico;
        private readonly ILogger<WeatherController> _logger;

        public WeatherController(IClimaServico climaServico, ILogger<WeatherController> logger)
        {
            _climaServico = climaServico;
            _logger = logger;
        }

        /// <summary>
        /// Clima normalizado para latitude/longitude. GPS do app; chave do provedor só no servidor.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ClimaResposta), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        public async Task<IActionResult> Obter(
            [FromQuery] decimal? latitude,
            [FromQuery] decimal? longitude,
            CancellationToken cancellationToken)
        {
            if (latitude is null || longitude is null)
                return BadRequest(new { message = "Informe latitude e longitude." });

            if (!ClimaServico.CoordenadasValidas(latitude.Value, longitude.Value))
                return BadRequest(new { message = "Latitude ou longitude fora do intervalo válido." });

            try
            {
                var clima = await _climaServico.ObterPorCoordenadasAsync(
                    latitude.Value,
                    longitude.Value,
                    cancellationToken);
                return Ok(clima);
            }
            catch (ArgumentOutOfRangeException)
            {
                return BadRequest(new { message = "Latitude ou longitude inválida." });
            }
            catch (ClimaIndisponivelException ex)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado ao consultar clima.");
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "Consulta de clima temporariamente indisponível." });
            }
        }
    }
}
