using System.Security.Claims;

namespace Pc.WebApi.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        public static Guid? GetUserId(this ClaimsPrincipal user)
        {
            var value = user.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? user.FindFirstValue(ClaimTypes.Name)
                ?? user.FindFirstValue("sub");

            return Guid.TryParse(value, out var id) ? id : null;
        }

        public static Guid? GetLojaId(this ClaimsPrincipal user)
        {
            var value = user.FindFirstValue("lojaId");
            return Guid.TryParse(value, out var id) ? id : null;
        }

        public static bool IsAdmin(this ClaimsPrincipal user) =>
            user.IsInRole("Admin");

        public static bool IsLojista(this ClaimsPrincipal user) =>
            user.IsInRole("Lojista");

        public static bool IsVendedor(this ClaimsPrincipal user) =>
            user.IsInRole("Vendedor");

        public static bool IsCliente(this ClaimsPrincipal user) =>
            user.IsInRole("Cliente");
    }
}
