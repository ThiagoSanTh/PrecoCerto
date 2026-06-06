using Microsoft.AspNetCore.Mvc;
using Pc.Dominio.Entities.Interacoes;
using Pc.Servico.Interfaces;
using Pc.WebApi.DTOs.Interacoes;

namespace Pc.WebApi.Controllers
{
    /// <summary>
    /// Controller para operações com Avaliações de Lojas
    /// Endpoints para criar, listar, atualizar e remover avaliações
    /// Rota base: /api/avaliacoes
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AvaliacoesController : ControllerBase
    {
        private readonly IAvaliacaoServico _avaliacaoServico;

        /// <summary>
        /// Injeta o serviço de avaliação
        /// </summary>
        public AvaliacoesController(IAvaliacaoServico avaliacaoServico)
        {
            _avaliacaoServico = avaliacaoServico;
        }

        /// <summary>
        /// POST: /api/avaliacoes
        /// Adiciona uma nova avaliação
        /// Body: AvaliacaoCriarDto
        /// Valida: nota entre 1-5, evita duplicatas
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Adicionar([FromBody] AvaliacaoCriarDto dto)
        {
            var avaliacao = new Avaliacao
            {
                ClienteId = dto.ClienteId,
                LojaId = dto.LojaId,
                Nota = dto.Nota,
                Comentario = dto.Comentario
            };

            var novaAvaliacao = await _avaliacaoServico.AdicionarAsync(avaliacao);

            var resposta = new AvaliacaoRespostaDto
            {
                Id = novaAvaliacao.Id,
                ClienteId = novaAvaliacao.ClienteId,
                LojaId = novaAvaliacao.LojaId,
                NomeLoja = novaAvaliacao.Loja?.NomeFantasia ?? string.Empty,
                Nota = novaAvaliacao.Nota,
                Comentario = novaAvaliacao.Comentario,
                DataAvaliacao = novaAvaliacao.DataAvaliacao
            };

            return CreatedAtAction(nameof(ObterPorId), new { id = resposta.Id }, resposta);
        }

        /// <summary>
        /// GET: /api/avaliacoes/{id}
        /// Retorna uma avaliação específica
        /// </summary>
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> ObterPorId(Guid id)
        {
            var avaliacao = await _avaliacaoServico.ObterPorIdAsync(id);

            if (avaliacao == null)
                return NotFound("Avaliação não encontrada.");

            var resposta = new AvaliacaoRespostaDto
            {
                Id = avaliacao.Id,
                ClienteId = avaliacao.ClienteId,
                LojaId = avaliacao.LojaId,
                NomeLoja = avaliacao.Loja?.NomeFantasia ?? string.Empty,
                Nota = avaliacao.Nota,
                Comentario = avaliacao.Comentario,
                DataAvaliacao = avaliacao.DataAvaliacao
            };

            return Ok(resposta);
        }

        /// <summary>
        /// GET: /api/avaliacoes/loja/{lojaId}
        /// Retorna todas as avaliações de uma loja
        /// </summary>
        [HttpGet("loja/{lojaId:guid}")]
        public async Task<IActionResult> ListarPorLoja(Guid lojaId)
        {
            var avaliacoes = await _avaliacaoServico.ListarPorLojaAsync(lojaId);

            var resposta = avaliacoes.Select(a => new AvaliacaoRespostaDto
            {
                Id = a.Id,
                ClienteId = a.ClienteId,
                LojaId = a.LojaId,
                NomeLoja = a.Loja?.NomeFantasia ?? string.Empty,
                Nota = a.Nota,
                Comentario = a.Comentario,
                DataAvaliacao = a.DataAvaliacao
            });

            return Ok(resposta);
        }

        /// <summary>
        /// GET: /api/avaliacoes/cliente/{clienteId}
        /// Retorna todas as avaliações feitas por um cliente
        /// </summary>
        [HttpGet("cliente/{clienteId:guid}")]
        public async Task<IActionResult> ListarPorCliente(Guid clienteId)
        {
            var avaliacoes = await _avaliacaoServico.ListarPorClienteAsync(clienteId);

            var resposta = avaliacoes.Select(a => new AvaliacaoRespostaDto
            {
                Id = a.Id,
                ClienteId = a.ClienteId,
                LojaId = a.LojaId,
                NomeLoja = a.Loja?.NomeFantasia ?? string.Empty,
                Nota = a.Nota,
                Comentario = a.Comentario,
                DataAvaliacao = a.DataAvaliacao
            });

            return Ok(resposta);
        }

        /// <summary>
        /// GET: /api/avaliacoes/loja/{lojaId}/media
        /// Retorna a nota média de uma loja
        /// </summary>
        [HttpGet("loja/{lojaId:guid}/media")]
        public async Task<IActionResult> ObterMediaAvaliacao(Guid lojaId)
        {
            var media = await _avaliacaoServico.ObterMediaAvaliacaoAsync(lojaId);
            var quantidade = await _avaliacaoServico.ObterQuantidadeAvaliacoesAsync(lojaId);

            return Ok(new { media, quantidade });
        }

        /// <summary>
        /// PUT: /api/avaliacoes/{id}
        /// Atualiza uma avaliação existente
        /// Body: AvaliacaoCriarDto
        /// </summary>
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Atualizar(Guid id, [FromBody] AvaliacaoCriarDto dto)
        {
            var avaliacao = await _avaliacaoServico.ObterPorIdAsync(id);

            if (avaliacao == null)
                return NotFound("Avaliação não encontrada.");

            avaliacao.Nota = dto.Nota;
            avaliacao.Comentario = dto.Comentario;

            await _avaliacaoServico.AtualizarAsync(avaliacao);

            var resposta = new AvaliacaoRespostaDto
            {
                Id = avaliacao.Id,
                ClienteId = avaliacao.ClienteId,
                LojaId = avaliacao.LojaId,
                NomeLoja = avaliacao.Loja?.NomeFantasia ?? string.Empty,
                Nota = avaliacao.Nota,
                Comentario = avaliacao.Comentario,
                DataAvaliacao = avaliacao.DataAvaliacao
            };

            return Ok(resposta);
        }

        /// <summary>
        /// DELETE: /api/avaliacoes/{id}
        /// Remove uma avaliação
        /// </summary>
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Remover(Guid id)
        {
            await _avaliacaoServico.RemoverAsync(id);
            return NoContent();
        }
    }
}
