using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Pc.Dominio.Enums;
using Pc.WebApi.Extensions;

namespace Pc.WebApi.Authorization
{
    /// <summary>
    /// Contexto operacional enviado pelo app (Modo Cliente vs Modo Loja).
    /// Não altera a identidade nem as roles do JWT; só escolhe a caixa de entrada/persona do chat.
    /// </summary>
    public static class ContextoOperacional
    {
        public const string HeaderName = "X-Contexto-Operacional";
        public const string QueryName = "contexto";
        public const string Cliente = "cliente";
        public const string Loja = "loja";

        public static string? Ler(HttpRequest? request)
        {
            if (request == null)
                return null;

            if (request.Headers.TryGetValue(HeaderName, out var header)
                && !string.IsNullOrWhiteSpace(header))
                return header.ToString().Trim();

            var query = request.Query[QueryName].FirstOrDefault();
            return string.IsNullOrWhiteSpace(query) ? null : query.Trim();
        }

        public static PapelUsuario ResolverPapel(ClaimsPrincipal user, string? contexto)
        {
            var comoCliente = string.Equals(contexto, Cliente, StringComparison.OrdinalIgnoreCase);
            if (comoCliente)
                return PapelUsuario.Cliente;

            if (user.IsLojista())
                return PapelUsuario.Lojista;

            if (user.IsVendedor())
                return PapelUsuario.Vendedor;

            return PapelUsuario.Cliente;
        }
    }
}
