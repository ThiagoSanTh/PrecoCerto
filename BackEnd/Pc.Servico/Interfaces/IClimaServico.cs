using Pc.Servico.Modelos;

namespace Pc.Servico.Interfaces
{
    public interface IClimaServico
    {
        Task<ClimaResposta> ObterPorCoordenadasAsync(
            decimal latitude,
            decimal longitude,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Contrato do fornecedor externo. O restante da aplicação usa só <see cref="IClimaServico"/>.
    /// </summary>
    public interface IClimaProvedor
    {
        string Nome { get; }

        Task<ClimaResposta> ConsultarAsync(
            decimal latitude,
            decimal longitude,
            CancellationToken cancellationToken = default);
    }
}
