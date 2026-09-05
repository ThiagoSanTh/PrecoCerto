namespace Pc.Servico.Interfaces
{
    public interface IEmbeddingService
    {
        Task<float[]> GerarEmbeddingAsync(string texto, CancellationToken cancellationToken = default);
    }
}
