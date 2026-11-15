using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Pc.Servico.Interfaces.MotorIA;
using Pc.Servico.Modelos.MotorIA;

namespace Pc.Servico.Implementacoes.MotorIA
{
    public class ValidadorRespostaIA : IValidadorRespostaIA
    {
        private const decimal ToleranciaPreco = 0.05m;

        private static readonly Regex RegexPreco = new(
            @"R\$\s*([0-9]+(?:[.,][0-9]+)*)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex RegexEstabelecimento = new(
            @"\b(?:loja|supermercado|hipermercado|mercado|mercadinho|atacad[aã]o|atacado|farm[aá]cia|drogaria|padaria|armaz[eé]m|emp[oó]rio|mercearia|hortifr[uú]ti)\s+(?:d[aeo]s?\s+)?([A-ZÀ-Ý][A-Za-zÀ-ÿ0-9'&-]+(?:\s+(?:d[aeo]s?\s+)?[A-ZÀ-Ý][A-Za-zÀ-ÿ0-9'&-]+){0,3})",
            RegexOptions.Compiled);

        private static readonly HashSet<string> Stopwords = new(StringComparer.Ordinal)
        {
            "com", "para", "por", "das", "dos", "uma", "uns", "umas", "que",
            "mais", "menos", "melhor", "oferta", "ofertas", "preco", "precos",
            "loja", "lojas", "produto", "produtos", "proximo", "proxima"
        };

        public bool Validar(string resposta, ContextoIA contexto)
        {
            if (string.IsNullOrWhiteSpace(resposta))
                return false;

            var ranking = contexto?.Decisao?.Ranking;
            if (ranking is not { Count: > 0 })
                return false;

            if (!ValidarPrecos(resposta, ranking))
                return false;

            if (!ValidarEstabelecimentos(resposta, ranking))
                return false;

            return PossuiSobreposicaoMinima(resposta, ranking);
        }

        private static bool ValidarPrecos(string resposta, IReadOnlyList<CandidatoIA> ranking)
        {
            var matches = RegexPreco.Matches(resposta);
            if (matches.Count == 0)
                return true;

            var precosRanking = ranking
                .Where(c => c.Preco.HasValue)
                .Select(c => c.Preco!.Value)
                .ToList();

            if (precosRanking.Count == 0)
                return false;

            foreach (Match match in matches)
            {
                if (!TryParseValorMonetario(match.Groups[1].Value, out var valor))
                    return false;

                if (!precosRanking.Any(p => Math.Abs(p - valor) <= ToleranciaPreco))
                    return false;
            }

            return true;
        }

        private static bool ValidarEstabelecimentos(string resposta, IReadOnlyList<CandidatoIA> ranking)
        {
            var matches = RegexEstabelecimento.Matches(resposta);
            if (matches.Count == 0)
                return true;

            var lojasNormalizadas = ranking
                .Select(c => Normalizar(c.NomeLoja))
                .Where(l => !string.IsNullOrEmpty(l))
                .ToList();

            if (lojasNormalizadas.Count == 0)
                return false;

            foreach (Match match in matches)
            {
                var citado = Normalizar(match.Groups[1].Value);
                if (string.IsNullOrEmpty(citado))
                    continue;

                var tokensCitado = ExtrairTokens(citado);
                var corresponde = lojasNormalizadas.Any(loja =>
                    loja.Contains(citado, StringComparison.Ordinal) ||
                    citado.Contains(loja, StringComparison.Ordinal) ||
                    (tokensCitado.Count > 0 && tokensCitado.All(t => loja.Contains(t, StringComparison.Ordinal))));

                if (!corresponde)
                    return false;
            }

            return true;
        }

        private static bool PossuiSobreposicaoMinima(string resposta, IReadOnlyList<CandidatoIA> ranking)
        {
            var tokensCandidatos = ranking
                .SelectMany(c => new[] { c.NomeProduto, c.Titulo, c.NomeLoja })
                .SelectMany(texto => ExtrairTokens(Normalizar(texto)))
                .Distinct(StringComparer.Ordinal)
                .ToList();

            if (tokensCandidatos.Count == 0)
                return true;

            var respostaNormalizada = Normalizar(resposta);
            var tokensResposta = new HashSet<string>(ExtrairTokens(respostaNormalizada), StringComparer.Ordinal);

            return tokensCandidatos.Any(t =>
                tokensResposta.Contains(t) ||
                respostaNormalizada.Contains(t, StringComparison.Ordinal));
        }

        private static bool TryParseValorMonetario(string raw, out decimal valor)
        {
            valor = 0m;
            var limpo = raw.Trim().TrimEnd('.', ',');
            if (string.IsNullOrEmpty(limpo))
                return false;

            var temVirgula = limpo.Contains(',');
            var temPonto = limpo.Contains('.');

            string normalizado;
            if (temVirgula && temPonto)
            {
                normalizado = limpo.LastIndexOf(',') > limpo.LastIndexOf('.')
                    ? limpo.Replace(".", string.Empty).Replace(',', '.')
                    : limpo.Replace(",", string.Empty);
            }
            else if (temVirgula)
            {
                normalizado = limpo.Replace(',', '.');
            }
            else if (temPonto && Regex.IsMatch(limpo, @"^\d{1,3}(\.\d{3})+$"))
            {
                normalizado = limpo.Replace(".", string.Empty);
            }
            else
            {
                normalizado = limpo;
            }

            return decimal.TryParse(normalizado, NumberStyles.Number, CultureInfo.InvariantCulture, out valor);
        }

        private static string Normalizar(string? texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return string.Empty;

            var decomposed = texto.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(decomposed.Length);
            foreach (var ch in decomposed)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
                    sb.Append(char.ToLowerInvariant(ch));
            }

            return Regex.Replace(sb.ToString().Normalize(NormalizationForm.FormC), @"[^a-z0-9\s]", " ").Trim();
        }

        private static List<string> ExtrairTokens(string textoNormalizado)
        {
            if (string.IsNullOrWhiteSpace(textoNormalizado))
                return new List<string>();

            return textoNormalizado
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(t => t.Length >= 3 && !Stopwords.Contains(t))
                .ToList();
        }
    }
}
