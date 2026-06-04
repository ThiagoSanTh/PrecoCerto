namespace Pc.WebApi.DTOs.Usuarios
{
    public class LojistaRespostaDto
    {
        public Guid Id { get; set; }
        public string NomeUsuario { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Telefone { get; set; }
        public int Tipo { get; set; }
        public DateTime? UltimoLogin { get; set; }
        public Guid? LojaId { get; set; }
        public string NomeLoja { get; set; } = string.Empty;
        public string? Cargo { get; set; }
        public bool Ativo { get; set; }
        public DateTime DataCriacao { get; set; }
    }
}
