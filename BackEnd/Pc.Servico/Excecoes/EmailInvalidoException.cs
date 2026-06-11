namespace Pc.Servico.Excecoes
{
    public class EmailInvalidoException : Exception
    {
        public EmailInvalidoException()
            : base("Informe um e-mail válido.")
        {
        }
    }
}
