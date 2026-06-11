using System.Net.Mail;
using System.Text.RegularExpressions;
using DnsClient;

namespace Pc.Dominio.Validacoes
{
    /// <summary>
    /// Valida formato, typos comuns de provedores e existência do domínio (registro MX/A).
    /// </summary>
    public static class EmailValidator
    {
        private static readonly Regex FormatoBasico = new(
            @"^[^\s@]+@[^\s@]+\.[^\s@]{2,}$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly LookupClient Dns = new();

        private static readonly string[] ProvedoresPopulares =
        [
            "gmail.com", "googlemail.com", "hotmail.com", "outlook.com", "live.com",
            "yahoo.com", "yahoo.com.br", "icloud.com", "uol.com.br", "bol.com.br",
            "terra.com.br", "ig.com.br", "globo.com", "proton.me", "protonmail.com"
        ];

        /// <summary>Domínios conhecidos por serem typos frequentes (ex.: gmail → ail).</summary>
        private static readonly HashSet<string> DominiosTypoExplicitos = new(StringComparer.OrdinalIgnoreCase)
        {
            "ail.com", "gmial.com", "gmai.com", "gamil.com", "gnail.com", "gmal.com",
            "gmil.com", "gmaill.com", "gmail.co", "gmail.con", "gmail.cm", "gmail.coom",
            "gmail.comn", "gmsil.com", "hotmial.com", "hotmal.com", "homail.com",
            "outlok.com", "outloook.com", "yaho.com", "yahooo.com", "uol.com.b",
            "bol.com.b", "terra.com.b", "ig.com.b"
        };

        /// <summary>Normaliza e-mail para armazenamento (trim + minúsculas).</summary>
        public static string Normalizar(string email) => email.Trim().ToLowerInvariant();

        public static string? ObterDominio(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return null;

            var at = email.LastIndexOf('@');
            if (at < 0 || at == email.Length - 1)
                return null;

            return email[(at + 1)..].Trim().ToLowerInvariant();
        }

        /// <summary>
        /// Validação síncrona: formato + bloqueio de typos conhecidos.
        /// </summary>
        public static bool IsValido(string? email)
        {
            if (!TemFormatoValido(email))
                return false;

            var dominio = ObterDominio(email!);
            return dominio != null && !EhDominioSuspeito(dominio);
        }

        /// <summary>
        /// Verifica se o domínio aceita e-mail (registro MX ou, na ausência, registro A/AAAA).
        /// </summary>
        public static async Task<bool> DominioAceitaEmailAsync(
            string email,
            CancellationToken cancellationToken = default)
        {
            var dominio = ObterDominio(email);
            if (string.IsNullOrWhiteSpace(dominio))
                return false;

            try
            {
                var mx = await Dns.QueryAsync(dominio, QueryType.MX, cancellationToken: cancellationToken);
                if (mx.Answers.MxRecords().Any(r => !string.IsNullOrWhiteSpace(r.Exchange.Value)))
                    return true;

                var a = await Dns.QueryAsync(dominio, QueryType.A, cancellationToken: cancellationToken);
                if (a.Answers.ARecords().Any())
                    return true;

                var aaaa = await Dns.QueryAsync(dominio, QueryType.AAAA, cancellationToken: cancellationToken);
                return aaaa.Answers.AaaaRecords().Any();
            }
            catch
            {
                return false;
            }
        }

        private static bool TemFormatoValido(string? email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            var trimmed = email.Trim();
            if (!FormatoBasico.IsMatch(trimmed))
                return false;

            try
            {
                var endereco = new MailAddress(trimmed);
                return endereco.Address.Equals(trimmed, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private static bool EhDominioSuspeito(string dominio)
        {
            if (DominiosTypoExplicitos.Contains(dominio))
                return true;

            if (ProvedoresPopulares.Contains(dominio))
                return false;

            foreach (var provedor in ProvedoresPopulares)
            {
                if (DistanciaLevenshtein(dominio, provedor) is >= 1 and <= 2)
                    return true;
            }

            return false;
        }

        private static int DistanciaLevenshtein(string a, string b)
        {
            var n = a.Length;
            var m = b.Length;
            var d = new int[n + 1, m + 1];

            for (var i = 0; i <= n; i++)
                d[i, 0] = i;
            for (var j = 0; j <= m; j++)
                d[0, j] = j;

            for (var i = 1; i <= n; i++)
            {
                for (var j = 1; j <= m; j++)
                {
                    var custo = a[i - 1] == b[j - 1] ? 0 : 1;
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + custo);
                }
            }

            return d[n, m];
        }
    }
}
