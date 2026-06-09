using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pc.Dominio.Entities.Catalogo;
using Pc.Dominio.Entities.Estabelecimentos;
using Pc.Dominio.Validacoes;
using Pc.Servico.Interfaces;
using Pc.WebApi.Authorization;
using Pc.WebApi.DTOs.Estabelecimentos;
using Pc.WebApi.Extensions;
using Pc.WebApi.Mappings;

namespace Pc.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LojasController : ControllerBase
    {
        private readonly ILojaServico _lojaServico;
        private readonly IClienteServico _usuarioServico;

        public LojasController(ILojaServico lojaServico, IClienteServico usuarioServico)
        {
            _lojaServico = lojaServico;
            _usuarioServico = usuarioServico;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Listar()
        {
            var lojas = await _lojaServico.ListarAsync();
            return Ok(lojas.Select(LojaMapper.ParaRespostaDto));
        }

        [HttpGet("{id:guid}")]
        [AllowAnonymous]
        public async Task<IActionResult> ObterPorId(Guid id)
        {
            var loja = await _lojaServico.ObterPorIdAsync(id);
            if (loja is null)
                return NotFound("Loja não encontrada.");

            return Ok(LojaMapper.ParaRespostaDto(loja));
        }

        [HttpPost("buscar")]
        [AllowAnonymous]
        public async Task<IActionResult> BuscarPorNome([FromBody] string nome)
        {
            var lojas = await _lojaServico.BuscarPorNomeAsync(nome);
            return Ok(lojas.Select(LojaMapper.ParaRespostaDto));
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Adicionar([FromBody] LojaCriarDto dto)
        {
            // Abrir loja exige CNPJ válido — é isso que promove o usuário a Lojista.
            if (!CnpjValidator.IsValido(dto.Cnpj))
                return BadRequest("CNPJ inválido. É necessário um CNPJ válido para abrir uma loja.");

            // O dono da loja é o usuário autenticado (admin pode informar outro).
            var usuarioId = User.GetUserId();
            if (User.IsAdmin() && dto.UsuarioId.HasValue && dto.UsuarioId.Value != Guid.Empty)
                usuarioId = dto.UsuarioId;

            if (usuarioId is null || usuarioId == Guid.Empty)
                return BadRequest("Usuário inválido.");

            var loja = new Loja
            {
                NomeFantasia = dto.NomeFantasia,
                RazaoSocial = dto.RazaoSocial,
                Cnpj = CnpjValidator.ApenasDigitos(dto.Cnpj),
                Telefone = dto.Telefone,
                Email = dto.Email,
                Descricao = dto.Descricao,
                UsuarioId = usuarioId,
                Endereco = CriarEndereco(dto.Endereco)
            };

            var novaLoja = await _lojaServico.AdicionarAsync(loja);

            // Promove o usuário a Lojista (papel + tipo).
            await _usuarioServico.DefinirComoLojistaAsync(usuarioId.Value);

            return CreatedAtAction(nameof(ObterPorId), new { id = novaLoja.Id }, LojaMapper.ParaRespostaDto(novaLoja));
        }

        [HttpPut("{id:guid}")]
        [Authorize(Roles = "Lojista,Admin")]
        public async Task<IActionResult> Atualizar(Guid id, [FromBody] LojaCriarDto dto)
        {
            var lojaExistente = await _lojaServico.ObterPorIdAsync(id);
            if (lojaExistente is null)
                return NotFound("Loja não encontrada.");

            if (!Authz.OwnsLoja(this, id) && !User.IsAdmin())
            {
                if (lojaExistente.UsuarioId != User.GetUserId())
                    return Forbid();
            }

            if (!string.IsNullOrWhiteSpace(dto.Cnpj) && !CnpjValidator.IsValido(dto.Cnpj))
                return BadRequest("CNPJ inválido.");

            lojaExistente.NomeFantasia = dto.NomeFantasia;
            lojaExistente.RazaoSocial = dto.RazaoSocial;
            lojaExistente.Cnpj = string.IsNullOrWhiteSpace(dto.Cnpj) ? dto.Cnpj : CnpjValidator.ApenasDigitos(dto.Cnpj);
            lojaExistente.Telefone = dto.Telefone;
            lojaExistente.Email = dto.Email;
            lojaExistente.Descricao = dto.Descricao;

            if (dto.EnderecoId.HasValue)
                lojaExistente.EnderecoId = dto.EnderecoId.Value;

            if (dto.UsuarioId.HasValue)
                lojaExistente.UsuarioId = dto.UsuarioId.Value;

            if (dto.Endereco is not null)
                lojaExistente.Endereco = CriarEndereco(dto.Endereco);

            await _lojaServico.AtualizarAsync(lojaExistente);
            return NoContent();
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Remover(Guid id)
        {
            var lojaExistente = await _lojaServico.ObterPorIdAsync(id);
            if (lojaExistente is null)
                return NotFound("Loja não encontrada.");

            await _lojaServico.RemoverAsync(id);
            return NoContent();
        }

        private static Endereco CriarEndereco(EnderecoDto? endereco)
        {
            if (endereco is null)
                return new Endereco();

            return new Endereco
            {
                Cep = endereco.Cep,
                Logradouro = endereco.Logradouro,
                Numero = endereco.Numero,
                Bairro = endereco.Bairro,
                Cidade = endereco.Cidade,
                Estado = endereco.Estado,
                Latitude = endereco.Latitude,
                Longitude = endereco.Longitude
            };
        }
    }
}
