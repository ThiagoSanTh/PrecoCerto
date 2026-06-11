using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pc.Dominio.Entities.Usuarios;
using Pc.Dominio.Enums;
using Pc.Servico.Interfaces;
using Pc.WebApi.Authorization;
using Pc.WebApi.DTOs.Comum;
using Pc.WebApi.DTOs.Usuarios;
using Pc.WebApi.Extensions;
using Pc.WebApi.Services;

namespace Pc.WebApi.Controllers
{
    /// <summary>
    /// Operações de perfil de lojista e gestão de vendedores.
    /// No modelo unificado, lojista e vendedor são <see cref="Usuario"/> com papéis distintos.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class LojistasController : ControllerBase
    {
        private readonly IClienteServico _usuarioServico;
        private readonly IIdCodificador _idCodificador;

        public LojistasController(IClienteServico usuarioServico, IIdCodificador idCodificador)
        {
            _usuarioServico = usuarioServico;
            _idCodificador = idCodificador;
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> ObterPorId(Guid id)
        {
            var denied = Authz.ForbidUnlessSelfOrAdmin(this, id);
            if (denied != null) return denied;

            var usuario = await _usuarioServico.ObterComLojaAsync(id);
            if (usuario == null)
                return NotFound("Usuário não encontrado.");

            return Ok(MapResposta(usuario));
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Atualizar(Guid id, [FromBody] LojistaCriarDto dto)
        {
            var denied = Authz.ForbidUnlessSelfOrAdmin(this, id);
            if (denied != null) return denied;

            var usuario = await _usuarioServico.ObterComLojaAsync(id);
            if (usuario == null)
                return NotFound("Usuário não encontrado.");

            usuario.NomeUsuario = dto.NomeUsuario;
            usuario.Email = dto.Email;
            usuario.Telefone = dto.Telefone;
            usuario.Cargo = dto.Cargo;

            await _usuarioServico.AtualizarAsync(usuario);
            return Ok(MapResposta(usuario));
        }

        [HttpPut("{id:guid}/senha")]
        public async Task<IActionResult> AlterarSenha(Guid id, [FromBody] AlterarSenhaDto dto)
        {
            var denied = Authz.ForbidUnlessSelfOrAdmin(this, id);
            if (denied != null) return denied;

            await _usuarioServico.AlterarSenhaAsync(id, dto.SenhaAtual, dto.NovaSenha);
            return Ok(new { mensagem = "Senha alterada com sucesso" });
        }

        /// <summary>Lista os vendedores vinculados a uma loja.</summary>
        [HttpGet("loja/{lojaId:guid}/vendedores")]
        [Authorize(Roles = "Admin,Lojista")]
        public async Task<IActionResult> ListarVendedores(Guid lojaId)
        {
            if (!Authz.OwnsLoja(this, lojaId))
                return Forbid();

            var vendedores = await _usuarioServico.ListarVendedoresPorLojaAsync(lojaId);
            return Ok(vendedores.Select(MapResposta));
        }

        /// <summary>Promove um cliente a vendedor da loja (controle de estoque).</summary>
        [HttpPost("loja/{lojaId:guid}/vendedores")]
        [Authorize(Roles = "Admin,Lojista")]
        public async Task<IActionResult> PromoverVendedor(Guid lojaId, [FromBody] PromoverVendedorDto dto)
        {
            if (!Authz.OwnsLoja(this, lojaId))
                return Forbid();

            Usuario? alvo = null;
            if (dto.UsuarioId.HasValue && dto.UsuarioId.Value != Guid.Empty)
                alvo = await _usuarioServico.ObterPorIdAsync(dto.UsuarioId.Value);
            else if (!string.IsNullOrWhiteSpace(dto.Email))
                alvo = await _usuarioServico.ObterPorEmailAsync(dto.Email);

            if (alvo == null)
                return NotFound("Usuário a promover não encontrado.");

            try
            {
                await _usuarioServico.PromoverParaVendedorAsync(alvo.Id, lojaId, dto.Cargo);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

            var atualizado = await _usuarioServico.ObterPorIdAsync(alvo.Id);
            return Ok(MapResposta(atualizado!));
        }

        /// <summary>Remove o vínculo de vendedor (volta a ser Cliente).</summary>
        [HttpDelete("loja/{lojaId:guid}/vendedores/{usuarioId:guid}")]
        [Authorize(Roles = "Admin,Lojista")]
        public async Task<IActionResult> RemoverVendedor(Guid lojaId, Guid usuarioId)
        {
            if (!Authz.OwnsLoja(this, lojaId))
                return Forbid();

            try
            {
                await _usuarioServico.RemoverVendedorAsync(usuarioId);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

            return NoContent();
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Listar()
        {
            var todos = await _usuarioServico.ListarAtivosAsync();
            var lojistas = todos.Where(u => u.Papel == PapelUsuario.Lojista || u.Papel == PapelUsuario.Vendedor);
            return Ok(lojistas.Select(MapResposta));
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Remover(Guid id)
        {
            await _usuarioServico.RemoverAsync(id);
            return NoContent();
        }

        private LojistaRespostaDto MapResposta(Usuario u) => new()
        {
            Id = u.Id,
            CodigoPublico = _idCodificador.Codificar(u.Id),
            NomeUsuario = u.NomeUsuario,
            Email = u.Email,
            Telefone = u.Telefone,
            Tipo = (int)u.Tipo,
            Papel = (int)u.Papel,
            UltimoLogin = u.UltimoLogin,
            LojaId = u.Papel == PapelUsuario.Vendedor ? u.LojaVinculadaId : u.LojaPropria?.Id,
            NomeLoja = u.LojaPropria?.NomeFantasia ?? string.Empty,
            Cargo = u.Cargo,
            Ativo = u.Ativo,
            DataCriacao = u.DataCriacao
        };
    }
}
