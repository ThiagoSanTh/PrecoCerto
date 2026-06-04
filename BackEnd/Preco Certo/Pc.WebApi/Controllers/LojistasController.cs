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
    public class LojistasController : ControllerBase
    {
        private readonly ILojistaServico _lojistaServico;
        private readonly IJwtTokenServico _jwtTokenServico;

        public LojistasController(ILojistaServico lojistaServico, IJwtTokenServico jwtTokenServico)
        {
            _lojistaServico = lojistaServico;
            _jwtTokenServico = jwtTokenServico;
        }

        [AllowAnonymous]
        [HttpPost("registrar")]
        public async Task<IActionResult> Registrar([FromBody] LojistaCriarDto dto)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            try
            {
                var novoLojista = await _lojistaServico.RegistrarAsync(
                    dto.NomeUsuario,
                    dto.Email,
                    dto.Senha,
                    dto.Telefone,
                    dto.Cargo);

                return CreatedAtAction(
                    nameof(ObterPorId),
                    new { id = novoLojista.Id },
                    MapearResposta(novoLojista));
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

            var lojista = await _lojistaServico.ValidarLoginAsync(dto.Email, dto.Senha);

            if (lojista?.Usuario == null)
                return Unauthorized("Email ou senha incorretos.");

            var perfil = MapearResposta(lojista);
            var token = _jwtTokenServico.GerarToken(
                lojista.Id,
                lojista.Usuario.Email,
                TipoUsuario.Lojista);

            return Ok(new LoginRespostaDto<LojistaRespostaDto>
            {
                Token = token,
                Tipo = "lojista",
                Perfil = perfil,
            });
        }

        [Authorize(Policy = PoliticasAutorizacao.QualquerAutenticado)]
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> ObterPorId(Guid id)
        {
            var lojista = await _lojistaServico.ObterPorIdAsync(id);

            if (lojista == null)
                return NotFound("Lojista não encontrado.");

            return Ok(MapearResposta(lojista));
        }

        private static LojistaRespostaDto MapearResposta(Lojista lojista) => new()
        {
            Id = lojista.Id,
            NomeUsuario = lojista.Usuario?.NomeUsuario ?? string.Empty,
            Email = lojista.Usuario?.Email ?? string.Empty,
            Telefone = lojista.Usuario?.Telefone,
            Tipo = (int)(lojista.Usuario?.Tipo ?? TipoUsuario.Lojista),
            UltimoLogin = lojista.Usuario?.UltimoLogin,
            LojaId = lojista.Loja?.Id ?? (lojista.LojaId == Guid.Empty ? null : lojista.LojaId),
            NomeLoja = lojista.Loja?.NomeFantasia ?? string.Empty,
            Cargo = lojista.Cargo,
            Ativo = lojista.Ativo,
            DataCriacao = lojista.DataCriacao,
        };
    }
}
