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

            var lojista = new Lojista
            {
                NomeUsuario = dto.NomeUsuario,
                Email = dto.Email,
                SenhaHash = dto.Senha,
                Telefone = dto.Telefone,
                LojaId = dto.LojaId,
                Cargo = dto.Cargo
            };

            var novoLojista = await _lojistaServico.RegistrarAsync(lojista);
            return CreatedAtAction(nameof(ObterPorId), new { id = novoLojista.Id }, MapearResposta(novoLojista));
        }

        [AllowAnonymous]
        [EnableRateLimiting("login")]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var lojista = await _lojistaServico.ValidarLoginAsync(dto.Email, dto.Senha);

            if (lojista == null)
                return Unauthorized("Email ou senha incorretos.");

            var perfil = MapearResposta(lojista);
            var token = _jwtTokenServico.GerarToken(lojista.Id, lojista.Email, TipoUsuario.Lojista);

            return Ok(new LoginRespostaDto<LojistaRespostaDto>
            {
                Token = token,
                Tipo = "lojista",
                Perfil = perfil,
            });
        }

        private static LojistaRespostaDto MapearResposta(Lojista lojista) => new()
        {
            Id = lojista.Id,
            NomeUsuario = lojista.NomeUsuario,
            Email = lojista.Email,
            Telefone = lojista.Telefone,
            Tipo = (int)lojista.Tipo,
            UltimoLogin = lojista.UltimoLogin,
            LojaId = lojista.Loja?.Id ?? lojista.LojaId,
            NomeLoja = lojista.Loja?.NomeFantasia ?? string.Empty,
            Cargo = lojista.Cargo,
            Ativo = lojista.Ativo,
            DataCriacao = lojista.DataCriacao
        };

        [Authorize(Policy = PoliticasAutorizacao.QualquerAutenticado)]
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> ObterPorId(Guid id)
        {
            var lojista = await _lojistaServico.ObterPorIdAsync(id);
            if (lojista == null)
                return NotFound("Lojista não encontrado.");

            return Ok(MapearResposta(lojista));
        }

        [Authorize(Policy = PoliticasAutorizacao.Admin)]
        [HttpGet("email/{email}")]
        public async Task<IActionResult> BuscarPorEmail(string email)
        {
            var lojista = await _lojistaServico.ObterPorEmailAsync(email);
            if (lojista == null)
                return NotFound("Lojista não encontrado.");

            return Ok(MapearResposta(lojista));
        }

        [Authorize(Policy = PoliticasAutorizacao.QualquerAutenticado)]
        [HttpGet("loja/{lojaId:guid}")]
        public async Task<IActionResult> ListarPorLoja(Guid lojaId)
        {
            var lojistas = await _lojistaServico.ListarPorLojaAsync(lojaId);
            return Ok(lojistas.Select(MapearResposta));
        }

        [Authorize(Policy = PoliticasAutorizacao.Admin)]
        [HttpGet]
        public async Task<IActionResult> Listar()
        {
            var lojistas = await _lojistaServico.ListarAtivosAsync();
            return Ok(lojistas.Select(MapearResposta));
        }

        [Authorize(Policy = PoliticasAutorizacao.Lojista)]
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Atualizar(Guid id, [FromBody] LojistaCriarDto dto)
        {
            var lojista = await _lojistaServico.ObterPorIdAsync(id);
            if (lojista == null)
                return NotFound("Lojista não encontrado.");

            lojista.NomeUsuario = dto.NomeUsuario;
            lojista.Email = dto.Email;
            lojista.Telefone = dto.Telefone;
            lojista.LojaId = dto.LojaId;
            lojista.Cargo = dto.Cargo;

            await _lojistaServico.AtualizarAsync(lojista);
            return Ok(MapearResposta(lojista));
        }

        [Authorize(Policy = PoliticasAutorizacao.Lojista)]
        [HttpPut("{id:guid}/senha")]
        public async Task<IActionResult> AlterarSenha(Guid id, [FromBody] AlterarSenhaDto dto)
        {
            await _lojistaServico.AlterarSenhaAsync(id, dto.SenhaAtual, dto.NovaSenha);
            return Ok(new { mensagem = "Senha alterada com sucesso" });
        }

        [Authorize(Policy = PoliticasAutorizacao.Admin)]
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Remover(Guid id)
        {
            await _lojistaServico.RemoverAsync(id);
            return NoContent();
        }
    }
}
