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
    [Authorize]
    public class FavoritosController : ControllerBase
    {
        private readonly IFavoritoServico _favoritoServico;

        public FavoritosController(IFavoritoServico favoritoServico)
        {
            _favoritoServico = favoritoServico;
        }

        [HttpGet("cliente/{clienteId:guid}")]
        [Authorize(Roles = "Cliente,Admin")]
        public async Task<IActionResult> ListarPorCliente(Guid clienteId)
        {
            if (!Authz.IsSelfOrAdmin(this, clienteId))
                return Forbid();

            var favoritos = await _favoritoServico.ListarPorClienteAsync(clienteId);
            return Ok(favoritos.Select(MapResposta));
        }

        [HttpGet("{id:guid}")]
        [Authorize(Roles = "Cliente,Admin")]
        public async Task<IActionResult> ObterPorId(Guid id)
        {
            var favorito = await _favoritoServico.ObterPorIdAsync(id);
            if (favorito == null)
                return NotFound("Favorito não encontrado.");

            if (!Authz.IsSelfOrAdmin(this, favorito.ClienteId))
                return Forbid();

            return Ok(MapResposta(favorito));
        }

        [HttpPost]
        [Authorize(Roles = "Cliente")]
        public async Task<IActionResult> Adicionar([FromBody] FavoritoCriarDto dto)
        {
            if (User.GetUserId() != dto.ClienteId)
                return Forbid();

            var favorito = new Favorito
            {
                ClienteId = dto.ClienteId,
                ProdutoId = dto.ProdutoId,
                LojaId = dto.LojaId
            };

            var novoFavorito = await _favoritoServico.AdicionarAsync(favorito);
            return CreatedAtAction(nameof(ObterPorId), new { id = novoFavorito.Id }, MapResposta(novoFavorito));
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Cliente,Admin")]
        public async Task<IActionResult> Remover(Guid id)
        {
            var favorito = await _favoritoServico.ObterPorIdAsync(id);
            if (favorito == null)
                return NotFound("Favorito não encontrado.");

            if (!Authz.IsSelfOrAdmin(this, favorito.ClienteId))
                return Forbid();

            await _favoritoServico.RemoverAsync(id);
            return NoContent();
        }

        [HttpDelete("cliente/{clienteId:guid}/produto/{produtoId:guid}")]
        [Authorize(Roles = "Cliente")]
        public async Task<IActionResult> RemoverFavoritoProduto(Guid clienteId, Guid produtoId)
        {
            if (User.GetUserId() != clienteId)
                return Forbid();

            await _favoritoServico.RemoverFavoritoProdutoAsync(clienteId, produtoId);
            return NoContent();
        }

        [HttpDelete("cliente/{clienteId:guid}/loja/{lojaId:guid}")]
        [Authorize(Roles = "Cliente")]
        public async Task<IActionResult> RemoverFavoritoLoja(Guid clienteId, Guid lojaId)
        {
            if (User.GetUserId() != clienteId)
                return Forbid();

            await _favoritoServico.RemoverFavoritoLojaAsync(clienteId, lojaId);
            return NoContent();
        }

        [HttpGet("verificar/{clienteId:guid}")]
        [Authorize(Roles = "Cliente,Admin")]
        public async Task<IActionResult> VerificaFavorito(Guid clienteId, Guid? produtoId = null, Guid? lojaId = null)
        {
            if (!Authz.IsSelfOrAdmin(this, clienteId))
                return Forbid();

            var ehFavorito = await _favoritoServico.EhFavoritoAsync(clienteId, produtoId, lojaId);
            return Ok(new { ehFavorito });
        }

        private static FavoritoRespostaDto MapResposta(Favorito f) => new()
        {
            Id = f.Id,
            ClienteId = f.ClienteId,
            ProdutoId = f.ProdutoId,
            NomeProduto = f.Produto?.NomeProduto ?? string.Empty,
            LojaId = f.LojaId,
            NomeLoja = f.Loja?.NomeFantasia ?? string.Empty,
            DataCriacao = f.DataCriacao
        };
    }
}
