using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pc.Dominio.Entities.Interacoes;
using Pc.Repositorio.Interfaces;
using Pc.Servico.Interfaces;
using Pc.WebApi.Authorization;
using Pc.WebApi.DTOs.Interacoes;
using Pc.WebApi.Extensions;
using Pc.WebApi.Helpers;

namespace Pc.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FavoritosController : ControllerBase
    {
        private readonly IFavoritoServico _favoritoServico;
        private readonly IOfertaRepositorio _ofertaRepositorio;

        public FavoritosController(IFavoritoServico favoritoServico, IOfertaRepositorio ofertaRepositorio)
        {
            _favoritoServico = favoritoServico;
            _ofertaRepositorio = ofertaRepositorio;
        }

        [HttpGet("cliente/{clienteId:guid}")]
        [Authorize(Roles = "Cliente,Admin")]
        public async Task<IActionResult> ListarPorCliente(
            Guid clienteId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            if (!Authz.IsSelfOrAdmin(this, clienteId))
                return Forbid();

            var paginacao = PaginacaoHelper.Normalizar(page, pageSize);
            var favoritos = await _favoritoServico.ListarPorClientePaginadoAsync(clienteId, paginacao);
            var produtoIds = favoritos.Items
                .Where(f => f.ProdutoId.HasValue)
                .Select(f => f.ProdutoId!.Value);
            var ofertas = await _ofertaRepositorio.ObterMelhorOfertaPorProdutosAsync(produtoIds);

            return Ok(PaginacaoHelper.ParaResposta(favoritos, f => MapResposta(f, ofertas)));
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

            var ofertas = favorito.ProdutoId.HasValue
                ? await _ofertaRepositorio.ObterMelhorOfertaPorProdutosAsync(new[] { favorito.ProdutoId.Value })
                : new Dictionary<Guid, Pc.Dominio.Entities.Estabelecimentos.Oferta>();

            return Ok(MapResposta(favorito, ofertas));
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
            return CreatedAtAction(nameof(ObterPorId), new { id = novoFavorito.Id }, MapResposta(novoFavorito, new Dictionary<Guid, Pc.Dominio.Entities.Estabelecimentos.Oferta>()));
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

        private static FavoritoRespostaDto MapResposta(
            Favorito f,
            Dictionary<Guid, Pc.Dominio.Entities.Estabelecimentos.Oferta> ofertas)
        {
            ofertas.TryGetValue(f.ProdutoId ?? Guid.Empty, out var oferta);
            var precoBase = f.Produto?.Preco;
            decimal? precoExibicao = precoBase;
            if (oferta != null && precoBase.HasValue)
                precoExibicao = Math.Min(oferta.Preco, precoBase.Value);
            else if (oferta != null)
                precoExibicao = oferta.Preco;

            return new FavoritoRespostaDto
            {
                Id = f.Id,
                ClienteId = f.ClienteId,
                ProdutoId = f.ProdutoId,
                NomeProduto = f.Produto?.NomeProduto ?? string.Empty,
                LojaId = f.LojaId,
                NomeLoja = f.Loja?.NomeFantasia ?? string.Empty,
                DataCriacao = f.DataCriacao,
                ImagemUrl = f.Produto?.ImagemUrl,
                PrecoBase = precoBase,
                PrecoExibicao = precoExibicao,
                PrecoAnterior = oferta?.PrecoAnterior,
                EmPromocao = oferta?.EmPromocao ?? false
            };
        }
    }
}
