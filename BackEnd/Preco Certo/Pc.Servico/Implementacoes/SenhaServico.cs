using Pc.Servico.Interfaces;

namespace Pc.Servico.Implementacoes
{
    public class SenhaServico : ISenhaServico
    {
        public string Hash(string senhaPlana) =>
            BCrypt.Net.BCrypt.HashPassword(senhaPlana, workFactor: 12);

        public bool Verificar(string senhaPlana, string senhaArmazenada)
        {
            if (string.IsNullOrWhiteSpace(senhaArmazenada))
                return false;

            if (senhaArmazenada.StartsWith("$2"))
                return BCrypt.Net.BCrypt.Verify(senhaPlana, senhaArmazenada);

            return senhaArmazenada == senhaPlana;
        }
    }
}
