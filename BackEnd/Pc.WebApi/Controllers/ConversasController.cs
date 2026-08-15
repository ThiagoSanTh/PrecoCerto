using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pc.Dominio.Enums;
using Pc.Servico.Interfaces;
using Pc.WebApi.DTOs.Interacoes;
using Pc.WebApi.Extensions;
using Pc.WebApi.Hubs;

namespace Pc.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ConversasController : ControllerBase
    {
        private readonly IConversaServico _conversaServico;
        private readonly IIdCodificador _idCodificador;
        private readonly ChatNotificacaoHelper _notificacao;

        public ConversasController(
            IConversaServico conversaServico,
            IIdCodificador idCodificador,
            ChatNotificacaoHelper notificacao)
        {
            _conversaServico = conversaServico;
            _idCodificador = idCodificador;
            _notificacao = notificacao;
        }

        [HttpGet]
        public async Task<IActionResult> Listar()
        {
            var userId = User.GetUserId();
            if (!userId.HasValue)
                return Unauthorized();

            var papel = ObterPapel();
            var lojaId = User.GetLojaId();
            var conversas = await _conversaServico.ListarDoUsuarioAsync(userId.Value, papel, lojaId);
            var naoLidasPorConversa = await _conversaServico.ContarNaoLidasPorConversasAsync(
                userId.Value, papel, lojaId);

            var resposta = conversas.Select(c => new ConversaRespostaDto
            {
                CodigoPublico = _idCodificador.Codificar(c.Id),
                CodigoLoja = _idCodificador.Codificar(c.LojaId),
                CodigoCliente = _idCodificador.Codificar(c.ClienteId),
                NomeContato = papel == PapelUsuario.Cliente
                    ? c.Loja?.NomeFantasia
                    : c.Cliente?.NomeUsuario,
                UltimaMensagemEm = c.UltimaMensagemEm,
                NaoLidas = naoLidasPorConversa.GetValueOrDefault(c.Id, 0)
            }).ToList();

            return Ok(resposta);
        }

        [HttpGet("nao-lidas")]
        public async Task<IActionResult> ContarNaoLidas()
        {
            var userId = User.GetUserId();
            if (!userId.HasValue)
                return Unauthorized();

            var total = await _conversaServico.ContarNaoLidasAsync(
                userId.Value, ObterPapel(), User.GetLojaId());
            return Ok(new { total });
        }

        [HttpGet("tem-novas")]
        public async Task<IActionResult> TemNovas([FromQuery] DateTime? desde)
        {
            var userId = User.GetUserId();
            if (!userId.HasValue)
                return Unauthorized();

            var temNovas = await _conversaServico.TemNaoLidasAsync(
                userId.Value, ObterPapel(), User.GetLojaId(), desde);
            return Ok(new { temNovas });
        }

        [HttpPost("abrir")]
        [Authorize(Roles = "Cliente")]
        public async Task<IActionResult> Abrir([FromQuery] string lojaCodigo)
        {
            var userId = User.GetUserId();
            if (!userId.HasValue || !_idCodificador.TentarDecodificar(lojaCodigo, out var lojaId))
                return BadRequest(new { message = "Loja inválida." });

            try
            {
                var conversa = await _conversaServico.AbrirComLojaAsync(userId.Value, lojaId);
                return Ok(new { codigoPublico = _idCodificador.Codificar(conversa.Id) });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("{codigo}/mensagens")]
        public async Task<IActionResult> ListarMensagens(
            string codigo,
            [FromQuery] DateTime? apos,
            [FromQuery] DateTime? antes,
            [FromQuery] int pageSize = 50)
        {
            if (!_idCodificador.TentarDecodificar(codigo, out var conversaId))
                return NotFound();

            var userId = User.GetUserId();
            if (!userId.HasValue)
                return Unauthorized();

            try
            {
                var mensagens = await _conversaServico.ListarMensagensAsync(
                    conversaId, userId.Value, ObterPapel(), User.GetLojaId(), apos, antes, pageSize);

                if (apos == null)
                    await _conversaServico.MarcarComoLidasAsync(conversaId, userId.Value);

                return Ok(mensagens.Select(m => new MensagemRespostaDto
                {
                    CodigoPublico = _idCodificador.Codificar(m.Id),
                    CodigoRemetente = _idCodificador.Codificar(m.RemetenteId),
                    RemetentePapel = (int)m.RemetentePapel,
                    Texto = m.Texto,
                    EnviadaEm = m.EnviadaEm,
                    Lida = m.Lida
                }));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        [HttpPost("{codigo}/mensagens")]
        public async Task<IActionResult> Enviar(string codigo, [FromBody] MensagemCriarDto dto)
        {
            if (!_idCodificador.TentarDecodificar(codigo, out var conversaId))
                return NotFound();

            var userId = User.GetUserId();
            if (!userId.HasValue)
                return Unauthorized();

            try
            {
                var mensagem = await _conversaServico.EnviarMensagemAsync(
                    conversaId, userId.Value, ObterPapel(), dto.Texto);

                var conversa = await _conversaServico.ObterPorIdAsync(conversaId);
                if (conversa != null)
                    await _notificacao.NotificarMensagemEnviadaAsync(conversa, mensagem);

                return Ok(_notificacao.ParaDto(mensagem));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        private PapelUsuario ObterPapel()
        {
            if (User.IsLojista()) return PapelUsuario.Lojista;
            if (User.IsVendedor()) return PapelUsuario.Vendedor;
            return PapelUsuario.Cliente;
        }
    }
}
