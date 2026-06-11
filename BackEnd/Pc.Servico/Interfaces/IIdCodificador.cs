namespace Pc.Servico.Interfaces
{
    public interface IIdCodificador
    {
        string Codificar(Guid id);
        Guid? Decodificar(string codigo);
        bool TentarDecodificar(string codigo, out Guid id);
    }
}
