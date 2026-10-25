using Pc.Dominio.Enums.MotorIA;
using Pc.Servico.Implementacoes.IA;
using Pc.Servico.Implementacoes.MotorIA.Vocabulario;
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
                LojaIdUsuario = pedido.LojaIdUsuario,
                ModoOperacional = pedido.ModoOperacional,
                Historico = pedido.Historico ?? new List<MensagemHistoricoIA>(),
                Pesos = PesosIA.Padrao()
            };

            _classificador.Classificar(ctx);
            _extrator.Extrair(ctx);

            // Multi-turn: herda loja ou produto do histórico recente se a pergunta atual for continuação ("e feijao?", "nessa loja").
            if (ctx.Historico.Count > 0)
            {
                if (string.IsNullOrWhiteSpace(ctx.LojaTermo) &&
                    (ctx.MensagemNormalizada.Contains("nessa loja") || ctx.MensagemNormalizada.Contains("nesse mercado") || ctx.MensagemNormalizada.Contains("la tem")))
                {
                    foreach (var h in ctx.Historico.AsEnumerable().Reverse())
                    {
                        var hCtx = new ContextoIA { MensagemOriginal = h.Texto, MensagemNormalizada = TextoNormalizador.Normalizar(h.Texto) };
                        _extrator.Extrair(hCtx);
                        if (!string.IsNullOrWhiteSpace(hCtx.LojaTermo))
                        {
                            ctx.LojaTermo = hCtx.LojaTermo;
                            break;
                        }
                    }
                }
            }

            // Nome de produto isolado ("coca", "camera") → busca, não "não entendi".
            if (ctx.Intencao is IntencaoIA.NaoEntendida
                && !string.IsNullOrWhiteSpace(ctx.ProdutoTermo))
            {
                ctx.Intencao = IntencaoIA.BuscarProduto;
            }

            ctx.TermosBuscaProduto = VocabularioIA.ExpandirTermosBusca(ctx.ProdutoTermo).ToList();
            if (ctx.TermosBuscaProduto.Count == 0 && !string.IsNullOrWhiteSpace(ctx.ProdutoTermo))
                ctx.TermosBuscaProduto.Add(ctx.ProdutoTermo.Trim());

            return ctx;
        }
    }
}
