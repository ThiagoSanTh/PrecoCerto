using Microsoft.AspNetCore.Mvc;
using Pc.Dominio.Entities.Interacoes;
using Pc.Servico.Interfaces;
using Pc.WebApi.DTOs.Comum;
using Pc.WebApi.DTOs.Interacoes;

namespace Pc.WebApi.Controllers
{
    /// <summary>
    /// Controller para operações com Preferências do Cliente
    /// Endpoints para gerenciar configurações e preferências do usuário
    /// Usa padrão chave-valor para máxima flexibilidade
    /// Rota base: /api/preferenciascliente
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
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
        public async Task<IActionResult> Salvar([FromBody] PreferenciaClienteCriarDto dto)
        {
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
        public async Task<IActionResult> ListarPorCliente(Guid clienteId)
        {
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
        public async Task<IActionResult> ObterValor(Guid clienteId, [FromQuery] string chave)
        {
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
        public async Task<IActionResult> Atualizar(Guid clienteId, [FromQuery] string chave, [FromBody] PreferenciaValorDto dto)
        {
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
        public async Task<IActionResult> RemoverPorChave(Guid clienteId, [FromQuery] string chave)
        {
            await _preferenciaServico.RemoverPorChaveAsync(clienteId, chave);
            return NoContent();
        }

        /// <summary>
        /// DELETE: /api/preferenciascliente/cliente/{clienteId}/limpar
        /// Remove TODAS as preferências de um cliente
        /// CUIDADO: Operação irreversível
        /// </summary>
        [HttpDelete("cliente/{clienteId:guid}/limpar")]
        public async Task<IActionResult> LimparTodas(Guid clienteId)
        {
            await _preferenciaServico.LimparTudasAsync(clienteId);
            return NoContent();
        }
    }
}
