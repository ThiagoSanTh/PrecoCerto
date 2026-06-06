using Microsoft.AspNetCore.Mvc;
using Pc.Dominio.Entities.Interacoes;
using Pc.Servico.Interfaces;
using Pc.WebApi.DTOs.Interacoes;

namespace Pc.WebApi.Controllers
{
    /// <summary>
    /// Controller para operações com Favoritos
    /// Endpoints para criar, listar, remover favoritos
    /// Rota base: /api/favoritos
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class FavoritosController : ControllerBase
    {
        private readonly IFavoritoServico _favoritoServico;

        /// <summary>
        /// Injeta o serviço de favorito
        /// </summary>
        public FavoritosController(IFavoritoServico favoritoServico)
        {
            _favoritoServico = favoritoServico;
        }

        /// <summary>
        /// GET: /api/favoritos/cliente/{clienteId}
        /// Retorna todos os favoritos de um cliente
        /// </summary>
        [HttpGet("cliente/{clienteId:guid}")]
        public async Task<IActionResult> ListarPorCliente(Guid clienteId)
        {
            var favoritos = await _favoritoServico.ListarPorClienteAsync(clienteId);

            var resposta = favoritos.Select(f => new FavoritoRespostaDto
            {
                Id = f.Id,
                ClienteId = f.ClienteId,
                ProdutoId = f.ProdutoId,
                NomeProduto = f.Produto?.NomeProduto ?? string.Empty,
                LojaId = f.LojaId,
                NomeLoja = f.Loja?.NomeFantasia ?? string.Empty,
                DataCriacao = f.DataCriacao
            });

            return Ok(resposta);
        }

        /// <summary>
        /// GET: /api/favoritos/{id}
        /// Retorna um favorito específico
        /// </summary>
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> ObterPorId(Guid id)
        {
            var favorito = await _favoritoServico.ObterPorIdAsync(id);

            if (favorito == null)
                return NotFound("Favorito não encontrado.");

            var resposta = new FavoritoRespostaDto
            {
                Id = favorito.Id,
                ClienteId = favorito.ClienteId,
                ProdutoId = favorito.ProdutoId,
                NomeProduto = favorito.Produto?.NomeProduto ?? string.Empty,
                LojaId = favorito.LojaId,
                NomeLoja = favorito.Loja?.NomeFantasia ?? string.Empty,
                DataCriacao = favorito.DataCriacao
            };

            return Ok(resposta);
        }

        /// <summary>
        /// POST: /api/favoritos
        /// Adiciona um novo favorito
        /// Body: FavoritoCriarDto
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Adicionar([FromBody] FavoritoCriarDto dto)
        {
            var favorito = new Favorito
            {
                ClienteId = dto.ClienteId,
                ProdutoId = dto.ProdutoId,
                LojaId = dto.LojaId
            };

            var novoFavorito = await _favoritoServico.AdicionarAsync(favorito);

            var resposta = new FavoritoRespostaDto
            {
                Id = novoFavorito.Id,
                ClienteId = novoFavorito.ClienteId,
                ProdutoId = novoFavorito.ProdutoId,
                LojaId = novoFavorito.LojaId,
                DataCriacao = novoFavorito.DataCriacao
            };

            return CreatedAtAction(nameof(ObterPorId), new { id = resposta.Id }, resposta);
        }

        /// <summary>
        /// DELETE: /api/favoritos/{id}
        /// Remove um favorito
        /// </summary>
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Remover(Guid id)
        {
            await _favoritoServico.RemoverAsync(id);
            return NoContent();
        }

        /// <summary>
        /// DELETE: /api/favoritos/cliente/{clienteId}/produto/{produtoId}
        /// Remove um favorito de produto específico
        /// </summary>
        [HttpDelete("cliente/{clienteId:guid}/produto/{produtoId:guid}")]
        public async Task<IActionResult> RemoverFavoritoProduto(Guid clienteId, Guid produtoId)
        {
            await _favoritoServico.RemoverFavoritoProdutoAsync(clienteId, produtoId);
            return NoContent();
        }

        /// <summary>
        /// DELETE: /api/favoritos/cliente/{clienteId}/loja/{lojaId}
        /// Remove um favorito de loja específica
        /// </summary>
        [HttpDelete("cliente/{clienteId:guid}/loja/{lojaId:guid}")]
        public async Task<IActionResult> RemoverFavoritoLoja(Guid clienteId, Guid lojaId)
        {
            await _favoritoServico.RemoverFavoritoLojaAsync(clienteId, lojaId);
            return NoContent();
        }

        /// <summary>
        /// GET: /api/favoritos/verificar/{clienteId}?produtoId={id}&lojaId={id}
        /// Verifica se um item é favorito
        /// </summary>
        [HttpGet("verificar/{clienteId:guid}")]
        public async Task<IActionResult> VerificaFavorito(Guid clienteId, Guid? produtoId = null, Guid? lojaId = null)
        {
            var ehFavorito = await _favoritoServico.EhFavoritoAsync(clienteId, produtoId, lojaId);
            return Ok(new { ehFavorito });
        }
    }
}
