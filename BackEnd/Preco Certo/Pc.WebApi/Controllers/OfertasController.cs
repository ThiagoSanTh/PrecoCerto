using Microsoft.AspNetCore.Mvc;
using Pc.Dominio.Entities.Catalogo;
using Pc.Dominio.Entities.Estabelecimentos;
using Pc.Servico.Interfaces;
using Pc.WebApi.DTOs.Estabelecimentos;

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
        public async Task<IActionResult> Listar()
        {
            var ofertas = await _ofertaServico.ListarAsync();

            var resposta = ofertas.Select(oferta => new OfertaRespostaDto
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
            });

            return Ok(resposta);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> ObterPorId(Guid id)
        {
            var oferta = await _ofertaServico.ObterPorIdAsync(id);

            if (oferta == null)
                return NotFound("Oferta não encontrada.");

            var resposta = new OfertaRespostaDto
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

            return Ok(resposta);
        }

        [HttpGet("produto/{produtoId:guid}")]
        public async Task<IActionResult> ObterPorProduto(Guid produtoId)
        {
            var ofertas = await _ofertaServico.ObterPorProdutoAsync(produtoId);

            var resposta = ofertas.Select(oferta => new OfertaRespostaDto
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
            });

            return Ok(resposta);
        }

        [HttpPost]
        public async Task<IActionResult> Adicionar([FromBody] OfertaCriarDto dto)
        {
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
        public async Task<IActionResult> Atualizar(Guid id, [FromBody] OfertaCriarDto dto)
        {
            var ofertaExistente = await _ofertaServico.ObterPorIdAsync(id);

            if (ofertaExistente == null)
                return NotFound("Oferta não encontrada.");

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
        public async Task<IActionResult> Remover(Guid id)
        {
            var ofertaExistente = await _ofertaServico.ObterPorIdAsync(id);

            if (ofertaExistente == null)
                return NotFound("Oferta não encontrada.");

            await _ofertaServico.RemoverAsync(id);
            return NoContent();
        }
    }
}