using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Pc.Dominio.Entities.Estabelecimentos;
using Pc.Servico.Interfaces;
using Pc.WebApi.Authorization;
using Pc.WebApi.DTOs.Estabelecimentos;
using Pc.WebApi.Helpers;

namespace Pc.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OfertasController : ControllerBase
    {
        private readonly IOfertaServico _ofertaServico;

        public OfertasController(IOfertaServico ofertaServico)
        {
            _ofertaServico = ofertaServico;
        }

        [HttpGet]
        [AllowAnonymous]
        [EnableRateLimiting("catalogo")]
        public async Task<IActionResult> Listar([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var paginacao = PaginacaoHelper.Normalizar(page, pageSize);
            var ofertas = await _ofertaServico.ListarPaginadoAsync(paginacao);
            return Ok(PaginacaoHelper.ParaResposta(ofertas, MapResposta));
        }

        [HttpGet("{id:guid}")]
        [AllowAnonymous]
        public async Task<IActionResult> ObterPorId(Guid id)
        {
            var oferta = await _ofertaServico.ObterPorIdAsync(id);

            if (oferta == null)
                return NotFound("Oferta nùo encontrada.");

            return Ok(MapResposta(oferta));
        }

        [HttpGet("produto/{produtoId:guid}")]
        [AllowAnonymous]
        [EnableRateLimiting("catalogo")]
        public async Task<IActionResult> ObterPorProduto(Guid produtoId)
        {
            var ofertas = await _ofertaServico.ObterPorProdutoAsync(produtoId);
            return Ok(ofertas.Select(MapResposta));
        }

        [HttpPost]
        [Authorize(Roles = "Lojista,Vendedor,Admin")]
        public async Task<IActionResult> Adicionar([FromBody] OfertaCriarDto dto)
        {
            if (!Authz.OwnsLoja(this, dto.LojaId))
                return Forbid();
            var oferta = new Oferta
            {
                ProdutoId = dto.ProdutoId,
                LojaId = dto.LojaId,
                Preco = dto.Preco,
                PrecoAnterior = dto.PrecoAnterior,
                EmPromocao = dto.EmPromocao,
                DataInicioPromocao = dto.DataInicioPromocao,
                DataFimPromocao = dto.DataFimPromocao,
                QuantidadeEstoque = dto.QuantidadeEstoque,
                Disponivel = dto.Disponivel
            };

            var novaOferta = await _ofertaServico.AdicionarAsync(oferta);

            var resposta = new OfertaRespostaDto
            {
                Id = novaOferta.Id,
                ProdutoId = novaOferta.ProdutoId,
                LojaId = novaOferta.LojaId,
                Preco = novaOferta.Preco,
                PrecoAnterior = novaOferta.PrecoAnterior,
                EmPromocao = novaOferta.EmPromocao,
                DataInicioPromocao = novaOferta.DataInicioPromocao,
                DataFimPromocao = novaOferta.DataFimPromocao,
                Disponivel = novaOferta.Disponivel,
                DataAtualizacaoPreco = novaOferta.DataAtualizacaoPreco
            };

            return CreatedAtAction(nameof(ObterPorId), new { id = resposta.Id }, resposta);
        }

        [HttpPut("{id:guid}")]
        [Authorize(Roles = "Lojista,Vendedor,Admin")]
        public async Task<IActionResult> Atualizar(Guid id, [FromBody] OfertaCriarDto dto)
        {
            if (!Authz.OwnsLoja(this, dto.LojaId))
                return Forbid();
            var ofertaExistente = await _ofertaServico.ObterPorIdAsync(id);

            if (ofertaExistente == null)
                return NotFound("Oferta nùo encontrada.");

            ofertaExistente.ProdutoId = dto.ProdutoId;
            ofertaExistente.LojaId = dto.LojaId;
            ofertaExistente.Preco = dto.Preco;
            ofertaExistente.PrecoAnterior = dto.PrecoAnterior;
            ofertaExistente.EmPromocao = dto.EmPromocao;
            ofertaExistente.DataInicioPromocao = dto.DataInicioPromocao;
            ofertaExistente.DataFimPromocao = dto.DataFimPromocao;
            ofertaExistente.QuantidadeEstoque = dto.QuantidadeEstoque;
            ofertaExistente.Disponivel = dto.Disponivel;

            await _ofertaServico.AtualizarAsync(ofertaExistente);

            return NoContent();
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Lojista,Vendedor,Admin")]
        public async Task<IActionResult> Remover(Guid id)
        {
            var ofertaExistente = await _ofertaServico.ObterPorIdAsync(id);

            if (ofertaExistente == null)
                return NotFound("Oferta nùo encontrada.");

            if (!Authz.OwnsLoja(this, ofertaExistente.LojaId))
                return Forbid();

            await _ofertaServico.RemoverAsync(id);
            return NoContent();
        }

        private static OfertaRespostaDto MapResposta(Oferta oferta) => new()
        {
            Id = oferta.Id,
            ProdutoId = oferta.ProdutoId,
            NomeProduto = oferta.Produto?.NomeProduto ?? string.Empty,
            MarcaProduto = oferta.Produto?.Marca ?? string.Empty,
            LojaId = oferta.LojaId,
            NomeLoja = oferta.Loja?.NomeFantasia ?? string.Empty,
            Preco = oferta.Preco,
            PrecoAnterior = oferta.PrecoAnterior,
            EmPromocao = oferta.EmPromocao,
            DataInicioPromocao = oferta.DataInicioPromocao,
            DataFimPromocao = oferta.DataFimPromocao,
            Disponivel = oferta.Disponivel,
            DataAtualizacaoPreco = oferta.DataAtualizacaoPreco
        };
    }
}