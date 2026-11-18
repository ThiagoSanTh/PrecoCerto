using Pc.Repositorio.Interfaces;
using Pc.Dominio.Enums;
using Pc.Servico.Interfaces;

namespace Pc.Servico.Implementacoes
{
    public class LojaAcesso : ILojaAcesso
    {
        private readonly ILojaRepositorio _lojas;
        private readonly IClienteRepositorio _usuarios;
        private readonly IAdminRepositorio? _admins;

        public LojaAcesso(ILojaRepositorio lojas, IClienteRepositorio usuarios, IAdminRepositorio? admins = null)
        {
            _lojas = lojas;
            _usuarios = usuarios;
            _admins = admins;
        }

        public async Task<bool> PertenceAsync(Guid usuarioId, Guid lojaId)
        {
            if (usuarioId == Guid.Empty || lojaId == Guid.Empty)
                return false;

            var loja = await _lojas.ObterPorIdAsync(lojaId);
            if (loja is null)
                return false;

            var usuario = await _usuarios.ObterPorIdAsync(usuarioId);
            if (usuario is not { Ativo: true })
                return false;
            if (loja.UsuarioId == usuarioId)
                return true;

            return usuario.Papel == PapelUsuario.Vendedor
                && usuario.LojaVinculadaId == loja.Id;
        }

        public async Task<bool> EhAdminAsync(Guid usuarioId)
        {
            if (_admins is null || usuarioId == Guid.Empty)
                return false;

            var admin = await _admins.ObterPorIdAsync(usuarioId);
            return admin is { Ativo: true };
        }
    }
}
