using System.Security.Cryptography;
using System.Text;

namespace Pc.Servico.Implementacoes.Rag
{
    public static class RagHashHelper
    {
        public static string Calcular(string conteudo)
        {
            var normalizado = (conteudo ?? string.Empty).Trim().Replace("\r\n", "\n");
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(normalizado));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }
    }
}
