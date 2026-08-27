using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Pc.Dominio.Entities.Catalogo;
using Pc.Servico.Excecoes;
using Pc.Servico.Interfaces;
using Pc.WebApi.Authorization;
using Pc.WebApi.DTOs.Catalogo;
using Pc.WebApi.Helpers;
using Pc.WebApi.Mappings;

namespace Pc.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProdutosController : ControllerBase
    {
        private readonly IProdutoServico _produtoServico;
        private readonly IIdCodificador _idCodificador;

        public ProdutosController(IProdutoServico produtoServico, IIdCodificador idCodificador)
        {
            _produtoServico = produtoServico;
            _idCodificador = idCodificador;
        }

        [HttpGet]
        [AllowAnonymous]
        [EnableRateLimiting("catalogo")]
        public async Task<IActionResult> Listar(
            [FromQuery] Guid? lojaId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var paginacao = PaginacaoHelper.Normalizar(page, pageSize);
            var produtos = await _produtoServico.ListarProdutosPaginadoAsync(paginacao, lojaId);
            return Ok(PaginacaoHelper.ParaResposta(produtos, ProdutoMapper.ParaResumoDto));
        }

        [HttpGet("{id:guid}")]
        [AllowAnonymous]
        [EnableRateLimiting("catalogo")]
        public async Task<IActionResult> ObterPorId(Guid id)
        {
            var produto = await _produtoServico.ObterPorIdAsync(id);
            if (produto is null)
                return NotFound(new { code = "PRODUCT_NOT_FOUND", message = "Produto n�o encontrado." });

            return Ok(ProdutoMapper.ParaRespostaDto(produto, _idCodificador));
        }

        [HttpPost("Buscar")]
        [AllowAnonymous]
        [EnableRateLimiting("catalogo")]
        public async Task<IActionResult> BuscarPorNome([FromBody] ProdutoBuscarDto dto)
        {
            var paginacao = PaginacaoHelper.Normalizar(dto.Page, dto.PageSize);
            var produtos = await _produtoServico.BuscarPorNomePaginadoAsync(dto.Nome, paginacao, dto.LojaId);
            return Ok(PaginacaoHelper.ParaResposta(produtos, ProdutoMapper.ParaResumoDto));
        }

        [HttpPost]
        [Authorize(Roles = "Lojista,Vendedor,Admin")]
        public async Task<IActionResult> Adicionar([FromBody] ProdutoCriarDto dto)
        {
            if (!dto.LojaId.HasValue)
                return BadRequest("LojaId � obrigat�rio.");

            if (!Authz.OwnsLoja(this, dto.LojaId.Value))
                return Forbid();

            var produto = new Produto
            {
                NomeProduto = dto.NomeProduto,
                Descricao = dto.Descricao,
                Marca = dto.Marca,
                CodigoBarras = dto.CodigoBarras,
                Preco = dto.Preco,
                LojaId = dto.LojaId.Value,
                ImagemUrl = dto.ImagemUrl,
                Categoria = dto.Categoria
            };

            var novoProduto = await _produtoServico.AdicionarAsync(produto);
            var recarregado = await _produtoServico.ObterPorIdAsync(novoProduto.Id);
            return CreatedAtAction(
                nameof(ObterPorId),
                new { id = novoProduto.Id },
                ProdutoMapper.ParaRespostaDto(recarregado ?? novoProduto, _idCodificador));
        }

        [HttpPut("{id:guid}")]
        [Authorize(Roles = "Lojista,Vendedor,Admin")]
        public async Task<IActionResult> Atualizar(Guid id, [FromBody] ProdutoAtualizarDto dto)
        {
            if (!Authz.OwnsLoja(this, dto.LojaId))
                return Forbid();

            try
            {
                var dados = new Produto
                {
                    NomeProduto = dto.NomeProduto,
                    Descricao = dto.Descricao,
                    Marca = dto.Marca,
                    CodigoBarras = dto.CodigoBarras,
                    Preco = dto.Preco,
                    ImagemUrl = dto.ImagemUrl,
                    Categoria = dto.Categoria ?? Pc.Dominio.Enums.CategoriaProduto.Outros
                };

                await _produtoServico.AtualizarPorLojaAsync(id, dados, dto.LojaId);
                return NoContent();
            }
            catch (ProdutoOperacaoException ex) when (ex.AcessoNegado)
            {
                return StatusCode(StatusCodes.Status403Forbidden, ex.Message);
            }
            catch (ProdutoOperacaoException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Lojista,Vendedor,Admin")]
        public async Task<IActionResult> Deletar(Guid id, [FromQuery] Guid lojaId)
        {
            if (!Authz.OwnsLoja(this, lojaId))
                return Forbid();

            try
            {
                await _produtoServico.RemoverPorLojaAsync(id, lojaId);
                return NoContent();
            }
            catch (ProdutoOperacaoException ex) when (ex.AcessoNegado)
            {
                return StatusCode(StatusCodes.Status403Forbidden, ex.Message);
            }
            catch (ProdutoOperacaoException ex)
            {
                return NotFound(ex.Message);
            }
        }
    }
}
