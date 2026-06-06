using Microsoft.AspNetCore.Mvc;
using Pc.Dominio.Entities.Usuarios;
using Pc.Servico.Interfaces;
using Pc.WebApi.DTOs.Comum;
using Pc.WebApi.DTOs.Usuarios;

namespace Pc.WebApi.Controllers
{
    /// <summary>
    /// Controller para operações com Lojistas
    /// Endpoints para autenticação, gerenciar perfil e acesso às lojas
    /// Rota base: /api/lojistas
    /// Consolidado - sem intermediário Usuario
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class LojistasController : ControllerBase
    {
        private readonly ILojistaServico _lojistaServico;

        public LojistasController(ILojistaServico lojistaServico)
        {
            _lojistaServico = lojistaServico;
        }

        /// <summary>
        /// POST: /api/lojistas/registrar
        /// Registra um novo lojista (signup)
        /// Body: LojistaCriarDto
        /// </summary>
        [HttpPost("registrar")]
        public async Task<IActionResult> Registrar([FromBody] LojistaCriarDto dto)
        {
            var lojista = new Lojista
            {
                NomeUsuario = dto.NomeUsuario,
                Email = dto.Email,
                SenhaHash = dto.Senha, // TODO: Hash com bcrypt
                Telefone = dto.Telefone,
                LojaId = dto.LojaId,
                Cargo = dto.Cargo
            };

            var novoLojista = await _lojistaServico.RegistrarAsync(lojista);

            var resposta = new LojistaRespostaDto
            {
                Id = novoLojista.Id,
                NomeUsuario = novoLojista.NomeUsuario,
                Email = novoLojista.Email,
                Telefone = novoLojista.Telefone,
                Tipo = (int)novoLojista.Tipo,
                UltimoLogin = novoLojista.UltimoLogin,
                LojaId = novoLojista.Loja?.Id ?? novoLojista.LojaId,
                NomeLoja = novoLojista.Loja?.NomeFantasia ?? string.Empty,
                Cargo = novoLojista.Cargo,
                Ativo = novoLojista.Ativo,
                DataCriacao = novoLojista.DataCriacao
            };

            return CreatedAtAction(nameof(ObterPorId), new { id = resposta.Id }, resposta);
        }

        /// <summary>
        /// POST: /api/lojistas/login
        /// Valida credenciais de login
        /// Body: { "email": "user@email.com", "senha": "123456" }
        /// </summary>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var lojista = await _lojistaServico.ValidarLoginAsync(dto.Email, dto.Senha);

            if (lojista == null)
                return Unauthorized("Email ou senha incorretos.");

            var resposta = new LojistaRespostaDto
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

            return Ok(resposta);
        }

        /// <summary>
        /// GET: /api/lojistas/{id}
        /// Retorna um lojista específico
        /// </summary>
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> ObterPorId(Guid id)
        {
            var lojista = await _lojistaServico.ObterPorIdAsync(id);

            if (lojista == null)
                return NotFound("Lojista não encontrado.");

            var resposta = new LojistaRespostaDto
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

            return Ok(resposta);
        }

        /// <summary>
        /// GET: /api/lojistas/email/{email}
        /// Busca lojista por email
        /// </summary>
        [HttpGet("email/{email}")]
        public async Task<IActionResult> BuscarPorEmail(string email)
        {
            var lojista = await _lojistaServico.ObterPorEmailAsync(email);

            if (lojista == null)
                return NotFound("Lojista não encontrado.");

            var resposta = new LojistaRespostaDto
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

            return Ok(resposta);
        }

        /// <summary>
        /// GET: /api/lojistas/loja/{lojaId}
        /// Lista todos os lojistas de uma loja
        /// Útil para gerenciar múltiplos funcionários
        /// </summary>
        [HttpGet("loja/{lojaId:guid}")]
        public async Task<IActionResult> ListarPorLoja(Guid lojaId)
        {
            var lojistas = await _lojistaServico.ListarPorLojaAsync(lojaId);

            var resposta = lojistas.Select(l => new LojistaRespostaDto
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
            });

            return Ok(resposta);
        }

        /// <summary>
        /// GET: /api/lojistas
        /// Lista todos os lojistas ativos
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Listar()
        {
            var lojistas = await _lojistaServico.ListarAtivosAsync();

            var resposta = lojistas.Select(l => new LojistaRespostaDto
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
            });

            return Ok(resposta);
        }

        /// <summary>
        /// PUT: /api/lojistas/{id}
        /// Atualiza dados do lojista (cargo, etc)
        /// Body: LojistaCriarDto
        /// </summary>
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

            var resposta = new LojistaRespostaDto
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

            return Ok(resposta);
        }

        /// <summary>
        /// PUT: /api/lojistas/{id}/senha
        /// Altera a senha do lojista
        /// Body: { "senhaAtual": "123456", "novaSenha": "654321" }
        /// </summary>
        [HttpPut("{id:guid}/senha")]
        public async Task<IActionResult> AlterarSenha(Guid id, [FromBody] AlterarSenhaDto dto)
        {
            await _lojistaServico.AlterarSenhaAsync(id, dto.SenhaAtual, dto.NovaSenha);

            return Ok(new { mensagem = "Senha alterada com sucesso" });
        }

        /// <summary>
        /// DELETE: /api/lojistas/{id}
        /// Remove/desativa um lojista
        /// </summary>
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Remover(Guid id)
        {
            await _lojistaServico.RemoverAsync(id);
            return NoContent();
        }
    }
}
