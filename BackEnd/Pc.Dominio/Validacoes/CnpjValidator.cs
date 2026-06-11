using System.Linq;

namespace Pc.Dominio.Validacoes
{
    /// <summary>
    /// Validação de formato/dígitos verificadores de CNPJ (sem consulta à Receita Federal).
    /// </summary>
    public static class CnpjValidator
    {
        /// <summary>Remove qualquer caractere não numérico do CNPJ.</summary>
        public static string ApenasDigitos(string? cnpj)
        {
            if (string.IsNullOrWhiteSpace(cnpj))
                return string.Empty;
            return new string(cnpj.Where(char.IsDigit).ToArray());
        }

        /// <summary>
        /// Valida os dígitos verificadores do CNPJ. Retorna false para nulo/vazio,
        /// tamanho diferente de 14, todos dígitos iguais ou DV incorreto.
        /// </summary>
        public static bool IsValido(string? cnpj)
        {
            var digitos = ApenasDigitos(cnpj);
            if (digitos.Length != 14)
                return false;

            // Rejeita sequências repetidas (ex.: 00000000000000).
            if (digitos.Distinct().Count() == 1)
                return false;

            int[] multiplicadores1 = { 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
            int[] multiplicadores2 = { 6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };

            var parcial = digitos.Substring(0, 12);
            var dv1 = CalcularDigito(parcial, multiplicadores1);
            var dv2 = CalcularDigito(parcial + dv1, multiplicadores2);

            return digitos.EndsWith($"{dv1}{dv2}");
        }

        private static int CalcularDigito(string baseCnpj, int[] multiplicadores)
        {
            var soma = 0;
            for (var i = 0; i < multiplicadores.Length; i++)
                soma += (baseCnpj[i] - '0') * multiplicadores[i];

            var resto = soma % 11;
            return resto < 2 ? 0 : 11 - resto;
        }
    }
}
