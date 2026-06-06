using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pc.Dominio.Entities.Catalogo;
using Pc.Servico.Excecoes;
using Pc.Servico.Interfaces;
using Pc.WebApi.Authorization;
using Pc.WebApi.DTOs.Catalogo;
using Pc.WebApi.Mappings;

namespace Pc.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProdutosController : ControllerBase
    {
        private readonly IProdutoServico _produtoServico;

        public ProdutosController(IProdutoServico produtoServico)
        {
            _produtoServico = produtoServico;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Listar([FromQuery] Guid? lojaId)
        {
            var produtos = await _produtoServico.ListarProdutosAsync(lojaId);
            return Ok(produtos.Select(ProdutoMapper.ParaRespostaDto));
        }

        [HttpGet("{id:guid}")]
        [AllowAnonymous]
        public async Task<IActionResult> ObterPorId(Guid id)
        {
            var produto = await _produtoServico.ObterPorIdAsync(id);
            if (produto is null)
                return NotFound("Produto não encontrado.");

            return Ok(ProdutoMapper.ParaRespostaDto(produto));
        }

        [HttpPost("Buscar")]
        [AllowAnonymous]
        public async Task<IActionResult> BuscarPorNome([FromBody] ProdutoBuscarDto dto)
        {
            var produtos = await _produtoServico.BuscarPorNomeAsync(dto.Nome, dto.LojaId);
            return Ok(produtos.Select(ProdutoMapper.ParaRespostaDto));
        }

        [HttpPost]
        [Authorize(Roles = "Lojista,Admin")]
        public async Task<IActionResult> Adicionar([FromBody] ProdutoCriarDto dto)
        {
            if (!dto.LojaId.HasValue)
                return BadRequest("LojaId é obrigatório.");

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
                ImagemUrl = dto.ImagemUrl
            };

            var novoProduto = await _produtoServico.AdicionarAsync(produto);
            var recarregado = await _produtoServico.ObterPorIdAsync(novoProduto.Id);
            return CreatedAtAction(
                nameof(ObterPorId),
                new { id = novoProduto.Id },
                ProdutoMapper.ParaRespostaDto(recarregado ?? novoProduto));
        }

        [HttpPut("{id:guid}")]
        [Authorize(Roles = "Lojista,Admin")]
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
                    ImagemUrl = dto.ImagemUrl
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
        [Authorize(Roles = "Lojista,Admin")]
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
