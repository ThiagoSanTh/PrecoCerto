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
    public class AdminsController : ControllerBase
    {
        private readonly IAdminServico _adminServico;
        private readonly IJwtTokenServico _jwtTokenServico;

        public AdminsController(IAdminServico adminServico, IJwtTokenServico jwtTokenServico)
        {
            _adminServico = adminServico;
            _jwtTokenServico = jwtTokenServico;
        }

        [Authorize(Policy = PoliticasAutorizacao.Admin)]
        [HttpPost("registrar")]
        public async Task<IActionResult> Registrar([FromBody] AdminCriarDto dto)
        {
            var admin = new Admin
            {
                NomeUsuario = dto.NomeUsuario,
                Email = dto.Email,
                SenhaHash = dto.Senha,
                Telefone = dto.Telefone,
                NivelAcesso = dto.NivelAcesso
            };

            var novoAdmin = await _adminServico.RegistrarAsync(admin);

            var resposta = new AdminRespostaDto
            {
                Id = novoAdmin.Id,
                NomeUsuario = novoAdmin.NomeUsuario,
                Email = novoAdmin.Email,
                Telefone = novoAdmin.Telefone,
                Tipo = (int)novoAdmin.Tipo,
                NivelAcesso = novoAdmin.NivelAcesso,
                Permissoes = novoAdmin.Permissoes,
                UltimoLogin = novoAdmin.UltimoLogin,
                Ativo = novoAdmin.Ativo,
                DataCriacao = novoAdmin.DataCriacao
            };

            return CreatedAtAction(nameof(ObterPorId), new { id = resposta.Id }, resposta);
        }

        [AllowAnonymous]
        [EnableRateLimiting("login")]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var admin = await _adminServico.ValidarLoginAsync(dto.Email, dto.Senha);

            if (admin == null)
                return Unauthorized("Email ou senha incorretos.");

            var perfil = new AdminRespostaDto
            {
                Id = admin.Id,
                NomeUsuario = admin.NomeUsuario,
                Email = admin.Email,
                Telefone = admin.Telefone,
                Tipo = (int)admin.Tipo,
                NivelAcesso = admin.NivelAcesso,
                Permissoes = admin.Permissoes,
                UltimoLogin = admin.UltimoLogin,
                Ativo = admin.Ativo,
                DataCriacao = admin.DataCriacao
            };

            var token = _jwtTokenServico.GerarToken(admin.Id, admin.Email, TipoUsuario.Admin);

            return Ok(new LoginRespostaDto<AdminRespostaDto>
            {
                Token = token,
                Tipo = "admin",
                Perfil = perfil,
            });
        }

        /// <summary>
        /// GET: /api/admins/{id}
        /// Retorna um admin específico
        /// </summary>
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> ObterPorId(Guid id)
        {
            var admin = await _adminServico.ObterPorIdAsync(id);

            if (admin == null)
                return NotFound("Admin não encontrado.");

            var resposta = new AdminRespostaDto
            {
                Id = admin.Id,
                NomeUsuario = admin.NomeUsuario,
                Email = admin.Email,
                Telefone = admin.Telefone,
                Tipo = (int)admin.Tipo,
                NivelAcesso = admin.NivelAcesso,
                Permissoes = admin.Permissoes,
                UltimoLogin = admin.UltimoLogin,
                Ativo = admin.Ativo,
                DataCriacao = admin.DataCriacao
            };

            return Ok(resposta);
        }

        /// <summary>
        /// GET: /api/admins/email/{email}
        /// Busca admin por email
        /// </summary>
        [HttpGet("email/{email}")]
        public async Task<IActionResult> BuscarPorEmail(string email)
        {
            var admin = await _adminServico.ObterPorEmailAsync(email);

            if (admin == null)
                return NotFound("Admin não encontrado.");

            var resposta = new AdminRespostaDto
            {
                Id = admin.Id,
                NomeUsuario = admin.NomeUsuario,
                Email = admin.Email,
                Telefone = admin.Telefone,
                Tipo = (int)admin.Tipo,
                NivelAcesso = admin.NivelAcesso,
                Permissoes = admin.Permissoes,
                UltimoLogin = admin.UltimoLogin,
                Ativo = admin.Ativo,
                DataCriacao = admin.DataCriacao
            };

            return Ok(resposta);
        }

        /// <summary>
        /// GET: /api/admins
        /// Lista todos os admins ativos
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Listar()
        {
            var admins = await _adminServico.ListarAtivosAsync();

            var resposta = admins.Select(a => new AdminRespostaDto
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
            });

            return Ok(resposta);
        }

        /// <summary>
        /// PUT: /api/admins/{id}
        /// Atualiza dados do admin (nível de acesso, permissões, etc)
        /// Body: AdminCriarDto
        /// </summary>
        [HttpPut("{id:guid}")]
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

            var resposta = new AdminRespostaDto
            {
                Id = admin.Id,
                NomeUsuario = admin.NomeUsuario,
                Email = admin.Email,
                Telefone = admin.Telefone,
                Tipo = (int)admin.Tipo,
                NivelAcesso = admin.NivelAcesso,
                Permissoes = admin.Permissoes,
                UltimoLogin = admin.UltimoLogin,
                Ativo = admin.Ativo,
                DataCriacao = admin.DataCriacao
            };

            return Ok(resposta);
        }

        /// <summary>
        /// PUT: /api/admins/{id}/senha
        /// Altera a senha do admin
        /// Body: { "senhaAtual": "123456", "novaSenha": "654321" }
        /// </summary>
        [HttpPut("{id:guid}/senha")]
        public async Task<IActionResult> AlterarSenha(Guid id, [FromBody] AlterarSenhaDto dto)
        {
            await _adminServico.AlterarSenhaAsync(id, dto.SenhaAtual, dto.NovaSenha);

            return Ok(new { mensagem = "Senha alterada com sucesso" });
        }

        /// <summary>
        /// DELETE: /api/admins/{id}
        /// Remove/desativa um admin
        /// </summary>
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Remover(Guid id)
        {
            await _adminServico.RemoverAsync(id);
            return NoContent();
        }
    }
}
