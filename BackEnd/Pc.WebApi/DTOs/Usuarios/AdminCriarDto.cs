using System.ComponentModel.DataAnnotations;
using Pc.WebApi.Validacao;

namespace Pc.WebApi.DTOs.Usuarios
{
    public class AdminCriarDto
    {
        [Required, MinLength(2), MaxLength(150)]
        public string NomeUsuario { get; set; } = string.Empty;

        [Required, EmailAddress, MaxLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required, MinLength(6), MaxLength(100)]
        public string Senha { get; set; } = string.Empty;

        [TelefoneValido, MaxLength(20)]
        public string? Telefone { get; set; }

        [Range(1, 3, ErrorMessage = "Nível de acesso deve ser 1 (SuperAdmin), 2 (Admin) ou 3 (Moderador).")]
        public int NivelAcesso { get; set; } = 2;
    }
}
