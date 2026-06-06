namespace Pc.Servico.Interfaces
{
    public interface IPasswordHasher
    {
        string Hash(string password);
        bool Verify(string password, string storedHash);
        bool IsBcryptHash(string storedHash);
    }
}
