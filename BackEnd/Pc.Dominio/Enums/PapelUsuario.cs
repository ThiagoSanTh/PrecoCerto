namespace Pc.Dominio.Enums
{
    /// <summary>
    /// Papel principal do usuário. Abrir loja adiciona capacidade de lojista;
    /// não remove a capacidade de cliente.
    /// - Cliente: usuário padrão.
    /// - Lojista: cliente que abriu uma loja com CNPJ válido.
    /// - Vendedor: cliente promovido por um lojista para gerenciar o estoque da loja.
    /// </summary>
    public enum PapelUsuario
    {
        Cliente = 1,
        Lojista = 2,
        Vendedor = 3
    }
}
