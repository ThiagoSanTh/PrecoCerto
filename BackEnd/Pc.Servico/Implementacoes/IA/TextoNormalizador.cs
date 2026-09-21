using System.Globalization;
using System.Text;

namespace Pc.Servico.Implementacoes.IA
{
    public static class TextoNormalizador
    {
        public static string Normalizar(string? texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return string.Empty;

            var formD = texto.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(formD.Length);
            foreach (var c in formD)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            }

            return sb.ToString().Normalize(NormalizationForm.FormC);
        }

        public static bool Contem(string haystack, string needle)
        {
            var h = Normalizar(haystack);
            var n = Normalizar(needle);
            return n.Length > 0 && h.Contains(n, StringComparison.Ordinal);
        }
    }
}
