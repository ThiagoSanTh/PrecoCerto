using System.Text.RegularExpressions;
using Pc.Dominio.Enums;

namespace Pc.Servico.Implementacoes.IA
{
    public sealed class IAEntidadesExtraidas
    {
        public string? Produto { get; init; }
        public string? Loja { get; init; }
    }

    /// <summary>
    /// Extração simples de entidades por padrões linguísticos e remoção de stopwords.
    /// </summary>
    public static class IAEntidadeExtrator
    {
        private static readonly HashSet<string> Stopwords = new(StringComparer.Ordinal)
        {
            "a", "o", "os", "as", "um", "uma", "uns", "umas", "de", "da", "do", "das", "dos",
            "em", "no", "na", "nos", "nas", "por", "para", "com", "sem", "que", "qual", "quais",
            "onde", "quando", "como", "tem", "têm", "esta", "está", "esse", "essa", "este", "esta",
            "desse", "dessa", "deste", "desta", "meu", "minha", "mim", "eu", "voce", "você",
            "disponivel", "disponível", "perto", "longe", "aqui", "la", "lá", "mais", "barato",
            "vende", "vendem", "encontrar", "encontro", "acho", "achar", "loja", "lojas",
            "produto", "produtos", "mercado", "promocao", "promoção", "oferta", "ofertas",
            "preco", "preço", "custa", "quanto", "quantos", "quilometros", "quilômetros", "km",
            "distancia", "distância", "fica", "existe", "cadastrada", "cadastrado", "ainda",
            "algum", "alguma", "sobre", "ate", "até", "sao", "são", "ser", "foi", "e", "ou",
            "me", "te", "se", "lhe", "nos", "vos", "eles", "elas", "isso", "isto", "aquilo",
            "hoje", "agora", "ja", "já", "nao", "não", "sim", "por", "favor", "pf", "pfv"
        };

        private static readonly Regex LojaAposArtigo = new(
            @"\b(?:loja|mercado|supermercado)\s+([a-záàâãéêíóôõúç0-9][\w\s\-']{1,60})",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

        private static readonly Regex LojaNomeGenerico = new(
            @"\b((?:mercado|supermercado)\s+[a-záàâãéêíóôõúç0-9][\w\s\-']{0,40})",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

        public static IAEntidadesExtraidas Extrair(string mensagem, IAIntencao intencao)
        {
            if (string.IsNullOrWhiteSpace(mensagem))
                return new IAEntidadesExtraidas();

            string? loja = ExtrairLoja(mensagem);
            string? produto = null;

            if (intencao is IAIntencao.DistanciaLoja or IAIntencao.InformacaoLoja or IAIntencao.ProdutosDaLoja)
            {
                produto = null;
            }
            else
            {
                produto = ExtrairProduto(mensagem, loja);
            }

            // Se intenção de loja e não achou padrão, tenta resto como nome
            if (loja is null && intencao is IAIntencao.DistanciaLoja or IAIntencao.InformacaoLoja or IAIntencao.ProdutosDaLoja)
            {
                loja = ExtrairTokensRestantes(mensagem);
            }

            return new IAEntidadesExtraidas
            {
                Produto = string.IsNullOrWhiteSpace(produto) ? null : produto.Trim(),
                Loja = string.IsNullOrWhiteSpace(loja) ? null : loja.Trim()
            };
        }

        private static string? ExtrairLoja(string mensagem)
        {
            var m = LojaAposArtigo.Match(mensagem);
            if (m.Success)
            {
                var nome = LimparNomeLoja(m.Groups[1].Value);
                if (!string.IsNullOrWhiteSpace(nome))
                    return nome;
            }

            m = LojaNomeGenerico.Match(mensagem);
            if (m.Success)
            {
                var nome = LimparNomeLoja(m.Groups[1].Value);
                if (!string.IsNullOrWhiteSpace(nome))
                    return nome;
            }

            return null;
        }

        private static string LimparNomeLoja(string valor)
        {
            // Mantém palavras como "Mercado"/"Supermercado" que fazem parte do nome fantasia.
            var limpo = Regex.Replace(valor, @"[?\.,!;:]+$", "").Trim();
            var stopFinais = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "fica", "esta", "está", "existe", "tem", "a", "o", "de", "da", "do",
                "quantos", "quilometros", "quilômetros", "km", "longe", "perto", "mim", "voce", "você"
            };
            var partes = limpo.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(p => !stopFinais.Contains(p))
                .ToArray();
            return string.Join(' ', partes);
        }

        private static string? ExtrairProduto(string mensagem, string? lojaJaExtraida)
        {
            var texto = mensagem;
            if (!string.IsNullOrWhiteSpace(lojaJaExtraida))
                texto = Regex.Replace(texto, Regex.Escape(lojaJaExtraida), " ", RegexOptions.IgnoreCase);

            // Padrões: "tem X", "vende X", "preço do X", "promoção de X"
            var padroes = new[]
            {
                @"\btem\s+(?:o\s+|a\s+|um\s+|uma\s+)?([a-záàâãéêíóôõúç][\w\s\-]{1,40}?)(?:\s+(?:disponivel|disponível|perto|barato|na|no|em|da|do|dessa|desse|\?|$))",
                @"\b(?:vende|vendem|encontro|encontrar|achar)\s+(?:o\s+|a\s+|um\s+|uma\s+)?([a-záàâãéêíóôõúç][\w\s\-]{1,40}?)(?:\s+(?:perto|barato|na|no|em|\?|$))",
                @"\b(?:preço|preco|promoção|promocao|oferta)\s+(?:do|da|de|d[eo]\s+)?([a-záàâãéêíóôõúç][\w\s\-]{1,40}?)(?:\s|\?|$)",
                @"\bquanto\s+custa\s+(?:o\s+|a\s+)?([a-záàâãéêíóôõúç][\w\s\-]{1,40}?)(?:\s|\?|$)",
                @"\bcategoria\s+(?:do|da|de)\s+([a-záàâãéêíóôõúç][\w\s\-]{1,40}?)(?:\s|\?|$)"
            };

            foreach (var p in padroes)
            {
                var m = Regex.Match(texto, p, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                if (m.Success)
                {
                    var cand = LimparEntidade(m.Groups[1].Value);
                    if (!string.IsNullOrWhiteSpace(cand) && !Stopwords.Contains(TextoNormalizador.Normalizar(cand)))
                        return cand;
                }
            }

            return ExtrairTokensRestantes(texto);
        }

        private static string? ExtrairTokensRestantes(string mensagem)
        {
            var tokens = Regex.Split(mensagem, @"[^\p{L}\p{N}]+")
                .Where(t => t.Length >= 2)
                .Select(t => t.Trim())
                .Where(t => !Stopwords.Contains(TextoNormalizador.Normalizar(t)))
                .ToList();

            if (tokens.Count == 0)
                return null;

            // Preferência: junta até 3 tokens significativos
            return string.Join(' ', tokens.Take(3));
        }

        private static string LimparEntidade(string valor)
        {
            var limpo = Regex.Replace(valor, @"[?\.,!;:]+$", "").Trim();
            var partes = limpo.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(p => !Stopwords.Contains(TextoNormalizador.Normalizar(p)))
                .ToArray();
            return string.Join(' ', partes);
        }
    }
}
