using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Pc.Dominio.Entities.Usuarios;
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
        private readonly IClienteServico _usuarioServico;
        private readonly IAdminServico _adminServico;
        private readonly IJwtTokenService _jwtTokenService;

        public AuthController(
            IClienteServico usuarioServico,
            IAdminServico adminServico,
            IJwtTokenService jwtTokenService)
        {
            _usuarioServico = usuarioServico;
            _adminServico = adminServico;
            _jwtTokenService = jwtTokenService;
        }

        /// <summary>POST /api/auth/login — login unificado com JWT.</summary>
        /// <param name="dto">Credenciais e tipo opcional (admin para login administrativo).</param>
        /// <returns>Token JWT e perfil do usuário autenticado (papel derivado do servidor).</returns>
        [HttpPost("login")]
        [AllowAnonymous]
        [EnableRateLimiting("login")]
        [ProducesResponseType(typeof(AuthLoginRespostaDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> Login([FromBody] AuthLoginDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return ValidationProblem(ModelState);

                var tipo = (dto.Tipo ?? "").Trim().ToLowerInvariant();
                if (tipo == "admin")
                    return await LoginAdminAsync(dto);

                return await LoginUsuarioAsync(dto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erro ao processar login.", detail = ex.Message });
            }
        }

        private async Task<IActionResult> LoginUsuarioAsync(AuthLoginDto dto)
        {
            var usuario = await _usuarioServico.ValidarLoginAsync(dto.Email, dto.Senha);
            if (usuario == null)
                return Unauthorized("Email ou senha incorretos.");

            // Papel e lojaId são derivados no servidor (não confiamos no cliente).
            var lojaId = usuario.Papel switch
            {
                PapelUsuario.Lojista => usuario.LojaPropria?.Id,
                PapelUsuario.Vendedor => usuario.LojaVinculadaId,
                _ => null
            };

            var tipoJwt = usuario.Papel switch
            {
                PapelUsuario.Lojista => TipoUsuario.Lojista,
                PapelUsuario.Vendedor => TipoUsuario.Vendedor,
                _ => TipoUsuario.Cliente
            };

            var token = _jwtTokenService.GenerateToken(usuario.Id, tipoJwt, lojaId);

            if (usuario.Papel == PapelUsuario.Cliente)
            {
                return Ok(new AuthLoginRespostaDto
                {
                    Token = token,
                    Tipo = "cliente",
                    Perfil = MapCliente(usuario)
                });
            }

            var tipoStr = usuario.Papel == PapelUsuario.Vendedor ? "vendedor" : "lojista";
            return Ok(new AuthLoginRespostaDto
            {
                Token = token,
                Tipo = tipoStr,
                Perfil = MapLojista(usuario, lojaId)
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

        /// <summary>
        /// GET /api/auth/confirmar-email?token=...
        /// Confirma o e-mail do usuário a partir do token enviado no cadastro.
        /// </summary>
        [HttpGet("confirmar-email")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ConfirmarEmail([FromQuery] string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return BadRequest("Token inválido.");

            var confirmado = await _usuarioServico.ConfirmarEmailAsync(token);
            if (!confirmado)
                return BadRequest("Token inválido ou e-mail já confirmado.");

            return Content(
                "<html><body style='font-family:sans-serif;text-align:center;padding:40px'>" +
                "<h2>E-mail confirmado com sucesso!</h2>" +
                "<p>Você já pode usar o Preço Certo normalmente.</p>" +
                "</body></html>",
                "text/html");
        }

        private static ClienteRespostaDto MapCliente(Usuario c) => new()
        {
            Id = c.Id,
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

        private static LojistaRespostaDto MapLojista(Usuario u, Guid? lojaId) => new()
        {
            Id = u.Id,
            NomeUsuario = u.NomeUsuario,
            Email = u.Email,
            Telefone = u.Telefone,
            Tipo = (int)u.Tipo,
            Papel = (int)u.Papel,
            UltimoLogin = u.UltimoLogin,
            LojaId = lojaId,
            NomeLoja = u.LojaPropria?.NomeFantasia ?? string.Empty,
            Cargo = u.Cargo,
            Ativo = u.Ativo,
            DataCriacao = u.DataCriacao
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
