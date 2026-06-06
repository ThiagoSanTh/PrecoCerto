namespace Pc.WebApi.DTOs.Usuarios
{
    public class LojistaCriarDto
    {
        public string NomeUsuario { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Senha { get; set; } = string.Empty;
        public string? Telefone { get; set; }
        public Guid? LojaId { get; set; }
        public string? Cargo { get; set; }
    }
}
