using System.Globalization;
using System.Text.RegularExpressions;
using Pc.Dominio.Enums;
using Pc.Dominio.Enums.MotorIA;
using Pc.Servico.Implementacoes.IA;
using Pc.Servico.Implementacoes.MotorIA.Vocabulario;
using Pc.Servico.Interfaces.MotorIA;
using Pc.Servico.Modelos.MotorIA;

namespace Pc.Servico.Implementacoes.MotorIA
{
    public class ExtratorEntidadesIA : IExtratorEntidadesIA
    {
        private static readonly HashSet<string> Stopwords = new(StringComparer.Ordinal)
        {
            "a", "o", "os", "as", "um", "uma", "uns", "umas", "de", "da", "do", "das", "dos", "em", "no", "na",
            "nos", "nas", "eu", "me", "mim", "voce", "você", "voces", "vocês", "vcs",
            "quero", "preciso", "buscar", "encontrar", "comprar", "achar", "acho", "encontro",
            "estou", "estava", "to", "tô", "ta", "tá", "procurando", "procuro", "buscando", "busco",
            "duma", "dum", "numa", "num", "dessa", "desse", "desta", "deste",
            "barato", "barata", "perto", "proximo", "proxima", "agora", "rapido", "urgente",
            "entrega", "delivery", "loja", "lojas", "produto", "produtos", "mercado", "supermercado",
            "mais", "menos", "ate", "até", "com", "sem", "para", "por", "qual", "quais",
            "onde", "aonde", "tem", "nao", "não", "sair", "casa", "fazer", "fazem", "faz",
            "compra", "gastando", "pouco", "vale", "pena", "opcao", "opção", "hoje", "clima", "marte",
            "posso", "pode", "podem", "alguem", "alguém", "algo", "algum", "alguma", "tipo",
            "vendo", "vende", "vendem", "vendendo", "venda", "vendas", "disponivel", "disponível",
            "favor", "pf", "pfv", "quando", "como", "aqui", "la", "lá", "ainda",
            "esse", "essa", "este", "esta", "isso", "isto", "aquele", "aquela",
            "alguma", "algum", "cidade", "redondezas"
        };

        public void Extrair(ContextoIA contexto)
        {
            var original = contexto.MensagemOriginal;
            var t = contexto.MensagemNormalizada;

            if (contexto.Intencao == IntencaoIA.Saudacao)
                return;

            if (VocabularioIA.ContemAlgum(t, VocabularioIA.PrecoBaixo))
            {
                contexto.PreferenciaPreco = NivelPreferenciaIA.Alta;
                contexto.Objetivo ??= ObjetivoIA.Economizar;
            }

            if (VocabularioIA.ContemAlgum(t, VocabularioIA.DistanciaBaixa))
            {
                contexto.PreferenciaDistancia = NivelPreferenciaIA.Alta;
                if (contexto.Objetivo is null or ObjetivoIA.Economizar)
                    contexto.Objetivo = contexto.PreferenciaPreco == NivelPreferenciaIA.Alta
                        ? ObjetivoIA.Economizar
                        : ObjetivoIA.Proximidade;
            }

            if (VocabularioIA.ContemAlgum(t, VocabularioIA.Urgencia))
            {
                contexto.UrgenciaAlta = true;
                contexto.Objetivo ??= ObjetivoIA.Rapidez;
            }

            if (VocabularioIA.ContemAlgum(t, VocabularioIA.Entrega))
            {
                contexto.NecessitaEntrega = true;
                contexto.Objetivo ??= ObjetivoIA.Conveniencia;
            }

            if (VocabularioIA.ContemAlgum(t, VocabularioIA.Promocao))
                contexto.Objetivo ??= ObjetivoIA.Promocao;

            if (VocabularioIA.ContemAlgum(t, VocabularioIA.Qualidade))
            {
                contexto.PreferenciaQualidade = NivelPreferenciaIA.Alta;
                contexto.Objetivo ??= ObjetivoIA.Qualidade;
            }

            ExtrairOrcamento(contexto, t);
            ExtrairDistanciaMax(contexto, t);
            ExtrairPeso(contexto, original);
            ExtrairCategoria(contexto, t);
            ExtrairLoja(contexto, original);
            ExtrairProduto(contexto, t);

            if (contexto.Objetivo is null && contexto.Intencao is IntencaoIA.BuscarProdutoMaisBarato)
                contexto.Objetivo = ObjetivoIA.Economizar;
            if (contexto.Objetivo is null && contexto.Intencao is IntencaoIA.BuscarLojaMaisProxima)
                contexto.Objetivo = ObjetivoIA.Proximidade;
        }

        private static void ExtrairOrcamento(ContextoIA ctx, string t)
        {
            var m = Regex.Match(t, @"ate\s+(\d+(?:[.,]\d+)?)\s*(?:reais|real|r\$)?", RegexOptions.IgnoreCase);
            if (!m.Success)
                m = Regex.Match(t, @"r\$\s*(\d+(?:[.,]\d+)?)", RegexOptions.IgnoreCase);
            if (!m.Success)
                return;

            if (decimal.TryParse(m.Groups[1].Value.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out var valor))
                ctx.OrcamentoMax = valor;
        }

        private static void ExtrairDistanciaMax(ContextoIA ctx, string t)
        {
            var m = Regex.Match(t, @"ate\s+(\d+(?:[.,]\d+)?)\s*km", RegexOptions.IgnoreCase);
            if (!m.Success)
                return;
            if (decimal.TryParse(m.Groups[1].Value.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out var km))
                ctx.DistanciaMaxKm = km;
        }

        private static void ExtrairPeso(ContextoIA ctx, string original)
        {
            var m = Regex.Match(original, @"(\d+(?:[.,]\d+)?)\s*(kg|g|l|ml)\b", RegexOptions.IgnoreCase);
            if (m.Success)
                ctx.PesoOuUnidade = m.Value.Trim();
        }

        private static void ExtrairCategoria(ContextoIA ctx, string t)
        {
            foreach (var kv in VocabularioIA.SinonimosCategoria)
            {
                if (t.Contains(TextoNormalizador.Normalizar(kv.Key), StringComparison.Ordinal)
                    && Enum.TryParse<CategoriaProduto>(kv.Value, out var cat))
                {
                    ctx.Categoria = cat;
                    return;
                }
            }
        }

        private static void ExtrairLoja(ContextoIA ctx, string original)
        {
            var m = Regex.Match(original, @"\b(?:loja|mercado|supermercado)\s+([A-Za-zÀ-ú0-9][\w\s\-]{1,40})", RegexOptions.IgnoreCase);
            if (!m.Success)
                return;
            var nome = m.Groups[1].Value.Trim();
            nome = Regex.Replace(nome, @"[?\.,!;:].*$", "").Trim();
            nome = Limpar(TextoNormalizador.Normalizar(nome));
            if (nome.Length >= 2
                && !VocabularioIA.DistanciaBaixa.Any(s => TextoNormalizador.Normalizar(s).Contains(nome) || nome.Contains(TextoNormalizador.Normalizar(s)))
                && !nome.Equals("mais", StringComparison.Ordinal))
            {
                ctx.LojaTermo = nome;
            }
        }

        private static void ExtrairProduto(ContextoIA ctx, string t)
        {
            var padroes = new[]
            {
                @"\b(?:promocao(?:\s+de)?|oferta(?:\s+de)?)\s+([a-z][\w\s\-]{1,40}?)(?:\s|$|\?|,)",
                @"\b(?:estou\s+)?(?:procurando|buscando|procuro|busco)\s+(?:por\s+)?(?:de\s+|um\s+|uma\s+|o\s+|a\s+)?([a-z][\w\s\-]{1,40}?)(?:\s*[,.]|\s+(?:aonde|onde|mais|barato|perto|agora|com|sem|ate|na|no|em|da|do|promocao|oferta|vendendo|vendo|\?|$))",
                @"\b(?:quero|preciso|buscar|encontrar|achar|comprar|tem|vende|vendem)\s+(?:de\s+|um\s+|uma\s+|o\s+|a\s+)?([a-z][\w\s\-]{1,40}?)(?:\s+(?:barato|perto|agora|com|sem|ate|na|no|em|da|do|promocao|oferta|vendendo|vendo|vende|aonde|onde|\?|$|,))",
                @"\b(?:aonde|onde)\s+(?:eu\s+)?(?:posso|pode|podem)\s+(?:achar|encontrar|comprar)\s+(?:um\s+|uma\s+|o\s+|a\s+)?([a-z][\w\s\-]{1,40}?)(?:\s+(?:barato|perto|agora|vendendo|vendo|\?|$|,))"
            };

            foreach (var p in padroes)
            {
                var m = Regex.Match(t, p, RegexOptions.IgnoreCase);
                if (!m.Success) continue;
                var cand = Limpar(m.Groups[1].Value);
                if (string.IsNullOrWhiteSpace(cand)) continue;
                if (VocabularioIA.Promocao.Any(s => TextoNormalizador.Normalizar(s) == TextoNormalizador.Normalizar(cand)))
                    continue;
                ctx.ProdutoTermo = EscolherTermoPrincipal(cand);
                return;
            }

            var tokens = TokensSignificativos(t);
            if (tokens.Count == 0)
                return;

            // Preferência: 1 substantivo (último token útil). Multi-palavra só se parecer marca+produto curto.
            ctx.ProdutoTermo = tokens.Count == 1
                ? tokens[0]
                : tokens[^1];
        }

        private static List<string> TokensSignificativos(string t) =>
            Regex.Split(t, @"[^\p{L}\p{N}]+")
                .Where(x => x.Length >= 3)
                .Select(TextoNormalizador.Normalizar)
                .Where(x => !Stopwords.Contains(x))
                .Where(x => !VocabularioIA.PrecoBaixo.Any(s => TextoNormalizador.Normalizar(s) == x))
                .Where(x => !VocabularioIA.DistanciaBaixa.Any(s => TextoNormalizador.Normalizar(s).Contains(x)))
                .Where(x => !VocabularioIA.Promocao.Any(s => TextoNormalizador.Normalizar(s) == x))
                .Where(x => !VocabularioIA.Saudacoes.Any(s => TextoNormalizador.Normalizar(s) == x || TextoNormalizador.Normalizar(s).Contains(x)))
                .ToList();

        private static string EscolherTermoPrincipal(string cand)
        {
            var partes = cand.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(TextoNormalizador.Normalizar)
                .Where(p => !Stopwords.Contains(p))
                .ToList();
            if (partes.Count == 0)
                return cand.Trim();
            if (partes.Count == 1)
                return partes[0];
            // "camera fotografica nikon" → mantém até 3; "uma coca" já limpo
            return string.Join(' ', partes.Take(3));
        }

        private static string Limpar(string valor)
        {
            var partes = valor.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(p => !Stopwords.Contains(TextoNormalizador.Normalizar(p)))
                .ToArray();
            return string.Join(' ', partes).Trim();
        }
    }
}
