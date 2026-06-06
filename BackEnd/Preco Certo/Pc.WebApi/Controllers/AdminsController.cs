using Microsoft.AspNetCore.Mvc;
using Pc.Dominio.Entities.Usuarios;
using Pc.Servico.Interfaces;
using Pc.WebApi.DTOs.Comum;
using Pc.WebApi.DTOs.Usuarios;

namespace Pc.WebApi.Controllers
{
    /// <summary>
    /// Controller para operações com Administradores
    /// Endpoints para autenticação, gerenciar permissões e acesso do sistema
    /// Rota base: /api/admins
    /// Consolidado - sem intermediário Usuario
    /// NOTA: Apenas superadmins podem criar/gerenciar outros admins
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AdminsController : ControllerBase
    {
        private readonly IAdminServico _adminServico;

        public AdminsController(IAdminServico adminServico)
        {
            _adminServico = adminServico;
        }

        /// <summary>
        /// POST: /api/admins/registrar
        /// Registra um novo administrador (apenas superadmins)
        /// Body: AdminCriarDto
        /// TODO: Implementar verificação de permissão de superadmin
        /// </summary>
        [HttpPost("registrar")]
        public async Task<IActionResult> Registrar([FromBody] AdminCriarDto dto)
        {
            var admin = new Admin
            {
                NomeUsuario = dto.NomeUsuario,
                Email = dto.Email,
                SenhaHash = dto.Senha, // TODO: Hash com bcrypt
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

        /// <summary>
        /// POST: /api/admins/login
        /// Valida credenciais de login
        /// Body: { "email": "admin@email.com", "senha": "123456" }
        /// </summary>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var admin = await _adminServico.ValidarLoginAsync(dto.Email, dto.Senha);

            if (admin == null)
                return Unauthorized("Email ou senha incorretos.");

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
