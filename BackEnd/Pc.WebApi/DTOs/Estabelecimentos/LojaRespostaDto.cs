namespace Pc.WebApi.DTOs.Estabelecimentos
{
    public class LojaRespostaDto
    {
        public Guid Id { get; set; }
        public string NomeFantasia { get; set; } = string.Empty;
        public string RazaoSocial { get; set; } = string.Empty;
        public string Cnpj { get; set; } = string.Empty;
        public string Telefone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public EnderecoDto Endereco { get; set; } = new EnderecoDto();
        public double? MediaAvaliacoes { get; set; }
    }
}
