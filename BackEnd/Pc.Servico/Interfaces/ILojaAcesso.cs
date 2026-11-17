namespace Pc.Servico.Interfaces
{
    /// <summary>
    /// Vínculo atual do usuário com a loja, lido do banco.
    /// Dono: <c>Loja.UsuarioId</c>. Vendedor: papel Vendedor e <c>LojaVinculadaId</c>.
    /// </summary>
    public interface ILojaAcesso
    {
        Task<bool> PertenceAsync(Guid usuarioId, Guid lojaId);
    }
}
