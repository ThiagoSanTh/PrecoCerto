using Pc.Servico.Implementacoes.IA;
using Pc.Servico.Interfaces.MotorIA;
using Pc.Servico.Modelos.MotorIA;

namespace Pc.Servico.Implementacoes.MotorIA
{
    public class InterpretadorIA : IInterpretadorIA
    {
        private readonly IClassificadorIntencaoIA _classificador;
        private readonly IExtratorEntidadesIA _extrator;

        public InterpretadorIA(IClassificadorIntencaoIA classificador, IExtratorEntidadesIA extrator)
        {
            _classificador = classificador;
            _extrator = extrator;
        }

        public ContextoIA Interpretar(PedidoAnaliseIA pedido)
        {
            var mensagem = pedido.Mensagem?.Trim() ?? string.Empty;
            var ctx = new ContextoIA
            {
                MensagemOriginal = mensagem,
                MensagemNormalizada = TextoNormalizador.Normalizar(mensagem),
                Latitude = pedido.Latitude,
                Longitude = pedido.Longitude,
                UsuarioId = pedido.UsuarioId,
                Pesos = PesosIA.Padrao()
            };

            _classificador.Classificar(ctx);
            _extrator.Extrair(ctx);
            return ctx;
        }
    }
}
