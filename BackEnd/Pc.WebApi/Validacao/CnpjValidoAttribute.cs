using System.ComponentModel.DataAnnotations;
using Pc.Dominio.Validacoes;

namespace Pc.WebApi.Validacao
{
    /// <summary>
    /// Valida dígitos verificadores de CNPJ. Valores nulos/vazios são aceitos —
    /// combine com [Required] quando o campo for obrigatório.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
    public sealed class CnpjValidoAttribute : ValidationAttribute
    {
        public CnpjValidoAttribute()
            : base("CNPJ inválido. Verifique os dígitos informados.")
        {
        }

        public override bool IsValid(object? value)
        {
            if (value is null)
                return true;

            var cnpj = value as string;
            if (string.IsNullOrWhiteSpace(cnpj))
                return true;

            return CnpjValidator.IsValido(cnpj);
        }
    }
}
