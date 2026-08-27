namespace Pc.Servico.Excecoes
{
    /// <summary>
    /// Provedor de clima indisponível, timeout ou resposta inválida.
    /// </summary>
    public class ClimaIndisponivelException : Exception
    {
        public ClimaIndisponivelException()
            : base("Consulta de clima temporariamente indisponível.")
        {
        }

        public ClimaIndisponivelException(string message)
            : base(message)
        {
        }
    }
}
