using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Pc.Dominio.Entities.Usuarios;
using Pc.Dominio.Enums;
using Pc.Servico.Interfaces;
using Pc.WebApi.Authorization;
using Pc.WebApi.DTOs.Comum;
using Pc.WebApi.DTOs.Usuarios;

namespace Pc.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ClientesController : ControllerBase
    {
        private readonly IClienteServico _clienteServico;
        private readonly IJwtTokenServico _jwtTokenServico;

        public ClientesController(IClienteServico clienteServico, IJwtTokenServico jwtTokenServico)
        {
            _clienteServico = clienteServico;
            _jwtTokenServico = jwtTokenServico;
        }

        [AllowAnonymous]
        [HttpPost("registrar")]
        public async Task<IActionResult> Registrar([FromBody] ClienteCriarDto dto)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var cliente = new Cliente
            {
                NomeUsuario = dto.NomeUsuario,
                Email = dto.Email,
                SenhaHash = dto.Senha,
                Telefone = dto.Telefone,
                LatitudeAtual = dto.LatitudeAtual,
                LongitudeAtual = dto.LongitudeAtual
            };

            var novoCliente = await _clienteServico.RegistrarAsync(cliente);

            var resposta = new ClienteRespostaDto
            {
                Id = novoCliente.Id,
                NomeUsuario = novoCliente.NomeUsuario,
                Email = novoCliente.Email,
                Telefone = novoCliente.Telefone,
                Tipo = (int)novoCliente.Tipo,
                UltimoLogin = novoCliente.UltimoLogin,
                LatitudeAtual = novoCliente.LatitudeAtual,
                LongitudeAtual = novoCliente.LongitudeAtual,
                Ativo = novoCliente.Ativo,
                DataCriacao = novoCliente.DataCriacao
            };

            return CreatedAtAction(nameof(ObterPorId), new { id = resposta.Id }, resposta);
        }

        [AllowAnonymous]
        [EnableRateLimiting("login")]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var cliente = await _clienteServico.ValidarLoginAsync(dto.Email, dto.Senha);

            if (cliente == null)
                return Unauthorized("Email ou senha incorretos.");

            var perfil = MapearResposta(cliente);
            var token = _jwtTokenServico.GerarToken(cliente.Id, cliente.Email, TipoUsuario.Cliente);

            return Ok(new LoginRespostaDto<ClienteRespostaDto>
            {
                Token = token,
                Tipo = "cliente",
                Perfil = perfil,
            });
        }

        private static ClienteRespostaDto MapearResposta(Cliente cliente) => new()
        {
            Id = cliente.Id,
            NomeUsuario = cliente.NomeUsuario,
            Email = cliente.Email,
            Telefone = cliente.Telefone,
            Tipo = (int)cliente.Tipo,
            UltimoLogin = cliente.UltimoLogin,
            LatitudeAtual = cliente.LatitudeAtual,
            LongitudeAtual = cliente.LongitudeAtual,
            Ativo = cliente.Ativo,
            DataCriacao = cliente.DataCriacao,
        };

        [Authorize(Policy = PoliticasAutorizacao.QualquerAutenticado)]
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> ObterPorId(Guid id)
        {
            var cliente = await _clienteServico.ObterPorIdAsync(id);

            if (cliente == null)
                return NotFound("Cliente não encontrado.");

            return Ok(MapearResposta(cliente));
        }

        [Authorize(Policy = PoliticasAutorizacao.Admin)]
        [HttpGet("email/{email}")]
        public async Task<IActionResult> BuscarPorEmail(string email)
        {
            var cliente = await _clienteServico.ObterPorEmailAsync(email);

            if (cliente == null)
                return NotFound("Cliente não encontrado.");

            return Ok(MapearResposta(cliente));
        }

        [Authorize(Policy = PoliticasAutorizacao.Admin)]
        [HttpGet]
        public async Task<IActionResult> Listar()
        {
            var clientes = await _clienteServico.ListarAtivosAsync();

            var resposta = clientes.Select(MapearResposta);
            return Ok(resposta);
        }

        [Authorize(Policy = PoliticasAutorizacao.Cliente)]
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Atualizar(Guid id, [FromBody] ClienteCriarDto dto)
        {
            var cliente = await _clienteServico.ObterPorIdAsync(id);
            if (cliente == null)
                return NotFound("Cliente não encontrado.");

            cliente.NomeUsuario = dto.NomeUsuario;
            cliente.Email = dto.Email;
            cliente.Telefone = dto.Telefone;

            await _clienteServico.AtualizarAsync(cliente);

            return Ok(MapearResposta(cliente));
        }

        [Authorize(Policy = PoliticasAutorizacao.Cliente)]
        [HttpPut("{id:guid}/localizacao")]
        public async Task<IActionResult> AtualizarLocalizacao(Guid id, [FromBody] LocalizacaoDto dto)
        {
            await _clienteServico.AtualizarLocalizacaoAsync(id, dto.Latitude, dto.Longitude);

            return Ok(new { mensagem = "Localização atualizada com sucesso" });
        }

        [Authorize(Policy = PoliticasAutorizacao.Admin)]
        [HttpGet("proximidade/buscar")]
        public async Task<IActionResult> BuscarPorProximidade(
            [FromQuery] decimal latitude,
            [FromQuery] decimal longitude,
            [FromQuery] decimal raio = 5)
        {
            var clientes = await _clienteServico.ObterPorProximidadeAsync(latitude, longitude, raio);

            var resposta = clientes.Select(MapearResposta);
            return Ok(resposta);
        }

        [Authorize(Policy = PoliticasAutorizacao.Cliente)]
        [HttpPut("{id:guid}/senha")]
        public async Task<IActionResult> AlterarSenha(Guid id, [FromBody] AlterarSenhaDto dto)
        {
            await _clienteServico.AlterarSenhaAsync(id, dto.SenhaAtual, dto.NovaSenha);

            return Ok(new { mensagem = "Senha alterada com sucesso" });
        }

        [Authorize(Policy = PoliticasAutorizacao.Admin)]
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Remover(Guid id)
        {
            await _clienteServico.RemoverAsync(id);
            return NoContent();
        }
    }
}
