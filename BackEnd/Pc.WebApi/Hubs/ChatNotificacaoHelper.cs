using Microsoft.AspNetCore.SignalR;
using Pc.Dominio.Entities.Interacoes;
using Pc.Dominio.Enums;
using Pc.Servico.Interfaces;

namespace Pc.WebApi.Hubs
{
    public class ChatNotificacaoHelper
    {
        private readonly IHubContext<ChatHub> _hubContext;
        private readonly IIdCodificador _idCodificador;

        public ChatNotificacaoHelper(IHubContext<ChatHub> hubContext, IIdCodificador idCodificador)
        {
            _hubContext = hubContext;
            _idCodificador = idCodificador;
        }

        public async Task NotificarNovaMensagemAsync(
            Conversa conversa, PapelUsuario remetentePapel, DateTime enviadaEm)
        {
            var conversaCodigo = _idCodificador.Codificar(conversa.Id);
            var payload = new
            {
                conversaCodigo,
                enviadaEm,
                incremento = 1
            };

            if (remetentePapel == PapelUsuario.Cliente)
            {
                await _hubContext.Clients
                    .Group($"loja:{conversa.LojaId}")
                    .SendAsync("NovaMensagemNaoLida", payload);
                return;
            }

            await _hubContext.Clients
                .Group($"cliente:{conversa.ClienteId}")
                .SendAsync("NovaMensagemNaoLida", payload);
        }
    }
}
