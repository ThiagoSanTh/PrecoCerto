using Pc.Repositorio.Interfaces;
using Pc.Dominio.Enums;
using Pc.Servico.Interfaces;

namespace Pc.Servico.Implementacoes
{
    public class LojaAcesso : ILojaAcesso
    {
        private readonly ILojaRepositorio _lojas;
        private readonly IClienteRepositorio _usuarios;

        public LojaAcesso(ILojaRepositorio lojas, IClienteRepositorio usuarios)
        {
            _lojas = lojas;
            _usuarios = usuarios;
        }

        public async Task<bool> PertenceAsync(Guid usuarioId, Guid lojaId)
        {
            if (usuarioId == Guid.Empty || lojaId == Guid.Empty)
                return false;

            var lojaTask = _lojas.ObterPorIdAsync(lojaId);
            var usuarioTask = _usuarios.ObterPorIdAsync(usuarioId);
            await Task.WhenAll(lojaTask, usuarioTask);

            var loja = await lojaTask;
            if (loja is null)
                return false;
            if (loja.UsuarioId == usuarioId)
                return true;

            var usuario = await usuarioTask;
            return usuario is { Papel: PapelUsuario.Vendedor, LojaVinculadaId: var vinculada }
                && vinculada == loja.Id;
        }
    }
}
