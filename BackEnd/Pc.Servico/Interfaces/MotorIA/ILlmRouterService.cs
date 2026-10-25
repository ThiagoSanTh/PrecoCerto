using Pc.Servico.Modelos.MotorIA;

namespace Pc.Servico.Interfaces.MotorIA
{
    public interface ILlmRouterService
    {
        Task<LlmInterpretacaoResultado?> InterpretarAsync(
            PedidoAnaliseIA pedido,
            CancellationToken cancellationToken = default);

        Task<(string? Texto, string Provedor)> GerarRespostaAsync(
            ContextoIA contexto,
            CancellationToken cancellationToken = default);

        void ResetarCircuitBreaker();
    }
}
