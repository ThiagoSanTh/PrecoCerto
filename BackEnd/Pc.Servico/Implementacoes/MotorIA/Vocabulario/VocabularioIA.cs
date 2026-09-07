using System.Text.RegularExpressions;
using Pc.Servico.Implementacoes.IA;

namespace Pc.Servico.Implementacoes.MotorIA.Vocabulario
{
    /// <summary>
    /// Vocabulário controlado e expansível (PT-BR) para o MotorIA v2.
    /// </summary>
    public static class VocabularioIA
    {
        public static readonly string[] PrecoBaixo =
        {
            "barato", "barata", "em conta", "economico", "econômica", "economica",
            "gastando pouco", "menor preco", "menor preço", "mais barato", "mais barata",
            "preco baixo", "preço baixo", "economizar", "economia"
        };

        public static readonly string[] DistanciaBaixa =
        {
            "perto", "perto de mim", "proximo", "próximo", "proxima", "próxima",
            "aqui perto", "nas redondezas", "nao quero ir longe", "não quero ir longe",
            "mais perto", "mais proximo", "mais próximo", "vizinho", "perto daqui"
        };

        public static readonly string[] Urgencia =
        {
            "agora", "rapido", "rápido", "urgente", "urgencia", "urgência",
            "preciso logo", "com urgencia", "com urgência", "imediatamente", "hoje"
        };

        public static readonly string[] Entrega =
        {
            "entregar", "delivery", "entrega", "nao quero sair", "não quero sair",
            "sem sair", "em casa", "recebo em casa"
        };

        public static readonly string[] Promocao =
        {
            "promocao", "promoção", "oferta", "desconto", "em promocao", "em promoção"
        };

        public static readonly string[] Qualidade =
        {
            "melhor", "qualidade", "bem avaliado", "bem avaliada", "nota alta", "recomendado"
        };

        public static readonly string[] ForaDominio =
        {
            "presidente", "neymar", "piada", "receita", "como fazer", "capital da",
            "capital do", "previsao do tempo", "previsão do tempo", "futebol", "chatgpt",
            "conte uma historia", "conte uma história", "traduza", "poema"
        };

        public static readonly string[] Saudacoes =
        {
            "ola", "olá", "oi", "oie", "eae", "eai", "hey", "hello", "hi",
            "bom dia", "boa tarde", "boa noite", "boa madrugada",
            "tudo bem", "tudo bom", "como vai", "como voce esta", "como você está"
        };

        /// <summary>
        /// Sinônimos / variantes ortográficas para ampliar busca SQL e RAG.
        /// Chave e valores são normalizados (sem acento) na expansão.
        /// </summary>
        public static readonly Dictionary<string, string[]> SinonimosProduto =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["coca"] = new[] { "coca", "coca cola", "coca-cola", "cola", "refrigerante" },
                ["camera"] = new[] { "camera", "câmera", "fotografica", "fotográfica" },
                ["brinco"] = new[] { "brinco", "brincos" },
                ["arroz"] = new[] { "arroz" },
                ["feijao"] = new[] { "feijao", "feijão" },
                ["leite"] = new[] { "leite" },
                ["sabonete"] = new[] { "sabonete" },
                ["cafe"] = new[] { "cafe", "café" },
                ["pao"] = new[] { "pao", "pão" },
                ["refrigerante"] = new[] { "refrigerante", "refri", "coca", "cola" }
            };

        public static readonly Dictionary<string, string> SinonimosCategoria = new(StringComparer.OrdinalIgnoreCase)
        {
            ["limpeza"] = "Limpeza",
            ["higiene"] = "HigienePessoal",
            ["bebida"] = "Bebidas",
            ["bebidas"] = "Bebidas",
            ["alimento"] = "Alimentos",
            ["alimentos"] = "Alimentos",
            ["padaria"] = "Padaria",
            ["acougue"] = "Acougue",
            ["açougue"] = "Acougue",
            ["hortifruti"] = "Hortifruti",
            ["farmacia"] = "Farmacia",
            ["farmácia"] = "Farmacia"
        };

        public static bool ContemAlgum(string textoNormalizado, IEnumerable<string> termos) =>
            termos.Any(t => textoNormalizado.Contains(TextoNormalizador.Normalizar(t), StringComparison.Ordinal));

        public static bool ContemSinonimoProduto(string textoNormalizado) =>
            SinonimosProduto.Keys.Any(k =>
                Regex.IsMatch(textoNormalizado, $@"\b{Regex.Escape(TextoNormalizador.Normalizar(k))}\b",
                    RegexOptions.IgnoreCase | RegexOptions.CultureInvariant));

        /// <summary>
        /// Expande um termo de produto em tokens de busca (termo + sinônimos).
        /// </summary>
        public static IReadOnlyList<string> ExpandirTermosBusca(string? produtoTermo)
        {
            if (string.IsNullOrWhiteSpace(produtoTermo))
                return Array.Empty<string>();

            var baseTokens = Regex.Split(produtoTermo, @"[^\p{L}\p{N}]+")
                .Where(t => t.Length >= 2)
                .Select(TextoNormalizador.Normalizar)
                .Distinct(StringComparer.Ordinal)
                .ToList();

            var expandido = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var token in baseTokens)
            {
                expandido.Add(token);
                foreach (var kv in SinonimosProduto)
                {
                    var chave = TextoNormalizador.Normalizar(kv.Key);
                    if (token == chave || token.Contains(chave, StringComparison.Ordinal))
                    {
                        foreach (var s in kv.Value)
                        {
                            if (string.IsNullOrWhiteSpace(s) || s.Trim().Length < 2)
                                continue;
                            // Mantém forma com acento (ILIKE no Postgres) + forma normalizada.
                            expandido.Add(s.Trim());
                            var sn = TextoNormalizador.Normalizar(s);
                            if (sn.Length >= 2)
                                expandido.Add(sn);
                        }
                    }
                }
            }

            // Mantém o termo completo se for multi-palavra útil (ex.: "coca cola").
            var completo = TextoNormalizador.Normalizar(produtoTermo.Trim());
            if (completo.Length >= 2 && completo.Contains(' '))
                expandido.Add(completo);

            return expandido.OrderByDescending(t => t.Length).ToList();
        }

        /// <summary>
        /// Consultas preferenciais para RAG: termo limpo, depois mensagem original curta.
        /// </summary>
        public static IReadOnlyList<string> MontarConsultasRag(string? produtoTermo, string? mensagemOriginal)
        {
            var lista = new List<string>();
            if (!string.IsNullOrWhiteSpace(produtoTermo))
                lista.Add(produtoTermo.Trim());

            foreach (var t in ExpandirTermosBusca(produtoTermo).Take(3))
            {
                if (!lista.Contains(t, StringComparer.OrdinalIgnoreCase))
                    lista.Add(t);
            }

            if (!string.IsNullOrWhiteSpace(mensagemOriginal))
            {
                var msg = mensagemOriginal.Trim();
                if (msg.Length <= 120 && !lista.Contains(msg, StringComparer.OrdinalIgnoreCase))
                    lista.Add(msg);
            }

            return lista;
        }
    }
}
