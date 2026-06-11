using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Pc.Dominio.Entities.Usuarios;
using Pc.Dominio.Enums;
using Pc.Servico.Excecoes;
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
        private readonly IEmailService _emailService;
        private readonly IIdCodificador _idCodificador;

        public ClientesController(
            IClienteServico clienteServico,
            IJwtTokenService jwtTokenService,
            IEmailService emailService,
            IIdCodificador idCodificador)
        {
            _clienteServico = clienteServico;
            _jwtTokenService = jwtTokenService;
            _emailService = emailService;
            _idCodificador = idCodificador;
        }

        [HttpPost("registrar")]
        [AllowAnonymous]
        public async Task<IActionResult> Registrar([FromBody] ClienteCriarDto dto)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            try
            {
                var cliente = new Usuario
                {
                    NomeUsuario = dto.NomeUsuario,
                    Email = dto.Email,
                    SenhaHash = dto.Senha,
                    Telefone = dto.Telefone,
                    LatitudeAtual = dto.LatitudeAtual,
                    LongitudeAtual = dto.LongitudeAtual
                };

                var novoCliente = await _clienteServico.RegistrarAsync(cliente);

                _ = _emailService.EnviarBoasVindasAsync(novoCliente.Email, novoCliente.NomeUsuario);

                var perfil = MapResposta(novoCliente);
                var token = _jwtTokenService.GenerateToken(novoCliente.Id, TipoUsuario.Cliente);

                return Ok(new AuthLoginRespostaDto { Token = token, Tipo = "cliente", Perfil = perfil });
            }
            catch (DbUpdateException)
            {
                return Conflict(new { message = "Este e-mail já está cadastrado." });
            }
            catch (EmailJaRegistradoException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (EmailInvalidoException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex) when (
                ex.Message is "Email é obrigatório."
                    or "Nome de usuário é obrigatório."
                    or "Senha deve ter pelo menos 6 caracteres.")
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("login")]
        [AllowAnonymous]
        [EnableRateLimiting("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            Usuario? cliente = await _clienteServico.ValidarLoginAsync(dto.Email, dto.Senha);

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
            cliente.Telefone = dto.Telefone;

            await _clienteServico.AtualizarAsync(cliente);
            return Ok(MapResposta(cliente));
        }

        [HttpPut("{id:guid}/email")]
        public async Task<IActionResult> AlterarEmail(Guid id, [FromBody] AlterarEmailDto dto)
        {
            var denied = Authz.ForbidUnlessSelfOrAdmin(this, id);
            if (denied != null) return denied;

            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            try
            {
                var cliente = await _clienteServico.ObterPorIdAsync(id);
                if (cliente == null)
                    return NotFound("Cliente não encontrado.");

                var emailAntigo = cliente.Email;
                await _clienteServico.AlterarEmailAsync(id, dto.SenhaAtual, dto.NovoEmail);

                var atualizado = await _clienteServico.ObterPorIdAsync(id);
                if (atualizado != null)
                {
                    await _emailService.EnviarNotificacaoAlteracaoEmailAsync(
                        emailAntigo, atualizado.Email, atualizado.NomeUsuario);
                }

                return Ok(MapResposta(atualizado!));
            }
            catch (EmailJaRegistradoException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (EmailInvalidoException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex) when (ex.Message is "Senha atual incorreta." or "Usuário não encontrado.")
            {
                return BadRequest(new { message = ex.Message });
            }
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

        private ClienteRespostaDto MapResposta(Usuario c) => new()
        {
            Id = c.Id,
            CodigoPublico = _idCodificador.Codificar(c.Id),
            NomeUsuario = c.NomeUsuario,
            Email = c.Email,
            Telefone = c.Telefone,
            Tipo = (int)c.Tipo,
            Papel = (int)c.Papel,
            UltimoLogin = c.UltimoLogin,
            LatitudeAtual = c.LatitudeAtual,
            LongitudeAtual = c.LongitudeAtual,
            Ativo = c.Ativo,
            DataCriacao = c.DataCriacao
        };
    }
}
