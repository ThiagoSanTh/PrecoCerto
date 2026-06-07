using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Pc.Dominio.Entities.Usuarios;
using Pc.Servico.Interfaces;
using Pc.WebApi.Authorization;
using Pc.WebApi.DTOs.Comum;
using Pc.WebApi.DTOs.Usuarios;
using Pc.WebApi.Extensions;
using Pc.WebApi.Services;

namespace Pc.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ClientesController : ControllerBase
    {
        private readonly IClienteServico _clienteServico;
        private readonly IJwtTokenService _jwtTokenService;

        public ClientesController(IClienteServico clienteServico, IJwtTokenService jwtTokenService)
        {
            _clienteServico = clienteServico;
            _jwtTokenService = jwtTokenService;
        }

        [HttpPost("registrar")]
        [AllowAnonymous]
        public async Task<IActionResult> Registrar([FromBody] ClienteCriarDto dto)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var cliente = new Cliente
            {
                NomeUsuario = dto.NomeUsuario,
                Email = dto.Email,
                SenhaHash = dto.Senha,
                Telefone = dto.Telefone,
                LatitudeAtual = dto.LatitudeAtual,
                LongitudeAtual = dto.LongitudeAtual
            };

            var novoCliente = await _clienteServico.RegistrarAsync(cliente);
            return CreatedAtAction(nameof(ObterPorId), new { id = novoCliente.Id }, MapResposta(novoCliente));
        }

        [HttpPost("login")]
        [AllowAnonymous]
        [EnableRateLimiting("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var cliente = await _clienteServico.ValidarLoginAsync(dto.Email, dto.Senha);
            if (cliente == null)
                return Unauthorized("Email ou senha incorretos.");

            var perfil = MapResposta(cliente);
            var token = _jwtTokenService.GenerateToken(cliente.Id, Pc.Dominio.Enums.TipoUsuario.Cliente);

            return Ok(new AuthLoginRespostaDto { Token = token, Tipo = "cliente", Perfil = perfil });
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> ObterPorId(Guid id)
        {
            var denied = Authz.ForbidUnlessSelfOrAdmin(this, id);
            if (denied != null) return denied;

            var cliente = await _clienteServico.ObterPorIdAsync(id);
            if (cliente == null)
                return NotFound("Cliente não encontrado.");

            return Ok(MapResposta(cliente));
        }

        [HttpGet("email/{email}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> BuscarPorEmail(string email)
        {
            var cliente = await _clienteServico.ObterPorEmailAsync(email);
            if (cliente == null)
                return NotFound("Cliente não encontrado.");

            return Ok(MapResposta(cliente));
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Listar()
        {
            var clientes = await _clienteServico.ListarAtivosAsync();
            return Ok(clientes.Select(MapResposta));
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Atualizar(Guid id, [FromBody] ClienteCriarDto dto)
        {
            var denied = Authz.ForbidUnlessSelfOrAdmin(this, id);
            if (denied != null) return denied;

            var cliente = await _clienteServico.ObterPorIdAsync(id);
            if (cliente == null)
                return NotFound("Cliente não encontrado.");

            cliente.NomeUsuario = dto.NomeUsuario;
            cliente.Email = dto.Email;
            cliente.Telefone = dto.Telefone;

            await _clienteServico.AtualizarAsync(cliente);
            return Ok(MapResposta(cliente));
        }

        [HttpPut("{id:guid}/localizacao")]
        public async Task<IActionResult> AtualizarLocalizacao(Guid id, [FromBody] LocalizacaoDto dto)
        {
            var denied = Authz.ForbidUnlessSelfOrAdmin(this, id);
            if (denied != null) return denied;

            await _clienteServico.AtualizarLocalizacaoAsync(id, dto.Latitude, dto.Longitude);
            return Ok(new { mensagem = "Localização atualizada com sucesso" });
        }

        [HttpGet("proximidade/buscar")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> BuscarPorProximidade(
            [FromQuery] decimal latitude,
            [FromQuery] decimal longitude,
            [FromQuery] decimal raio = 5)
        {
            var clientes = await _clienteServico.ObterPorProximidadeAsync(latitude, longitude, raio);
            return Ok(clientes.Select(MapResposta));
        }

        [HttpPut("{id:guid}/senha")]
        public async Task<IActionResult> AlterarSenha(Guid id, [FromBody] AlterarSenhaDto dto)
        {
            var denied = Authz.ForbidUnlessSelfOrAdmin(this, id);
            if (denied != null) return denied;

            await _clienteServico.AlterarSenhaAsync(id, dto.SenhaAtual, dto.NovaSenha);
            return Ok(new { mensagem = "Senha alterada com sucesso" });
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Remover(Guid id)
        {
            await _clienteServico.RemoverAsync(id);
            return NoContent();
        }

        private static ClienteRespostaDto MapResposta(Cliente c) => new()
        {
            Id = c.Id,
            NomeUsuario = c.NomeUsuario,
            Email = c.Email,
            Telefone = c.Telefone,
            Tipo = (int)c.Tipo,
            UltimoLogin = c.UltimoLogin,
            LatitudeAtual = c.LatitudeAtual,
            LongitudeAtual = c.LongitudeAtual,
            Ativo = c.Ativo,
            DataCriacao = c.DataCriacao
        };
    }
}
