namespace Pc.Dominio.Enums
{
    /// <summary>
    /// Papel de um usuário dentro do sistema.
    /// O papel é derivado das ações do usuário:
    /// - Cliente: usuário padrão (não possui loja).
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
