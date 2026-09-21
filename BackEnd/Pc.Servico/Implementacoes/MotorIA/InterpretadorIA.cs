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
                Pesos = PesosIA.Padrao()
            };

            _classificador.Classificar(ctx);
            _extrator.Extrair(ctx);

            // Nome de produto isolado ("coca", "camera") → busca, não "não entendi".
            if (ctx.Intencao is IntencaoIA.NaoEntendida
                && !string.IsNullOrWhiteSpace(ctx.ProdutoTermo))
            {
                ctx.Intencao = IntencaoIA.BuscarProduto;
            }

            ctx.TermosBuscaProduto = VocabularioIA.ExpandirTermosBusca(ctx.ProdutoTermo).ToList();
            if (ctx.TermosBuscaProduto.Count == 0 && !string.IsNullOrWhiteSpace(ctx.ProdutoTermo))
                ctx.TermosBuscaProduto.Add(ctx.ProdutoTermo.Trim());

            // #region agent log
            try
            {
                var payload = System.Text.Json.JsonSerializer.Serialize(new
                {
                    sessionId = "6c7c29",
                    runId = "pre-fix",
                    hypothesisId = "A",
                    location = "InterpretadorIA.cs:Interpretar",
                    message = "pos-interpretacao",
                    data = new
                    {
                        msg = ctx.MensagemOriginal,
                        intencao = ctx.Intencao.ToString(),
                        produto = ctx.ProdutoTermo,
                        termos = ctx.TermosBuscaProduto
                    },
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                });
                System.IO.File.AppendAllText(@"d:\Dev\PrecoCerto\debug-6c7c29.log", payload + "\n");
            }
            catch { /* debug */ }
            // #endregion

            return ctx;
        }
    }
}
