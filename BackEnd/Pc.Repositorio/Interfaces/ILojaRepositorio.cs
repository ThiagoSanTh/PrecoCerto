using Pc.Dominio.Entities.Estabelecimentos;

namespace Pc.Repositorio.Interfaces
{
    public interface ILojaRepositorio : IRepositorio<Loja>
    {
        Task<List<Loja>> BuscarPorNomeAsync(string nome);
    }
}
