using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Pc.Servico.Interfaces;
using Pc.WebApi.Extensions;

namespace Pc.WebApi.Authorization
{
    public static class Authz
    {
        public static bool IsSelfOrAdmin(ControllerBase controller, Guid resourceUserId) =>
            controller.User.IsAdmin() ||
            controller.User.GetUserId() == resourceUserId;

        public static IActionResult ForbidUnlessSelfOrAdmin(ControllerBase controller, Guid resourceUserId)
        {
            if (IsSelfOrAdmin(controller, resourceUserId))
                return null!;

            return controller.Forbid();
        }

        public static bool OwnsLoja(ControllerBase controller, Guid lojaId) =>
            controller.User.IsAdmin() ||
            ((controller.User.IsLojista() || controller.User.IsVendedor())
                && controller.User.GetLojaId() == lojaId);

        /// <summary>
        /// Admin passa só com o papel ativo no banco. Os demais precisam do vínculo
        /// atual (dono por Loja.UsuarioId, vendedor por LojaVinculadaId). O claim não basta.
        /// </summary>
        public static Task<bool> PertenceAsync(ClaimsPrincipal user, Guid lojaId, ILojaAcesso acesso)
        {
            var userId = user.GetUserId();
            if (!userId.HasValue)
                return Task.FromResult(false);

            if (user.IsAdmin())
                return acesso.EhAdminAsync(userId.Value);

            return acesso.PertenceAsync(userId.Value, lojaId);
        }
    }
}
