using System.Data.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Pc.Dominio.Entities.Usuarios;
using Pc.Dominio.Enums;
using Pc.Servico.Interfaces;
using Pc.Servico.Modelos;
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
        private readonly IEmailService _emailService;
        private readonly IIdCodificador _idCodificador;

        public AuthController(
            IClienteServico usuarioServico,
            IAdminServico adminServico,
            IJwtTokenService jwtTokenService,
            IEmailService emailService,
            IIdCodificador idCodificador)
        {
            _usuarioServico = usuarioServico;
            _adminServico = adminServico;
            _jwtTokenService = jwtTokenService;
            _emailService = emailService;
            _idCodificador = idCodificador;
        }

        /// <summary>POST /api/auth/login — login unificado com JWT.</summary>
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
                if (EhFalhaDeBanco(ex))
                {
                    return StatusCode(503, new
                    {
                        message = "Não foi possível conectar ao banco. No Railway, defina ConnectionStrings__DefaultConnection com o pooler IPv4 (Host=aws-1-sa-east-1.pooler.supabase.com)."
                    });
                }

                if (ex.Message.Contains("IDX107", StringComparison.OrdinalIgnoreCase)
                    || ex.Message.Contains("signing key", StringComparison.OrdinalIgnoreCase))
                {
                    return StatusCode(500, new
                    {
                        message = "Jwt:Secret inválido. Defina Jwt__Secret com pelo menos 32 caracteres no Railway."
                    });
                }

                return StatusCode(500, new { message = "Erro ao processar login.", detail = ex.Message });
            }
        }

        /// <summary>POST /api/auth/esqueci-senha — envia link de redefinição por e-mail.</summary>
        [HttpPost("esqueci-senha")]
        [AllowAnonymous]
        [EnableRateLimiting("login")]
        public async Task<IActionResult> EsqueciSenha([FromBody] EsqueciSenhaDto dto)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var token = await _usuarioServico.GerarTokenRecuperacaoSenhaAsync(dto.Email);
            if (token != null)
            {
                await _emailService.EnviarRecuperacaoSenhaAsync(dto.Email.Trim(), token);
            }

            return Ok(new
            {
                mensagem = "Se o e-mail estiver cadastrado, enviamos instruções para redefinir a senha."
            });
        }

        /// <summary>POST /api/auth/redefinir-senha — redefine senha com token recebido por e-mail.</summary>
        [HttpPost("redefinir-senha")]
        [AllowAnonymous]
        [EnableRateLimiting("login")]
        public async Task<IActionResult> RedefinirSenha([FromBody] RedefinirSenhaDto dto)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            try
            {
                var ok = await _usuarioServico.RedefinirSenhaComTokenAsync(dto.Token, dto.NovaSenha);
                if (!ok)
                    return BadRequest(new { message = "Token inválido ou expirado." });

                return Ok(new { mensagem = "Senha redefinida com sucesso." });
            }
            catch (Exception ex) when (ex.Message == "Senha deve ter pelo menos 6 caracteres.")
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        private async Task<IActionResult> LoginUsuarioAsync(AuthLoginDto dto)
        {
            var result = await _usuarioServico.ValidarLoginDetalhadoAsync(dto.Email, dto.Senha);
            if (result.Erro == LoginErroCodigo.EmailNaoEncontrado)
            {
                return Unauthorized(new
                {
                    code = "EMAIL_NOT_FOUND",
                    message = "Este e-mail não existe. Por favor, insira um e-mail válido."
                });
            }

            if (result.Erro == LoginErroCodigo.SenhaIncorreta)
            {
                return Unauthorized(new
                {
                    code = "WRONG_PASSWORD",
                    message = "Senha incorreta."
                });
            }

            var usuario = result.Usuario!;

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

        private ClienteRespostaDto MapCliente(Usuario c) => new()
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

        private LojistaRespostaDto MapLojista(Usuario u, Guid? lojaId) => new()
        {
            Id = u.Id,
            CodigoPublico = _idCodificador.Codificar(u.Id),
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

        private static bool EhFalhaDeBanco(Exception ex)
        {
            for (var atual = ex; atual != null; atual = atual.InnerException)
            {
                if (atual is DbException) return true;
                if (atual.GetType().Name.Contains("RetryLimitExceeded", StringComparison.Ordinal))
                    return true;
                if (atual.Message.Contains("Npgsql", StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }
    }
}
