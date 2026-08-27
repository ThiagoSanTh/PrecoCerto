using Pc.Dominio.Entities.Usuarios;
using Pc.Dominio.Enums;

namespace Pc.WebApi.Authorization
{
    /// <summary>
    /// Resolve loja e tipo de login a partir do usuário autenticado.
    /// Vendedor usa LojaVinculada; lojista usa LojaPropria. A identidade permanece o próprio usuário.
    /// </summary>
    public static class LoginLojaResolver
    {
        public static bool EhStaffLoja(Usuario usuario)
        {
            if (usuario.Papel is PapelUsuario.Lojista or PapelUsuario.Vendedor)
                return true;
            if (usuario.LojaVinculadaId.HasValue && usuario.LojaVinculadaId.Value != Guid.Empty)
                return true;
            return usuario.LojaPropria != null;
        }

        public static TipoUsuario ResolverTipoJwt(Usuario usuario)
        {
            if (usuario.Papel == PapelUsuario.Lojista)
                return TipoUsuario.Lojista;

            if (usuario.Papel == PapelUsuario.Vendedor
                || (usuario.LojaVinculadaId.HasValue && usuario.LojaVinculadaId.Value != Guid.Empty))
                return TipoUsuario.Vendedor;

            if (usuario.LojaPropria != null)
                return TipoUsuario.Lojista;

            return TipoUsuario.Cliente;
        }

        public static string ResolverTipoResposta(Usuario usuario) =>
            ResolverTipoJwt(usuario) switch
            {
                TipoUsuario.Lojista => "lojista",
                TipoUsuario.Vendedor => "vendedor",
                _ => "cliente"
            };

        public static Guid? ResolverLojaId(Usuario usuario)
        {
            return ResolverTipoJwt(usuario) switch
            {
                TipoUsuario.Lojista => usuario.LojaPropria?.Id,
                TipoUsuario.Vendedor => usuario.LojaVinculadaId ?? usuario.LojaVinculada?.Id,
                _ => null
            };
        }

        public static string ResolverNomeLoja(Usuario usuario) =>
            usuario.LojaPropria?.NomeFantasia
            ?? usuario.LojaVinculada?.NomeFantasia
            ?? string.Empty;
    }
}
