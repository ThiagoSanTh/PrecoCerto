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
    }
}
