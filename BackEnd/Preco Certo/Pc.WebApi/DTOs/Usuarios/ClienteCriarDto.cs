using System.ComponentModel.DataAnnotations;

namespace Pc.WebApi.DTOs.Usuarios
{
    public class ClienteCriarDto
    {
        [Required(ErrorMessage = "Nome de usuário é obrigatório.")]
        [MaxLength(100)]
        public string NomeUsuario { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email é obrigatório.")]
        [EmailAddress(ErrorMessage = "Email inválido.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Senha é obrigatória.")]
        [MinLength(6, ErrorMessage = "Senha deve ter pelo menos 6 caracteres.")]
        public string Senha { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? Telefone { get; set; }

        [Range(-90, 90)]
        public decimal? LatitudeAtual { get; set; }

        [Range(-180, 180)]
        public decimal? LongitudeAtual { get; set; }
    }
}
