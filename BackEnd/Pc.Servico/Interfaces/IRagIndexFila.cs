using System.Threading.Channels;
using Pc.Dominio.Enums;
using Pc.Servico.Modelos.Rag;

namespace Pc.Servico.Interfaces
{
    public interface IRagIndexFila
    {
        void Enfileirar(RagDocumentoTipo tipo, Guid entidadeId, RagIndexAcao acao = RagIndexAcao.Indexar);
        void Liberar(RagIndexEvento evento);
        ChannelReader<RagIndexEvento> Reader { get; }
    }
}
