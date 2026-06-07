using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pc.Dominio.Entities.Interacoes;
using Pc.Servico.Interfaces;
using Pc.WebApi.Authorization;
using Pc.WebApi.DTOs.Comum;
using Pc.WebApi.DTOs.Interacoes;
using Pc.WebApi.Extensions;

namespace Pc.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class HistoricoPesquisaController : ControllerBase
    {
        private readonly IHistoricoPesquisaServico _historicoPesquisaServico;

        /// <summary>
        /// Injeta o serviço de histórico
        /// </summary>
        public HistoricoPesquisaController(IHistoricoPesquisaServico historicoPesquisaServico)
        {
            _historicoPesquisaServico = historicoPesquisaServico;
        }

        /// <summary>
        /// POST: /api/historicopesquisa
        /// Registra uma nova pesquisa no histórico
        /// Body: HistoricoPesquisaCriarDto
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Cliente")]
        public async Task<IActionResult> RegistrarPesquisa([FromBody] HistoricoPesquisaCriarDto dto)
        {
            if (User.GetUserId() != dto.ClienteId)
                return Forbid();
            var historico = new HistoricoPesquisa
            {
                ClienteId = dto.ClienteId,
                TermoPesquisa = dto.TermoPesquisa
            };

            var novoHistorico = await _historicoPesquisaServico.RegistrarPesquisaAsync(historico);

            var resposta = new HistoricoPesquisaRespostaDto
            {
                Id = novoHistorico.Id,
                ClienteId = novoHistorico.ClienteId,
                TermoPesquisa = novoHistorico.TermoPesquisa,
                DataPesquisa = novoHistorico.DataPesquisa
            };

            return CreatedAtAction(nameof(ObterPorId), new { id = resposta.Id }, resposta);
        }

        /// <summary>
        /// GET: /api/historicopesquisa/{id}
        /// Retorna um registro específico do histórico
        /// </summary>
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> ObterPorId(Guid id)
        {
            var historico = await _historicoPesquisaServico.ObterPorIdAsync(id);

            if (historico == null)
                return NotFound("Histórico não encontrado.");

            var resposta = new HistoricoPesquisaRespostaDto
            {
                Id = historico.Id,
                ClienteId = historico.ClienteId,
                TermoPesquisa = historico.TermoPesquisa,
                DataPesquisa = historico.DataPesquisa
            };

            return Ok(resposta);
        }

        /// <summary>
        /// GET: /api/historicopesquisa/cliente/{clienteId}
        /// Retorna todo o histórico de pesquisa de um cliente
        /// Ordenado por data descrescente (mais recentes primeiro)
        /// </summary>
        [HttpGet("cliente/{clienteId:guid}")]
        [Authorize(Roles = "Cliente,Admin")]
        public async Task<IActionResult> ObterHistoricoCliente(Guid clienteId)
        {
            if (!Authz.IsSelfOrAdmin(this, clienteId))
                return Forbid();
            var historicos = await _historicoPesquisaServico.ObterHistoricoClienteAsync(clienteId);

            var resposta = historicos.Select(h => new HistoricoPesquisaRespostaDto
            {
                Id = h.Id,
                ClienteId = h.ClienteId,
                TermoPesquisa = h.TermoPesquisa,
                DataPesquisa = h.DataPesquisa
            });

            return Ok(resposta);
        }

        /// <summary>
        /// GET: /api/historicopesquisa/cliente/{clienteId}/ultimos?quantidade=5
        /// Retorna os últimos N termos de pesquisa (padrão: 5)
        /// </summary>
        [HttpGet("cliente/{clienteId:guid}/ultimos")]
        [Authorize(Roles = "Cliente,Admin")]
        public async Task<IActionResult> ObterUltimos(Guid clienteId, [FromQuery] int quantidade = 5)
        {
            if (!Authz.IsSelfOrAdmin(this, clienteId))
                return Forbid();
            var historicos = await _historicoPesquisaServico.ObterUltimosTermosAsync(clienteId, quantidade);

            var resposta = historicos.Select(h => new HistoricoPesquisaRespostaDto
            {
                Id = h.Id,
                ClienteId = h.ClienteId,
                TermoPesquisa = h.TermoPesquisa,
                DataPesquisa = h.DataPesquisa
            });

            return Ok(resposta);
        }

        /// <summary>
        /// GET: /api/historicopesquisa/sugestoes?termo=pao
        /// Retorna sugestões de termos para autocomplete
        /// </summary>
        [HttpGet("sugestoes")]
        [AllowAnonymous]
        public async Task<IActionResult> ObterSugestoes([FromQuery] string termo)
        {
            var sugestoes = await _historicoPesquisaServico.ObterSugestoesAsync(termo);

            var resposta = sugestoes
                .GroupBy(h => h.TermoPesquisa)
                .Select(g => g.First())
                .Select(h => new HistoricoPesquisaRespostaDto
                {
                    Id = h.Id,
                    ClienteId = h.ClienteId,
                    TermoPesquisa = h.TermoPesquisa,
                    DataPesquisa = h.DataPesquisa
                });

            return Ok(resposta);
        }

        /// <summary>
        /// DELETE: /api/historicopesquisa/{id}
        /// Remove um registro específico
        /// </summary>
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Remover(Guid id)
        {
            await _historicoPesquisaServico.RemoverAsync(id);
            return NoContent();
        }

        /// <summary>
        /// DELETE: /api/historicopesquisa/cliente/{clienteId}/limpar
        /// Limpa todo o histórico de um cliente
        /// CUIDADO: Operação irreversível
        /// </summary>
        [HttpDelete("cliente/{clienteId:guid}/limpar")]
        [Authorize(Roles = "Cliente,Admin")]
        public async Task<IActionResult> LimparHistorico(Guid clienteId)
        {
            if (!Authz.IsSelfOrAdmin(this, clienteId))
                return Forbid();
            await _historicoPesquisaServico.LimparHistoricoAsync(clienteId);
            return NoContent();
        }
    }
}
