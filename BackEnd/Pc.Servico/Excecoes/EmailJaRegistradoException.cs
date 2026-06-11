namespace Pc.Servico.Excecoes
{
    /// <summary>
    /// Lançada no cadastro quando o e-mail já pertence a um usuário com confirmação concluída.
    /// </summary>
    public class EmailJaRegistradoException : Exception
    {
        public EmailJaRegistradoException()
            : base("Este e-mail já está cadastrado.")
        {
        }
    }
}
