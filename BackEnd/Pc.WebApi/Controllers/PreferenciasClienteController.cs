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
    public class PreferenciasClienteController : ControllerBase
    {
        private readonly IPreferenciaClienteServico _preferenciaServico;

        /// <summary>
        /// Injeta o serviço de preferência
        /// </summary>
        public PreferenciasClienteController(IPreferenciaClienteServico preferenciaServico)
        {
            _preferenciaServico = preferenciaServico;
        }

        /// <summary>
        /// POST: /api/preferenciascliente
        /// Salva uma nova preferência ou atualiza se existir
        /// Body: PreferenciaClienteCriarDto
        /// Padrão chave-valor: permite flexibilidade na adição de novas configurações
        /// </summary>
        [HttpPost]
        [Authorize(Policy = AuthPolicies.CapacidadeCliente)]
        public async Task<IActionResult> Salvar([FromBody] PreferenciaClienteCriarDto dto)
        {
            if (User.GetUserId() != dto.ClienteId)
                return Forbid();
            var preferencia = new PreferenciaCliente
            {
                ClienteId = dto.ClienteId,
                Chave = dto.Chave,
                Valor = dto.Valor
            };

            var novaPreferencia = await _preferenciaServico.SalvarAsync(preferencia);

            var resposta = new PreferenciaClienteRespostaDto
            {
                Id = novaPreferencia.Id,
                ClienteId = novaPreferencia.ClienteId,
                Chave = novaPreferencia.Chave,
                Valor = novaPreferencia.Valor,
                DataCriacao = novaPreferencia.DataCriacao
            };

            return CreatedAtAction(nameof(Obter), new { id = resposta.Id }, resposta);
        }

        /// <summary>
        /// GET: /api/preferenciascliente/{id}
        /// Retorna uma preferência específica
        /// </summary>
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> Obter(Guid id)
        {
            var preferencia = await _preferenciaServico.ObterAsync(id);

            if (preferencia == null)
                return NotFound("Preferência não encontrada.");

            var resposta = new PreferenciaClienteRespostaDto
            {
                Id = preferencia.Id,
                ClienteId = preferencia.ClienteId,
                Chave = preferencia.Chave,
                Valor = preferencia.Valor,
                DataCriacao = preferencia.DataCriacao
            };

            return Ok(resposta);
        }

        /// <summary>
        /// GET: /api/preferenciascliente/cliente/{clienteId}
        /// Retorna todas as preferências de um cliente
        /// </summary>
        [HttpGet("cliente/{clienteId:guid}")]
        [Authorize(Policy = AuthPolicies.CapacidadeClienteOuAdmin)]
        public async Task<IActionResult> ListarPorCliente(Guid clienteId)
        {
            if (!Authz.IsSelfOrAdmin(this, clienteId))
                return Forbid();
            var preferencias = await _preferenciaServico.ListarPorClienteAsync(clienteId);

            var resposta = preferencias.Select(p => new PreferenciaClienteRespostaDto
            {
                Id = p.Id,
                ClienteId = p.ClienteId,
                Chave = p.Chave,
                Valor = p.Valor,
                DataCriacao = p.DataCriacao
            });

            return Ok(resposta);
        }

        /// <summary>
        /// GET: /api/preferenciascliente/cliente/{clienteId}/valor?chave=raio_busca
        /// Retorna o valor de uma preferência específica
        /// </summary>
        [HttpGet("cliente/{clienteId:guid}/valor")]
        [Authorize(Policy = AuthPolicies.CapacidadeClienteOuAdmin)]
        public async Task<IActionResult> ObterValor(Guid clienteId, [FromQuery] string chave)
        {
            if (!Authz.IsSelfOrAdmin(this, clienteId))
                return Forbid();
            var valor = await _preferenciaServico.ObterValorAsync(clienteId, chave);

            if (valor == null)
                return NotFound($"Preferência '{chave}' não encontrada.");

            return Ok(new { chave, valor });
        }

        /// <summary>
        /// PUT: /api/preferenciascliente/cliente/{clienteId}?chave=notificacoes
        /// Atualiza o valor de uma preferência
        /// Body: { "valor": "desabilitado" }
        /// </summary>
        [HttpPut("cliente/{clienteId:guid}")]
        [Authorize(Policy = AuthPolicies.CapacidadeCliente)]
        public async Task<IActionResult> Atualizar(Guid clienteId, [FromQuery] string chave, [FromBody] PreferenciaValorDto dto)
        {
            if (User.GetUserId() != clienteId)
                return Forbid();
            if (string.IsNullOrEmpty(dto.Valor))
                return BadRequest("Valor é obrigatório.");

            await _preferenciaServico.AtualizarAsync(clienteId, chave, dto.Valor);

            return Ok(new { mensagem = "Preferência atualizada com sucesso" });
        }

        /// <summary>
        /// DELETE: /api/preferenciascliente/{id}
        /// Remove uma preferência
        /// </summary>
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Remover(Guid id)
        {
            await _preferenciaServico.RemoverAsync(id);
            return NoContent();
        }

        /// <summary>
        /// DELETE: /api/preferenciascliente/cliente/{clienteId}?chave=notificacoes
        /// Remove uma preferência específica por chave
        /// </summary>
        [HttpDelete("cliente/{clienteId:guid}")]
        [Authorize(Policy = AuthPolicies.CapacidadeCliente)]
        public async Task<IActionResult> RemoverPorChave(Guid clienteId, [FromQuery] string chave)
        {
            if (User.GetUserId() != clienteId)
                return Forbid();
            await _preferenciaServico.RemoverPorChaveAsync(clienteId, chave);
            return NoContent();
        }

        /// <summary>
        /// DELETE: /api/preferenciascliente/cliente/{clienteId}/limpar
        /// Remove TODAS as preferências de um cliente
        /// CUIDADO: Operação irreversível
        /// </summary>
        [HttpDelete("cliente/{clienteId:guid}/limpar")]
        [Authorize(Policy = AuthPolicies.CapacidadeClienteOuAdmin)]
        public async Task<IActionResult> LimparTodas(Guid clienteId)
        {
            if (!Authz.IsSelfOrAdmin(this, clienteId))
                return Forbid();
            await _preferenciaServico.LimparTudasAsync(clienteId);
            return NoContent();
        }
    }
}
