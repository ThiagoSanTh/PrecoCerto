using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Pc.Dominio.Enums;
using Pc.Servico.Interfaces;
using Pc.WebApi.DTOs.Comum;
using Pc.WebApi.DTOs.Usuarios;
using Pc.WebApi.Services;

namespace Pc.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IClienteServico _clienteServico;
        private readonly ILojistaServico _lojistaServico;
        private readonly IAdminServico _adminServico;
        private readonly IJwtTokenService _jwtTokenService;

        public AuthController(
            IClienteServico clienteServico,
            ILojistaServico lojistaServico,
            IAdminServico adminServico,
            IJwtTokenService jwtTokenService)
        {
            _clienteServico = clienteServico;
            _lojistaServico = lojistaServico;
            _adminServico = adminServico;
            _jwtTokenService = jwtTokenService;
        }

        /// <summary>POST /api/auth/login — login unificado com JWT.</summary>
        [HttpPost("login")]
        [AllowAnonymous]
        [EnableRateLimiting("login")]
        public async Task<IActionResult> Login([FromBody] AuthLoginDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return ValidationProblem(ModelState);

                var tipo = (dto.Tipo ?? "cliente").Trim().ToLowerInvariant();

                return tipo switch
                {
                    "cliente" => await LoginClienteAsync(dto),
                    "lojista" => await LoginLojistaAsync(dto),
                    "admin" => await LoginAdminAsync(dto),
                    _ => BadRequest("Tipo inválido. Use: cliente, lojista ou admin.")
                };
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erro ao processar login.", detail = ex.Message });
            }
        }

        private async Task<IActionResult> LoginClienteAsync(AuthLoginDto dto)
        {
            var cliente = await _clienteServico.ValidarLoginAsync(dto.Email, dto.Senha);
            if (cliente == null)
                return Unauthorized("Email ou senha incorretos.");

            var perfil = MapCliente(cliente);
            var token = _jwtTokenService.GenerateToken(cliente.Id, TipoUsuario.Cliente);

            return Ok(new AuthLoginRespostaDto
            {
                Token = token,
                Tipo = "cliente",
                Perfil = perfil
            });
        }

        private async Task<IActionResult> LoginLojistaAsync(AuthLoginDto dto)
        {
            var lojista = await _lojistaServico.ValidarLoginAsync(dto.Email, dto.Senha);
            if (lojista == null)
                return Unauthorized("Email ou senha incorretos.");

            var lojaId = lojista.Loja?.Id ?? lojista.LojaId;
            var perfil = MapLojista(lojista);
            var token = _jwtTokenService.GenerateToken(lojista.Id, TipoUsuario.Lojista, lojaId);

            return Ok(new AuthLoginRespostaDto
            {
                Token = token,
                Tipo = "lojista",
                Perfil = perfil
            });
        }

        private async Task<IActionResult> LoginAdminAsync(AuthLoginDto dto)
        {
            var admin = await _adminServico.ValidarLoginAsync(dto.Email, dto.Senha);
            if (admin == null)
                return Unauthorized("Email ou senha incorretos.");

            var perfil = MapAdmin(admin);
            var token = _jwtTokenService.GenerateToken(admin.Id, TipoUsuario.Admin);

            return Ok(new AuthLoginRespostaDto
            {
                Token = token,
                Tipo = "admin",
                Perfil = perfil
            });
        }

        private static ClienteRespostaDto MapCliente(Pc.Dominio.Entities.Usuarios.Cliente c) => new()
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

        private static LojistaRespostaDto MapLojista(Pc.Dominio.Entities.Usuarios.Lojista l) => new()
        {
            Id = l.Id,
            NomeUsuario = l.NomeUsuario,
            Email = l.Email,
            Telefone = l.Telefone,
            Tipo = (int)l.Tipo,
            UltimoLogin = l.UltimoLogin,
            LojaId = l.Loja?.Id ?? l.LojaId,
            NomeLoja = l.Loja?.NomeFantasia ?? string.Empty,
            Cargo = l.Cargo,
            Ativo = l.Ativo,
            DataCriacao = l.DataCriacao
        };

        private static AdminRespostaDto MapAdmin(Pc.Dominio.Entities.Usuarios.Admin a) => new()
        {
            Id = a.Id,
            NomeUsuario = a.NomeUsuario,
            Email = a.Email,
            Telefone = a.Telefone,
            Tipo = (int)a.Tipo,
            NivelAcesso = a.NivelAcesso,
            Permissoes = a.Permissoes,
            UltimoLogin = a.UltimoLogin,
            Ativo = a.Ativo,
            DataCriacao = a.DataCriacao
        };
    }
}
