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

        /// <summary>
        /// Admin passa. Os demais precisam do vínculo atual no banco
        /// (dono por Loja.UsuarioId, vendedor por LojaVinculadaId). O claim lojaId não basta.
        /// </summary>
        public static Task<bool> PertenceAsync(ClaimsPrincipal user, Guid lojaId, ILojaAcesso acesso)
        {
            if (user.IsAdmin())
                return Task.FromResult(true);

            var userId = user.GetUserId();
            if (!userId.HasValue)
                return Task.FromResult(false);

            return acesso.PertenceAsync(userId.Value, lojaId);
        }
    }
}
