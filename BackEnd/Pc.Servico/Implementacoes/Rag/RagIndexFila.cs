using System.Collections.Concurrent;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Pc.Dominio.Enums;
using Pc.Servico.Interfaces;
using Pc.Servico.Modelos.Rag;

namespace Pc.Servico.Implementacoes.Rag
{
    /// <summary>
    /// Fila in-process com coalescência: atualizações rápidas da mesma entidade
    /// colapsam em um único evento pendente.
    /// </summary>
    public sealed class RagIndexFila : IRagIndexFila
    {
        private readonly Channel<RagIndexEvento> _channel;
        private readonly ConcurrentDictionary<string, byte> _pendentes = new();
        private readonly ILogger<RagIndexFila> _logger;

        public RagIndexFila(ILogger<RagIndexFila> logger)
        {
            _logger = logger;
            _channel = Channel.CreateUnbounded<RagIndexEvento>(new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false
            });
        }

        public ChannelReader<RagIndexEvento> Reader => _channel.Reader;

        public void Enfileirar(RagDocumentoTipo tipo, Guid entidadeId, RagIndexAcao acao = RagIndexAcao.Indexar)
        {
            var key = $"{(int)tipo}:{entidadeId}:{(int)acao}";
            if (!_pendentes.TryAdd(key, 0))
            {
                _logger.LogDebug("RAG evento coalescido. Tipo={Tipo} Id={Id} Acao={Acao}", tipo, entidadeId, acao);
                return;
            }

            var ok = _channel.Writer.TryWrite(new RagIndexEvento
            {
                Tipo = tipo,
                EntidadeId = entidadeId,
                Acao = acao
            });

            if (!ok)
            {
                _pendentes.TryRemove(key, out _);
                _logger.LogWarning("RAG fila rejeitou evento. Tipo={Tipo} Id={Id}", tipo, entidadeId);
                return;
            }

            _logger.LogInformation("RAG evento enfileirado. Tipo={Tipo} Id={Id} Acao={Acao}", tipo, entidadeId, acao);
        }

        public void Liberar(RagIndexEvento evento)
        {
            var key = $"{(int)evento.Tipo}:{evento.EntidadeId}:{(int)evento.Acao}";
            _pendentes.TryRemove(key, out _);
        }
    }
}
