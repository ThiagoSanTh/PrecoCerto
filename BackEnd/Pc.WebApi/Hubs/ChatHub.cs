using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Pc.Dominio.Enums;
using Pc.Servico.Interfaces;
using Pc.WebApi.DTOs.Interacoes;
using Pc.WebApi.Extensions;

namespace Pc.WebApi.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly IConversaServico _conversaServico;
        private readonly IIdCodificador _idCodificador;

        public ChatHub(IConversaServico conversaServico, IIdCodificador idCodificador)
        {
            _conversaServico = conversaServico;
            _idCodificador = idCodificador;
        }

        public async Task EnviarMensagem(string conversaCodigo, string texto)
        {
            var userId = Context.User?.GetUserId();
            if (!userId.HasValue || !_idCodificador.TentarDecodificar(conversaCodigo, out var conversaId))
                throw new HubException("Conversa inválida.");

            var papel = ObterPapel(Context.User!);
            var mensagem = await _conversaServico.EnviarMensagemAsync(
                conversaId, userId.Value, papel, texto);

            var dto = new MensagemRespostaDto
            {
                CodigoPublico = _idCodificador.Codificar(mensagem.Id),
                CodigoRemetente = _idCodificador.Codificar(mensagem.RemetenteId),
                RemetentePapel = (int)mensagem.RemetentePapel,
                Texto = mensagem.Texto,
                EnviadaEm = mensagem.EnviadaEm,
                Lida = mensagem.Lida
            };

            await Clients.Group(conversaCodigo).SendAsync("ReceberMensagem", dto);
        }

        public async Task EntrarConversa(string conversaCodigo)
        {
            var userId = Context.User?.GetUserId();
            if (!userId.HasValue || !_idCodificador.TentarDecodificar(conversaCodigo, out var conversaId))
                throw new HubException("Conversa inválida.");

            var pode = await _conversaServico.UsuarioPodeAcessarAsync(
                conversaId, userId.Value, ObterPapel(Context.User!), Context.User?.GetLojaId());

            if (!pode)
                throw new HubException("Acesso negado.");

            await Groups.AddToGroupAsync(Context.ConnectionId, conversaCodigo);
        }

        private static PapelUsuario ObterPapel(System.Security.Claims.ClaimsPrincipal user)
        {
            if (user.IsLojista()) return PapelUsuario.Lojista;
            if (user.IsVendedor()) return PapelUsuario.Vendedor;
            return PapelUsuario.Cliente;
        }
    }
}
