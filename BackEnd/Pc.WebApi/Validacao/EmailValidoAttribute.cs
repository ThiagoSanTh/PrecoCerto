using System.ComponentModel.DataAnnotations;
using Pc.Dominio.Validacoes;

namespace Pc.WebApi.Validacao
{
    /// <summary>
    /// Valida formato de e-mail (user@dominio.tld). Não exige confirmação por link.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
    public sealed class EmailValidoAttribute : ValidationAttribute
    {
        public EmailValidoAttribute()
            : base("Informe um e-mail válido.")
        {
        }

        public override bool IsValid(object? value)
        {
            if (value is null)
                return true;

            var email = value as string;
            if (string.IsNullOrWhiteSpace(email))
                return true;

            return EmailValidator.IsValido(email);
        }
    }
}
