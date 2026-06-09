using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Pc.Dominio.Entities.Usuarios;
using Pc.Servico.Interfaces;
using Pc.WebApi.Authorization;
using Pc.WebApi.DTOs.Comum;
using Pc.WebApi.DTOs.Usuarios;
using Pc.WebApi.Services;

namespace Pc.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class LojistasController : ControllerBase
    {
        private readonly ILojistaServico _lojistaServico;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly IEmailService _emailService;

        public LojistasController(
            ILojistaServico lojistaServico,
            IJwtTokenService jwtTokenService,
            IEmailService emailService)
        {
            _lojistaServico = lojistaServico;
            _jwtTokenService = jwtTokenService;
            _emailService = emailService;
        }

        [HttpPost("registrar")]
        [AllowAnonymous]
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
                Cargo = dto.Cargo
            };

            var novoLojista = await _lojistaServico.RegistrarAsync(lojista);

            if (!string.IsNullOrWhiteSpace(novoLojista.TokenConfirmacao))
                await _emailService.EnviarConfirmacaoEmailAsync(
                    novoLojista.Email, novoLojista.NomeUsuario, novoLojista.TokenConfirmacao, "lojista");

            return CreatedAtAction(nameof(ObterPorId), new { id = novoLojista.Id }, MapResposta(novoLojista));
        }

        [HttpPost("login")]
        [AllowAnonymous]
        [EnableRateLimiting("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var lojista = await _lojistaServico.ValidarLoginAsync(dto.Email, dto.Senha);
            if (lojista == null)
                return Unauthorized("Email ou senha incorretos.");

            var lojaId = lojista.Loja?.Id;
            var perfil = MapResposta(lojista);
            var token = _jwtTokenService.GenerateToken(lojista.Id, Pc.Dominio.Enums.TipoUsuario.Lojista, lojaId);

            return Ok(new AuthLoginRespostaDto { Token = token, Tipo = "lojista", Perfil = perfil });
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> ObterPorId(Guid id)
        {
            var denied = Authz.ForbidUnlessSelfOrAdmin(this, id);
            if (denied != null) return denied;

            var lojista = await _lojistaServico.ObterPorIdAsync(id);
            if (lojista == null)
                return NotFound("Lojista não encontrado.");

            return Ok(MapResposta(lojista));
        }

        [HttpGet("email/{email}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> BuscarPorEmail(string email)
        {
            var lojista = await _lojistaServico.ObterPorEmailAsync(email);
            if (lojista == null)
                return NotFound("Lojista não encontrado.");

            return Ok(MapResposta(lojista));
        }

        [HttpGet("loja/{lojaId:guid}")]
        [Authorize(Roles = "Admin,Lojista")]
        public async Task<IActionResult> ListarPorLoja(Guid lojaId)
        {
            if (!Authz.OwnsLoja(this, lojaId))
                return Forbid();

            var lojistas = await _lojistaServico.ListarPorLojaAsync(lojaId);
            return Ok(lojistas.Select(MapResposta));
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Listar()
        {
            var lojistas = await _lojistaServico.ListarAtivosAsync();
            return Ok(lojistas.Select(MapResposta));
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Atualizar(Guid id, [FromBody] LojistaCriarDto dto)
        {
            var denied = Authz.ForbidUnlessSelfOrAdmin(this, id);
            if (denied != null) return denied;

            var lojista = await _lojistaServico.ObterPorIdAsync(id);
            if (lojista == null)
                return NotFound("Lojista não encontrado.");

            lojista.NomeUsuario = dto.NomeUsuario;
            lojista.Email = dto.Email;
            lojista.Telefone = dto.Telefone;
            lojista.Cargo = dto.Cargo;

            await _lojistaServico.AtualizarAsync(lojista);
            return Ok(MapResposta(lojista));
        }

        [HttpPut("{id:guid}/senha")]
        public async Task<IActionResult> AlterarSenha(Guid id, [FromBody] AlterarSenhaDto dto)
        {
            var denied = Authz.ForbidUnlessSelfOrAdmin(this, id);
            if (denied != null) return denied;

            await _lojistaServico.AlterarSenhaAsync(id, dto.SenhaAtual, dto.NovaSenha);
            return Ok(new { mensagem = "Senha alterada com sucesso" });
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Remover(Guid id)
        {
            await _lojistaServico.RemoverAsync(id);
            return NoContent();
        }

        private static LojistaRespostaDto MapResposta(Lojista l) => new()
        {
            Id = l.Id,
            NomeUsuario = l.NomeUsuario,
            Email = l.Email,
            Telefone = l.Telefone,
            Tipo = (int)l.Tipo,
            UltimoLogin = l.UltimoLogin,
            LojaId = l.Loja?.Id,
            NomeLoja = l.Loja?.NomeFantasia ?? string.Empty,
            Cargo = l.Cargo,
            Ativo = l.Ativo,
            DataCriacao = l.DataCriacao
        };
    }
}
