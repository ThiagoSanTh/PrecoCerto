using Pc.Servico.Modelos.IA;

namespace Pc.Servico.Interfaces
{
    public interface IIAServico
    {
        Task<IAChatResultado> ProcessarAsync(IAChatPedido pedido, CancellationToken cancellationToken = default);
    }
}
