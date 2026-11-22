namespace Pc.Servico.Excecoes
{
    /// <summary>
    /// O vendedor já está vinculado a outra loja. Quem chama não pode promovê-lo nem removê-lo.
    /// </summary>
    public class VendedorDeOutraLojaException : Exception
    {
        public VendedorDeOutraLojaException()
            : base("Vendedor pertence a outra loja.")
        {
        }
    }
}
