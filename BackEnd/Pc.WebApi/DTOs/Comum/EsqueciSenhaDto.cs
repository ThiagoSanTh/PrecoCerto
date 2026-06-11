using System.ComponentModel.DataAnnotations;

namespace Pc.WebApi.DTOs.Comum
{
    public class EsqueciSenhaDto
    {
        [Required, EmailAddress, MaxLength(150)]
        public string Email { get; set; } = string.Empty;
    }
}
