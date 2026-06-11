using System.ComponentModel.DataAnnotations;
using Pc.Dominio.Validacoes;

namespace Pc.WebApi.Validacao
{
    /// <summary>
    /// Valida telefone brasileiro (8–9 dígitos, DDD opcional). Valores nulos/vazios
    /// são aceitos — combine com [Required] quando o campo for obrigatório.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
    public sealed class TelefoneValidoAttribute : ValidationAttribute
    {
        public TelefoneValidoAttribute()
            : base("Telefone inválido. Informe 8 dígitos (fixo) ou 9 dígitos (celular), com DDD opcional.")
        {
        }

        public override bool IsValid(object? value)
        {
            if (value is null)
                return true;

            var telefone = value as string;
            if (string.IsNullOrWhiteSpace(telefone))
                return true;

            return TelefoneValidator.IsValido(telefone);
        }
    }
}
