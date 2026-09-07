using System.Text.RegularExpressions;
using Pc.Dominio.Enums.MotorIA;
using Pc.Servico.Implementacoes.IA;
using Pc.Servico.Implementacoes.MotorIA.Vocabulario;
using Pc.Servico.Interfaces.MotorIA;
using Pc.Servico.Modelos.MotorIA;

namespace Pc.Servico.Implementacoes.MotorIA
{
    public class ClassificadorIntencaoIA : IClassificadorIntencaoIA
    {
        private static readonly Regex TokenOnde = new(
            @"\b(?:aonde|onde)\b",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

        public void Classificar(ContextoIA contexto)
        {
            var t = contexto.MensagemNormalizada;
            if (string.IsNullOrWhiteSpace(t))
            {
                contexto.Intencao = IntencaoIA.NaoEntendida;
                return;
            }

            if (EhSaudacao(t))
            {
                contexto.Intencao = IntencaoIA.Saudacao;
                contexto.NivelConfianca = 1d;
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
                if (t.Contains("loja") && !ContemProdutoHint(t) && !TemVerboBuscaProduto(t))
                {
                    contexto.Intencao = IntencaoIA.BuscarLoja;
                    return;
                }

                contexto.Intencao = IntencaoIA.ConsultarDisponibilidade;
                return;
            }

            // "aonde/onde posso achar X" → produto; "onde fica a loja" → loja.
            if (TokenOnde.IsMatch(t) || t.Contains("qual loja") || t.Contains("quais lojas"))
            {
                if (TemVerboBuscaProduto(t) || ContemProdutoHint(t))
                {
                    contexto.Intencao = IntencaoIA.BuscarProduto;
                    return;
                }

                contexto.Intencao = IntencaoIA.BuscarLoja;
                return;
            }

            if (t.Contains("oferta") || t.Contains("preco") || t.Contains("preço") || t.Contains("custa") || t.Contains("quanto"))
            {
                contexto.Intencao = IntencaoIA.BuscarOferta;
                return;
            }

            if (t.Contains("quero") || t.Contains("preciso") || t.Contains("buscar")
                || t.Contains("encontrar") || t.Contains("achar") || t.Contains("comprar")
                || TemVerboBuscaProduto(t) || ContemProdutoHint(t))
            {
                contexto.Intencao = IntencaoIA.BuscarProduto;
                return;
            }

            contexto.Intencao = IntencaoIA.NaoEntendida;
        }

        private static bool EhSaudacao(string t)
        {
            var limpo = Regex.Replace(t, @"[^\p{L}\p{N}\s]+", " ");
            limpo = Regex.Replace(limpo, @"\s+", " ").Trim();
            if (string.IsNullOrWhiteSpace(limpo))
                return false;

            foreach (var s in VocabularioIA.Saudacoes)
            {
                var sn = TextoNormalizador.Normalizar(s);
                if (limpo == sn)
                    return true;
            }

            // "ola tudo bem?" / "bom dia!" — curto e sem verbo de busca de produto.
            var tokens = limpo.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length > 5 || TemVerboBuscaProduto(limpo) || ContemProdutoHint(limpo))
                return false;

            return VocabularioIA.Saudacoes.Any(s =>
            {
                var sn = TextoNormalizador.Normalizar(s);
                return limpo.StartsWith(sn + " ", StringComparison.Ordinal) || limpo == sn;
            });
        }

        private static bool TemVerboBuscaProduto(string t) =>
            t.Contains("achar") || t.Contains("encontrar") || t.Contains("comprar")
            || t.Contains("buscar") || t.Contains("vende") || t.Contains("vendo")
            || t.Contains("vendendo") || t.Contains("quero") || t.Contains("preciso")
            || t.Contains("procurando") || t.Contains("procuro") || t.Contains("buscando")
            || t.Contains("busco");

        private static bool ContemProdutoHint(string t) =>
            VocabularioIA.ContemSinonimoProduto(t) ||
            t.Contains("arroz") || t.Contains("feijao") || t.Contains("feijão") ||
            t.Contains("sabonete") || t.Contains("cafe") || t.Contains("café") ||
            t.Contains("leite") || t.Contains("pao") || t.Contains("pão") ||
            t.Contains("camera") || t.Contains("câmera") || t.Contains("brinco") ||
            t.Contains("coca") || t.Contains("produto");
    }
}
