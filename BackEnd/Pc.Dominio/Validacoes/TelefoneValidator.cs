using System.Linq;

namespace Pc.Dominio.Validacoes
{
    /// <summary>
    /// Validação de telefone brasileiro: 8 dígitos (fixo) ou 9 dígitos (celular),
    /// com DDD opcional (10 ou 11 dígitos no total). Máscara é ignorada.
    /// </summary>
    public static class TelefoneValidator
    {
        /// <summary>Remove qualquer caractere não numérico do telefone.</summary>
        public static string ApenasDigitos(string? telefone)
        {
            if (string.IsNullOrWhiteSpace(telefone))
                return string.Empty;
            return new string(telefone.Where(char.IsDigit).ToArray());
        }

        /// <summary>
        /// Valida o telefone após remover a máscara:
        /// 8 dígitos (fixo sem DDD), 9 (celular sem DDD), 10 (fixo com DDD) ou 11 (celular com DDD).
        /// </summary>
        public static bool IsValido(string? telefone)
        {
            var digitos = ApenasDigitos(telefone);
            if (digitos.Length < 8 || digitos.Length > 11)
                return false;

            // Rejeita sequências repetidas (ex.: 99999999999).
            if (digitos.Distinct().Count() == 1)
                return false;

            // Celular (9 dígitos sem DDD ou 11 com DDD) deve começar com 9.
            if (digitos.Length == 9 && digitos[0] != '9')
                return false;
            if (digitos.Length == 11 && digitos[2] != '9')
                return false;

            return true;
        }
    }
}
