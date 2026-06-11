using Microsoft.AspNetCore.Mvc;
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
    }
}
