using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pc.Dominio.Entities.Interacoes;
using Pc.Servico.Interfaces;
using Pc.WebApi.Authorization;
using Pc.WebApi.DTOs.Interacoes;
using Pc.WebApi.Extensions;

namespace Pc.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Cliente,Admin")]
    public class CarrinhoController : ControllerBase
    {
        private readonly ICarrinhoServico _carrinhoServico;

        public CarrinhoController(ICarrinhoServico carrinhoServico)
        {
            _carrinhoServico = carrinhoServico;
        }

        /// <summary>GET /api/carrinho/{clienteId} — retorna (ou cria) o carrinho do cliente.</summary>
        [HttpGet("{clienteId:guid}")]
        [ProducesResponseType(typeof(CarrinhoRespostaDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> Obter(Guid clienteId)
        {
            if (!Authz.IsSelfOrAdmin(this, clienteId))
                return Forbid();

            var carrinho = await _carrinhoServico.ObterOuCriarAsync(clienteId);
            return Ok(MapResposta(carrinho));
        }

        /// <summary>POST /api/carrinho/{clienteId}/itens — adiciona item ao carrinho.</summary>
        [HttpPost("{clienteId:guid}/itens")]
        [ProducesResponseType(typeof(CarrinhoRespostaDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> AdicionarItem(Guid clienteId, [FromBody] AdicionarItemCarrinhoDto dto)
        {
            if (!Authz.IsSelfOrAdmin(this, clienteId))
                return Forbid();

            var carrinho = await _carrinhoServico.AdicionarItemAsync(
                clienteId, dto.ProdutoId, dto.Quantidade, dto.PrecoUnitario, dto.OfertaId);
            return Ok(MapResposta(carrinho));
        }

        /// <summary>PUT /api/carrinho/{clienteId}/itens/{itemId} — atualiza a quantidade.</summary>
        [HttpPut("{clienteId:guid}/itens/{itemId:guid}")]
        [ProducesResponseType(typeof(CarrinhoRespostaDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> AtualizarItem(Guid clienteId, Guid itemId, [FromBody] AtualizarItemCarrinhoDto dto)
        {
            if (!Authz.IsSelfOrAdmin(this, clienteId))
                return Forbid();

            var carrinho = await _carrinhoServico.AtualizarQuantidadeAsync(clienteId, itemId, dto.Quantidade);
            return Ok(MapResposta(carrinho));
        }

        /// <summary>DELETE /api/carrinho/{clienteId}/itens/{itemId} — remove um item.</summary>
        [HttpDelete("{clienteId:guid}/itens/{itemId:guid}")]
        [ProducesResponseType(typeof(CarrinhoRespostaDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> RemoverItem(Guid clienteId, Guid itemId)
        {
            if (!Authz.IsSelfOrAdmin(this, clienteId))
                return Forbid();

            var carrinho = await _carrinhoServico.RemoverItemAsync(clienteId, itemId);
            return Ok(MapResposta(carrinho));
        }

        /// <summary>DELETE /api/carrinho/{clienteId} — esvazia o carrinho.</summary>
        [HttpDelete("{clienteId:guid}")]
        public async Task<IActionResult> Limpar(Guid clienteId)
        {
            if (!Authz.IsSelfOrAdmin(this, clienteId))
                return Forbid();

            await _carrinhoServico.LimparAsync(clienteId);
            return NoContent();
        }

        private static CarrinhoRespostaDto MapResposta(Carrinho c)
        {
            var itens = c.Itens
                .OrderBy(i => i.DataCriacao)
                .Select(i => new ItemCarrinhoRespostaDto
                {
                    Id = i.Id,
                    ProdutoId = i.ProdutoId,
                    NomeProduto = i.Produto?.NomeProduto ?? string.Empty,
                    ImagemUrl = i.Produto?.ImagemUrl,
                    OfertaId = i.OfertaId,
                    Quantidade = i.Quantidade,
                    PrecoUnitario = i.PrecoUnitario,
                    Subtotal = i.PrecoUnitario * i.Quantidade
                })
                .ToList();

            return new CarrinhoRespostaDto
            {
                Id = c.Id,
                ClienteId = c.ClienteId,
                Itens = itens,
                Total = itens.Sum(i => i.Subtotal),
                QuantidadeItens = itens.Sum(i => i.Quantidade)
            };
        }
    }
}
