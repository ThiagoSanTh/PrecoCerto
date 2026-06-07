namespace Pc.Servico.Excecoes
{
    public class ProdutoOperacaoException : Exception
    {
        public ProdutoOperacaoException(string mensagem, bool acessoNegado = false)
            : base(mensagem)
        {
            AcessoNegado = acessoNegado;
        }

        public bool AcessoNegado { get; }
    }
}
