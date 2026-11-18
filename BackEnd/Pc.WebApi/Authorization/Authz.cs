using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Pc.Dominio.Enums;
using Pc.Servico.Interfaces;
using Pc.WebApi.Extensions;

namespace Pc.WebApi.Authorization
{
    public static class Authz
    {
        public const string LojaClaimAusente = "loja_claim_ausente";
        public const string LojaClaimAusenteMensagem =
            "loja_claim_ausente: Seu acesso à loja mudou. Entre de novo.";

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
        /// Staff precisa do claim lojaId. Sem o claim, o token não se renova: 403 com
        /// <see cref="LojaClaimAusente"/>. Claim presente sem vínculo continua Forbid sem código.
        /// </summary>
        public static async Task<IActionResult?> ExigirVinculoDeStaffAsync(
            ControllerBase controller,
            PapelUsuario papel,
            ILojaAcesso acesso)
        {
            if (papel is not (PapelUsuario.Lojista or PapelUsuario.Vendedor))
                return null;

            var loja = await ResolverLojaDoTokenAsync(controller.User, acesso);
            if (loja.ClaimAusente)
                return ProblemaClaimAusente(controller);
            if (!loja.LojaId.HasValue)
                return controller.Forbid();
            return null;
        }

        public static async Task<Guid> ExigirLojaDoTokenNoHubAsync(ClaimsPrincipal user, ILojaAcesso acesso)
        {
            var loja = await ResolverLojaDoTokenAsync(user, acesso);
            if (loja.ClaimAusente)
                throw new HubException(LojaClaimAusenteMensagem);
            if (!loja.LojaId.HasValue)
                throw new HubException("Acesso negado.");
            return loja.LojaId.Value;
        }

        private static async Task<LojaDoToken> ResolverLojaDoTokenAsync(ClaimsPrincipal user, ILojaAcesso acesso)
        {
            if (user.GetLojaId() is not Guid lojaId)
                return new LojaDoToken(null, true);
            if (!await PertenceAsync(user, lojaId, acesso))
                return new LojaDoToken(null, false);
            return new LojaDoToken(lojaId, false);
        }

        private static IActionResult ProblemaClaimAusente(ControllerBase controller)
        {
            controller.Response.Headers["X-Error-Code"] = LojaClaimAusente;
            return new ObjectResult(new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "Seu acesso à loja mudou",
                Detail = "Entre de novo.",
                Extensions = { ["code"] = LojaClaimAusente }
            })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }

        private readonly record struct LojaDoToken(Guid? LojaId, bool ClaimAusente);

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
