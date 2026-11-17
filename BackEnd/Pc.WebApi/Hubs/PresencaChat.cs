using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;

namespace Pc.WebApi.Hubs
{
    /// <summary>
    /// Conexões do chat ainda abertas. Revogar tira o usuário dos grupos daquela loja.
    /// </summary>
    public sealed class PresencaChat
    {
        private sealed class Conexao
        {
            public Guid UsuarioId { get; init; }
            public HashSet<Guid> Lojas { get; } = new();
            public List<(Guid LojaId, string Codigo)> Conversas { get; } = new();
        }

        private readonly IHubContext<ChatHub> _hub;
        private readonly ConcurrentDictionary<string, Conexao> _conexoes = new();

        public PresencaChat(IHubContext<ChatHub> hub)
        {
            _hub = hub;
        }

        public void Ligar(string connectionId, Guid usuarioId) =>
            _conexoes[connectionId] = new Conexao { UsuarioId = usuarioId };

        public void Loja(string connectionId, Guid lojaId)
        {
            if (_conexoes.TryGetValue(connectionId, out var conexao))
                lock (conexao) conexao.Lojas.Add(lojaId);
        }

        public void Conversa(string connectionId, Guid lojaId, string codigo)
        {
            if (string.IsNullOrEmpty(codigo) || !_conexoes.TryGetValue(connectionId, out var conexao))
                return;
            lock (conexao) conexao.Conversas.Add((lojaId, codigo));
        }

        public void Desligar(string connectionId) =>
            _conexoes.TryRemove(connectionId, out _);

        public async Task RevogarAsync(Guid usuarioId, Guid lojaId)
        {
            foreach (var (connectionId, conexao) in _conexoes)
            {
                if (conexao.UsuarioId != usuarioId)
                    continue;

                List<string> grupos;
                lock (conexao)
                {
                    grupos = new List<string>();
                    if (conexao.Lojas.Remove(lojaId))
                        grupos.Add($"loja:{lojaId}");
                    for (var i = conexao.Conversas.Count - 1; i >= 0; i--)
                    {
                        if (conexao.Conversas[i].LojaId != lojaId)
                            continue;
                        grupos.Add(conexao.Conversas[i].Codigo);
                        conexao.Conversas.RemoveAt(i);
                    }
                }

                foreach (var grupo in grupos)
                    await _hub.Groups.RemoveFromGroupAsync(connectionId, grupo);
            }
        }
    }
}
