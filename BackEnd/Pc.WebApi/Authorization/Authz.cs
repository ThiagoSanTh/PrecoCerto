using Microsoft.AspNetCore.Mvc;
using Pc.Dominio.Entities.Estabelecimentos;
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
        /// Posse de escrita: admin passa; lojista precisa ser o UsuarioId salvo
        /// (o claim lojaId do token pode estar velho por até 24h); vendedor segue o claim.
        /// </summary>
        public static bool OwnsLojaPersistida(ControllerBase controller, Loja? loja)
        {
            if (controller.User.IsAdmin())
                return true;
            if (loja is null)
                return false;

            if (controller.User.IsLojista())
                return loja.UsuarioId == controller.User.GetUserId();

            return controller.User.IsVendedor() && controller.User.GetLojaId() == loja.Id;
        }
    }
}
