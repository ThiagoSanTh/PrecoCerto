namespace Pc.Servico.Interfaces
{
    public interface IValidadorEmail
    {
        Task ValidarAsync(string email, CancellationToken cancellationToken = default);
    }
}
