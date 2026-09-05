using Pc.Dominio.Enums.MotorIA;
using Pc.Servico.Implementacoes.IA;
using Pc.Servico.Implementacoes.MotorIA.Vocabulario;
using Pc.Servico.Interfaces.MotorIA;
using Pc.Servico.Modelos.MotorIA;

namespace Pc.Servico.Implementacoes.MotorIA
{
    public class ClassificadorIntencaoIA : IClassificadorIntencaoIA
    {
        public void Classificar(ContextoIA contexto)
        {
            var t = contexto.MensagemNormalizada;
            if (string.IsNullOrWhiteSpace(t))
            {
                contexto.Intencao = IntencaoIA.NaoEntendida;
                return;
            }

            if (VocabularioIA.ContemAlgum(t, VocabularioIA.ForaDominio))
            {
                contexto.Intencao = IntencaoIA.ForaDoDominio;
                return;
            }

            if (VocabularioIA.ContemAlgum(t, VocabularioIA.Entrega) &&
                (t.Contains("entrega") || t.Contains("delivery") || t.Contains("entregar")
                 || t.Contains("nao quero sair") || t.Contains("não quero sair")))
            {
                contexto.Intencao = IntencaoIA.ConsultarEntrega;
                return;
            }

            if (t.Contains("cesta") || t.Contains("lista de compra") || t.Contains("montar compra"))
            {
                contexto.Intencao = IntencaoIA.MontarCesta;
                return;
            }

            if (t.Contains("mais barato") || t.Contains("mais barata") || t.Contains("menor preco") || t.Contains("menor preço"))
            {
                contexto.Intencao = IntencaoIA.BuscarProdutoMaisBarato;
                return;
            }

            if (t.Contains("mais perto") || t.Contains("mais proxima") || t.Contains("mais próxima") || t.Contains("loja mais proxima") || t.Contains("loja mais próxima"))
            {
                contexto.Intencao = IntencaoIA.BuscarLojaMaisProxima;
                return;
            }

            if (t.Contains("compar") || t.Contains("vale mais a pena") || t.Contains("melhor opcao") || t.Contains("melhor opção"))
            {
                contexto.Intencao = IntencaoIA.CompararPrecos;
                return;
            }

            if (VocabularioIA.ContemAlgum(t, VocabularioIA.Promocao))
            {
                contexto.Intencao = IntencaoIA.BuscarPromocao;
                return;
            }

            if (t.Contains("relacionad") || t.Contains("parecid") || t.Contains("semelhante"))
            {
                contexto.Intencao = IntencaoIA.BuscarProdutosRelacionados;
                return;
            }

            if (t.Contains("recomenda") || t.Contains("suger") || t.Contains("indica"))
            {
                contexto.Intencao = t.Contains("loja")
                    ? IntencaoIA.RecomendarLoja
                    : IntencaoIA.RecomendarProduto;
                return;
            }

            if (t.Contains("disponivel") || t.Contains("disponível") || t.StartsWith("tem ") || t.Contains(" tem "))
            {
                if (t.Contains("loja") && !ContemProdutoHint(t))
                {
                    contexto.Intencao = IntencaoIA.BuscarLoja;
                    return;
                }

                contexto.Intencao = IntencaoIA.ConsultarDisponibilidade;
                return;
            }

            if (t.Contains("onde") || t.Contains("qual loja") || t.Contains("quais lojas"))
            {
                contexto.Intencao = IntencaoIA.BuscarLoja;
                return;
            }

            if (t.Contains("oferta") || t.Contains("preco") || t.Contains("preço") || t.Contains("custa") || t.Contains("quanto"))
            {
                contexto.Intencao = IntencaoIA.BuscarOferta;
                return;
            }

            if (t.Contains("quero") || t.Contains("preciso") || t.Contains("buscar") || t.Contains("encontrar") || ContemProdutoHint(t))
            {
                contexto.Intencao = IntencaoIA.BuscarProduto;
                return;
            }

            contexto.Intencao = IntencaoIA.NaoEntendida;
        }

        private static bool ContemProdutoHint(string t) =>
            t.Contains("arroz") || t.Contains("feijao") || t.Contains("feijão") ||
            t.Contains("sabonete") || t.Contains("cafe") || t.Contains("café") ||
            t.Contains("leite") || t.Contains("pao") || t.Contains("pão") ||
            t.Contains("produto");
    }
}
