namespace Pc.Servico.Interfaces
{
    public interface IConsultaCnpjServico
    {
        Task<CnpjConsultaResultado?> ConsultarAsync(string cnpj, CancellationToken cancellationToken = default);
    }

    public class CnpjConsultaResultado
    {
        public string Cnpj { get; set; } = string.Empty;
        public string RazaoSocial { get; set; } = string.Empty;
        public string? NomeFantasia { get; set; }
        public string SituacaoCadastral { get; set; } = string.Empty;
        public bool Ativo =>
            SituacaoCadastral.Trim().Equals("ATIVA", StringComparison.OrdinalIgnoreCase);
        public CnpjEndereco? Endereco { get; set; }
        public string? Email { get; set; }
        public string? Telefone { get; set; }
    }

    public class CnpjEndereco
    {
        public string? Logradouro { get; set; }
        public string? Numero { get; set; }
        public string? Complemento { get; set; }
        public string? Bairro { get; set; }
        public string? Cidade { get; set; }
        public string? Uf { get; set; }
        public string? Cep { get; set; }
    }
}
