using System.Linq;

namespace Pc.Dominio.Validacoes
{
    /// <summary>
    /// Validação de formato/dígitos verificadores de CPF (sem consulta à Receita Federal).
    /// </summary>
    public static class CpfValidator
    {
        /// <summary>Remove qualquer caractere não numérico do CPF.</summary>
        public static string ApenasDigitos(string? cpf)
        {
            if (string.IsNullOrWhiteSpace(cpf))
                return string.Empty;
            return new string(cpf.Where(char.IsDigit).ToArray());
        }

        /// <summary>
        /// Valida os dígitos verificadores do CPF. Retorna false para nulo/vazio,
        /// tamanho diferente de 11, todos dígitos iguais ou DV incorreto.
        /// </summary>
        public static bool IsValido(string? cpf)
        {
            var digitos = ApenasDigitos(cpf);
            if (digitos.Length != 11)
                return false;

            // Rejeita sequências repetidas (ex.: 00000000000).
            if (digitos.Distinct().Count() == 1)
                return false;

            var dv1 = CalcularDigito(digitos.Substring(0, 9), 10);
            var dv2 = CalcularDigito(digitos.Substring(0, 9) + dv1, 11);

            return digitos.EndsWith($"{dv1}{dv2}");
        }

        private static int CalcularDigito(string baseCpf, int pesoInicial)
        {
            var soma = 0;
            for (var i = 0; i < baseCpf.Length; i++)
                soma += (baseCpf[i] - '0') * (pesoInicial - i);

            var resto = soma % 11;
            return resto < 2 ? 0 : 11 - resto;
        }
    }
}
