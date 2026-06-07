namespace Pc.WebApi.DTOs.Usuarios
{
    public class AdminRespostaDto
    {
        public Guid Id { get; set; }
        public string NomeUsuario { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Telefone { get; set; }
        public int Tipo { get; set; }
        public int NivelAcesso { get; set; }
        public string? Permissoes { get; set; }
        public DateTime? UltimoLogin { get; set; }
        public bool Ativo { get; set; }
        public DateTime DataCriacao { get; set; }
    }
}
