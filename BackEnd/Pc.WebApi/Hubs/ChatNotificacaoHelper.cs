using Microsoft.AspNetCore.SignalR;
using Pc.Dominio.Entities.Interacoes;
using Pc.Dominio.Enums;
using Pc.Servico.Interfaces;
using Pc.WebApi.DTOs.Interacoes;

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

        public MensagemRespostaDto ParaDto(Mensagem mensagem) => new()
        {
            CodigoPublico = _idCodificador.Codificar(mensagem.Id),
            CodigoRemetente = _idCodificador.Codificar(mensagem.RemetenteId),
            RemetentePapel = (int)mensagem.RemetentePapel,
            Texto = mensagem.Texto,
            EnviadaEm = mensagem.EnviadaEm,
            RecebidaEm = mensagem.RecebidaEm,
            Lida = mensagem.Lida
        };

        public async Task NotificarMensagemEnviadaAsync(Conversa conversa, Mensagem mensagem)
        {
            var dto = ParaDto(mensagem);
            var conversaCodigo = _idCodificador.Codificar(conversa.Id);

            await _hubContext.Clients
                .Group(conversaCodigo)
                .SendAsync("ReceberMensagem", dto);

            await NotificarNovaMensagemAsync(conversa, mensagem.RemetentePapel, mensagem.EnviadaEm);
        }

        private async Task NotificarNovaMensagemAsync(
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
