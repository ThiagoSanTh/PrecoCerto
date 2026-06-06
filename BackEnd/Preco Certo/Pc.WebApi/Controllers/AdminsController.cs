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
    public class AdminsController : ControllerBase
    {
        private readonly IAdminServico _adminServico;
        private readonly IJwtTokenService _jwtTokenService;

        public AdminsController(IAdminServico adminServico, IJwtTokenService jwtTokenService)
        {
            _adminServico = adminServico;
            _jwtTokenService = jwtTokenService;
        }

        [HttpPost("registrar")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Registrar([FromBody] AdminCriarDto dto)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var admin = new Admin
            {
                NomeUsuario = dto.NomeUsuario,
                Email = dto.Email,
                SenhaHash = dto.Senha,
                Telefone = dto.Telefone,
                NivelAcesso = dto.NivelAcesso
            };

            var novoAdmin = await _adminServico.RegistrarAsync(admin);
            return CreatedAtAction(nameof(ObterPorId), new { id = novoAdmin.Id }, MapResposta(novoAdmin));
        }

        [HttpPost("login")]
        [AllowAnonymous]
        [EnableRateLimiting("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var admin = await _adminServico.ValidarLoginAsync(dto.Email, dto.Senha);
            if (admin == null)
                return Unauthorized("Email ou senha incorretos.");

            var perfil = MapResposta(admin);
            var token = _jwtTokenService.GenerateToken(admin.Id, Pc.Dominio.Enums.TipoUsuario.Admin);

            return Ok(new AuthLoginRespostaDto { Token = token, Tipo = "admin", Perfil = perfil });
        }

        [HttpGet("{id:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ObterPorId(Guid id)
        {
            var admin = await _adminServico.ObterPorIdAsync(id);
            if (admin == null)
                return NotFound("Admin não encontrado.");

            return Ok(MapResposta(admin));
        }

        [HttpGet("email/{email}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> BuscarPorEmail(string email)
        {
            var admin = await _adminServico.ObterPorEmailAsync(email);
            if (admin == null)
                return NotFound("Admin não encontrado.");

            return Ok(MapResposta(admin));
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Listar()
        {
            var admins = await _adminServico.ListarAtivosAsync();
            return Ok(admins.Select(MapResposta));
        }

        [HttpPut("{id:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Atualizar(Guid id, [FromBody] AdminCriarDto dto)
        {
            var admin = await _adminServico.ObterPorIdAsync(id);
            if (admin == null)
                return NotFound("Admin não encontrado.");

            admin.NomeUsuario = dto.NomeUsuario;
            admin.Email = dto.Email;
            admin.Telefone = dto.Telefone;
            admin.NivelAcesso = dto.NivelAcesso;

            await _adminServico.AtualizarAsync(admin);
            return Ok(MapResposta(admin));
        }

        [HttpPut("{id:guid}/senha")]
        public async Task<IActionResult> AlterarSenha(Guid id, [FromBody] AlterarSenhaDto dto)
        {
            var denied = Authz.ForbidUnlessSelfOrAdmin(this, id);
            if (denied != null) return denied;

            await _adminServico.AlterarSenhaAsync(id, dto.SenhaAtual, dto.NovaSenha);
            return Ok(new { mensagem = "Senha alterada com sucesso" });
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Remover(Guid id)
        {
            await _adminServico.RemoverAsync(id);
            return NoContent();
        }

        private static AdminRespostaDto MapResposta(Admin a) => new()
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
