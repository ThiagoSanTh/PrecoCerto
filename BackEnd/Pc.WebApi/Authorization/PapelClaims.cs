using Pc.Dominio.Enums;

namespace Pc.WebApi.Authorization
{
    /// <summary>
    /// Roles JWT derivadas do tipo do usuário.
    /// Cliente é capacidade-base: abrir uma loja adiciona Lojista/Vendedor, não remove Cliente.
    /// </summary>
    public static class PapelClaims
    {
        public static IReadOnlyList<string> RolesPara(TipoUsuario tipo)
        {
            if (tipo == TipoUsuario.Admin)
                return new[] { "Admin" };

            if (tipo == TipoUsuario.Lojista)
                return new[] { "Cliente", "Lojista" };

            if (tipo == TipoUsuario.Vendedor)
                return new[] { "Cliente", "Vendedor" };

            return new[] { "Cliente" };
        }
    }
}
