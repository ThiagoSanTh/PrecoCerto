using Pc.Servico.Interfaces;

namespace Pc.Servico.Implementacoes
{
    public class BcryptPasswordHasher : IPasswordHasher
    {
        public string Hash(string password) =>
            BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);

        public bool Verify(string password, string storedHash)
        {
            if (string.IsNullOrEmpty(storedHash))
                return false;

            try
            {
                if (IsBcryptHash(storedHash))
                    return BCrypt.Net.BCrypt.Verify(password, storedHash);
            }
            catch
            {
                return false;
            }

            return false;
        }

        public bool IsBcryptHash(string storedHash) =>
            storedHash.StartsWith("$2", StringComparison.Ordinal);
    }
}
