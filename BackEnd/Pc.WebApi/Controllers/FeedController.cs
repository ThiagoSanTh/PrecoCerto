using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Pc.Dominio.Enums;
using Pc.Servico.Interfaces;
using Pc.WebApi.DTOs.Catalogo;
using Pc.WebApi.Helpers;

namespace Pc.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FeedController : ControllerBase
    {
        private readonly IFeedServico _feedServico;

        public FeedController(IFeedServico feedServico)
        {
            _feedServico = feedServico;
        }

        /// <summary>Feed paginado com melhor preço/oferta por produto.</summary>
        [HttpGet]
        [AllowAnonymous]
        [EnableRateLimiting("catalogo")]
        public async Task<IActionResult> Listar(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? termo = null,
            [FromQuery] CategoriaProduto? categoria = null,
            [FromQuery] Guid? lojaId = null)
        {
            var paginacao = PaginacaoHelper.Normalizar(page, pageSize);
            var resultado = await _feedServico.ListarAsync(paginacao, termo, categoria, lojaId);

            return Ok(PaginacaoHelper.ParaResposta(resultado, MapFeed));
        }

        private static ProdutoFeedDto MapFeed(Pc.Servico.Modelos.FeedItem item) => new()
        {
            Id = item.ProdutoId,
            Nome = item.Nome,
            ImagemUrl = item.ImagemUrl,
            LojaId = item.LojaId,
            LojaNome = item.LojaNome,
            PrecoBase = item.PrecoBase,
            PrecoExibicao = item.PrecoExibicao,
            PrecoAnterior = item.PrecoAnterior,
            EmPromocao = item.EmPromocao,
            Categoria = item.Categoria,
            CategoriaNome = item.Categoria.ToString()
        };
    }
}
