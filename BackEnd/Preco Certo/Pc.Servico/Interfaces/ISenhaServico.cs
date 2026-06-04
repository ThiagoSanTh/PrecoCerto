namespace Pc.Servico.Interfaces
{
    public interface ISenhaServico
    {
        string Hash(string senhaPlana);
        bool Verificar(string senhaPlana, string senhaArmazenada);
    }
}
