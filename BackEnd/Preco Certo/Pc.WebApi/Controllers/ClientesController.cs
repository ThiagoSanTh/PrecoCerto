using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Pc.Dominio.Entities.Usuarios;
using Pc.Dominio.Enums;
using Pc.Servico.Interfaces;
using Pc.WebApi.Authorization;
using Pc.WebApi.DTOs.Comum;
using Pc.WebApi.DTOs.Usuarios;

namespace Pc.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ClientesController : ControllerBase
    {
        private readonly IClienteServico _clienteServico;
        private readonly IJwtTokenServico _jwtTokenServico;

        public ClientesController(IClienteServico clienteServico, IJwtTokenServico jwtTokenServico)
        {
            _clienteServico = clienteServico;
            _jwtTokenServico = jwtTokenServico;
        }

        [AllowAnonymous]
        [HttpPost("registrar")]
        public async Task<IActionResult> Registrar([FromBody] ClienteCriarDto dto)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            try
            {
                var novoCliente = await _clienteServico.RegistrarAsync(
                    dto.NomeUsuario,
                    dto.Email,
                    dto.Senha,
                    dto.Telefone,
                    dto.LatitudeAtual,
                    dto.LongitudeAtual);

                return CreatedAtAction(
                    nameof(ObterPorId),
                    new { id = novoCliente.Id },
                    MapearResposta(novoCliente));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [AllowAnonymous]
        [EnableRateLimiting("login")]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var cliente = await _clienteServico.ValidarLoginAsync(dto.Email, dto.Senha);

            if (cliente?.Usuario == null)
                return Unauthorized("Email ou senha incorretos.");

            var perfil = MapearResposta(cliente);
            var token = _jwtTokenServico.GerarToken(
                cliente.Id,
                cliente.Usuario.Email,
                TipoUsuario.Cliente);

            return Ok(new LoginRespostaDto<ClienteRespostaDto>
            {
                Token = token,
                Tipo = "cliente",
                Perfil = perfil,
            });
        }

        [Authorize(Policy = PoliticasAutorizacao.QualquerAutenticado)]
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> ObterPorId(Guid id)
        {
            var cliente = await _clienteServico.ObterPorIdAsync(id);

            if (cliente == null)
                return NotFound("Cliente não encontrado.");

            return Ok(MapearResposta(cliente));
        }

        [Authorize(Policy = PoliticasAutorizacao.Cliente)]
        [HttpPut("{id:guid}/senha")]
        public async Task<IActionResult> AlterarSenha(Guid id, [FromBody] AlterarSenhaDto dto)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            try
            {
                await _clienteServico.AlterarSenhaAsync(id, dto.SenhaAtual, dto.NovaSenha);
                return Ok(new { mensagem = "Senha alterada com sucesso" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        private static ClienteRespostaDto MapearResposta(Cliente cliente) => new()
        {
            Id = cliente.Id,
            NomeUsuario = cliente.Usuario?.NomeUsuario ?? string.Empty,
            Email = cliente.Usuario?.Email ?? string.Empty,
            Telefone = cliente.Usuario?.Telefone,
            Tipo = (int)(cliente.Usuario?.Tipo ?? TipoUsuario.Cliente),
            UltimoLogin = cliente.Usuario?.UltimoLogin,
            LatitudeAtual = cliente.LatitudeAtual,
            LongitudeAtual = cliente.LongitudeAtual,
            Ativo = cliente.Ativo,
            DataCriacao = cliente.DataCriacao,
        };
    }
}
