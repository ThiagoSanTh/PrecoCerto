using Pc.Servico.Modelos.MotorIA;

namespace Pc.Servico.Interfaces.MotorIA
{
    public interface IInterpretadorIA
    {
        ContextoIA Interpretar(PedidoAnaliseIA pedido);
    }

    public interface IClassificadorIntencaoIA
    {
        void Classificar(ContextoIA contexto);
    }

    public interface IExtratorEntidadesIA
    {
        void Extrair(ContextoIA contexto);
    }

    public interface IRegraIA
    {
        string Nome { get; }
        bool Aplica(ContextoIA contexto);
        void Aplicar(ContextoIA contexto);
    }

    public interface IMotorRegrasIA
    {
        void Aplicar(ContextoIA contexto);
    }

    public interface IMotorPontuacaoIA
    {
        void Pontuar(ContextoIA contexto);
    }

    public interface IMotorRecomendacaoIA
    {
        DecisaoIA Recomendar(ContextoIA contexto);
    }

    public interface IGeradorRespostaIA
    {
        string Gerar(ContextoIA contexto);
    }

    public interface IRagConhecimentoIA
    {
        Task<IReadOnlyList<RagHitIA>> BuscarAuxiliarAsync(
            string consulta,
            int limite = 5,
            CancellationToken cancellationToken = default);
    }

    public sealed class RagHitIA
    {
        public string Tipo { get; init; } = string.Empty;
        public Guid EntidadeId { get; init; }
        public string Titulo { get; init; } = string.Empty;
        public string Conteudo { get; init; } = string.Empty;
        public double Score { get; init; }
    }

    public interface IMotorIA
    {
        Task<ResultadoAnaliseIA> AnalisarAsync(
            PedidoAnaliseIA pedido,
            CancellationToken cancellationToken = default);
    }
}
