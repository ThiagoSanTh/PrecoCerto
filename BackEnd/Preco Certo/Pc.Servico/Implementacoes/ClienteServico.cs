using Pc.Dominio.Entities.Usuarios;
using Pc.Repositorio.Interfaces;
using Pc.Servico.Interfaces;

namespace Pc.Servico.Implementacoes
{
    /// <summary>
    /// Serviço de Cliente: lógica de negócio para autenticação e gerenciamento de clientes
    /// Validações: email único, senha válida, dados obrigatórios
    /// Consolidado - sem intermediário Usuario
    /// </summary>
    public class ClienteServico : IClienteServico
    {
        private readonly IClienteRepositorio _clienteRepositorio;

        public ClienteServico(IClienteRepositorio clienteRepositorio)
        {
            _clienteRepositorio = clienteRepositorio;
        }

        /// <summary>
        /// Registra um novo cliente
        /// Validações: email único, NomeUsuario obrigatório, senha mínima 6 caracteres
        /// </summary>
        public async Task<Cliente> RegistrarAsync(Cliente cliente)
        {
            if (string.IsNullOrWhiteSpace(cliente.Email))
                throw new Exception("Email é obrigatório.");

            if (string.IsNullOrWhiteSpace(cliente.NomeUsuario))
                throw new Exception("Nome de usuário é obrigatório.");

            if (string.IsNullOrWhiteSpace(cliente.SenhaHash) || cliente.SenhaHash.Length < 6)
                throw new Exception("Senha deve ter pelo menos 6 caracteres.");

            var clientes = await _clienteRepositorio.ListarAsync();
            if (clientes.Exists(c => c.Email.ToLower() == cliente.Email.ToLower()))
                throw new Exception("Email já registrado.");

            cliente.Ativo = true;
            cliente.DataCriacao = DateTime.UtcNow;
            cliente.Tipo = Pc.Dominio.Enums.TipoUsuario.Cliente;

            return await _clienteRepositorio.AdicionarAsync(cliente);
        }

        /// <summary>
        /// Valida credenciais de login
        /// Retorna o cliente se credenciais forem válidas
        /// TODO: Usar bcrypt para comparação de senha
        /// </summary>
        public async Task<Cliente?> ValidarLoginAsync(string email, string senha)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(senha))
                return null;

            var cliente = await _clienteRepositorio.ObterPorEmailAsync(email);

            if (cliente == null || cliente.SenhaHash != senha) // TODO: bcrypt
                return null;

            cliente.UltimoLogin = DateTime.UtcNow;
            await _clienteRepositorio.AtualizarAsync(cliente);

            return cliente;
        }

        /// <summary>
        /// Obtém cliente por ID
        /// </summary>
        public async Task<Cliente?> ObterPorIdAsync(Guid id)
        {
            return await _clienteRepositorio.ObterPorIdAsync(id);
        }

        /// <summary>
        /// Obtém cliente por email
        /// </summary>
        public async Task<Cliente?> ObterPorEmailAsync(string email)
        {
            return await _clienteRepositorio.ObterPorEmailAsync(email);
        }

        /// <summary>
        /// Lista todos os clientes ativos
        /// </summary>
        public async Task<List<Cliente>> ListarAtivosAsync()
        {
            return await _clienteRepositorio.ListarAtivosAsync();
        }

        /// <summary>
        /// Lista todos os clientes
        /// </summary>
        public async Task<List<Cliente>> ListarAsync()
        {
            return await _clienteRepositorio.ListarAsync();
        }

        /// <summary>
        /// Atualiza dados do cliente
        /// </summary>
        public async Task AtualizarAsync(Cliente cliente)
        {
            cliente.DataAtualizacao = DateTime.UtcNow;
            await _clienteRepositorio.AtualizarAsync(cliente);
        }

        /// <summary>
        /// Atualiza localização do cliente (geolocalização)
        /// Validações: Latitude -90~90, Longitude -180~180
        /// </summary>
        public async Task AtualizarLocalizacaoAsync(Guid clienteId, decimal latitude, decimal longitude)
        {
            if (latitude < -90 || latitude > 90)
                throw new Exception("Latitude fora do intervalo válido (-90 a 90).");

            if (longitude < -180 || longitude > 180)
                throw new Exception("Longitude fora do intervalo válido (-180 a 180).");

            await _clienteRepositorio.AtualizarLocalizacaoAsync(clienteId, latitude, longitude);
        }

        /// <summary>
        /// Obtém clientes por proximidade (raio em KM)
        /// Usa Haversine para cálculo de distância
        /// </summary>
        public async Task<List<Cliente>> ObterPorProximidadeAsync(decimal latitude, decimal longitude, decimal raioKm)
        {
            if (raioKm <= 0)
                throw new Exception("Raio deve ser maior que zero.");

            return await _clienteRepositorio.ObterPorProximidadeAsync(latitude, longitude, raioKm);
        }

        /// <summary>
        /// Altera a senha do cliente
        /// TODO: Usar bcrypt para comparação
        /// </summary>
        public async Task AlterarSenhaAsync(Guid clienteId, string senhaAtual, string novaSenha)
        {
            if (string.IsNullOrWhiteSpace(novaSenha) || novaSenha.Length < 6)
                throw new Exception("Senha deve ter pelo menos 6 caracteres.");

            var cliente = await _clienteRepositorio.ObterPorIdAsync(clienteId);
            if (cliente == null)
                throw new Exception("Cliente não encontrado.");

            if (cliente.SenhaHash != senhaAtual) // TODO: bcrypt
                throw new Exception("Senha atual incorreta.");

            cliente.SenhaHash = novaSenha;
            await _clienteRepositorio.AtualizarAsync(cliente);
        }

        /// <summary>
        /// Remove/desativa cliente (soft delete)
        /// </summary>
        public async Task RemoverAsync(Guid id)
        {
            var cliente = await _clienteRepositorio.ObterPorIdAsync(id);
            if (cliente != null)
            {
                cliente.Ativo = false;
                await _clienteRepositorio.AtualizarAsync(cliente);
            }
        }

    }
}

