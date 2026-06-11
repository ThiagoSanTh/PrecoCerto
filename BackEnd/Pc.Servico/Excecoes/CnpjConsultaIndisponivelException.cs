namespace Pc.Servico.Excecoes
{
    /// <summary>
    /// Falha temporária ao consultar CNPJ (rate limit, indisponibilidade da BrasilAPI, etc.).
    /// </summary>
    public class CnpjConsultaIndisponivelException : Exception
    {
        public CnpjConsultaIndisponivelException()
            : base("Consulta de CNPJ temporariamente indisponível. Aguarde alguns segundos e tente novamente.")
        {
        }

        public CnpjConsultaIndisponivelException(string message)
            : base(message)
        {
        }
    }
}
