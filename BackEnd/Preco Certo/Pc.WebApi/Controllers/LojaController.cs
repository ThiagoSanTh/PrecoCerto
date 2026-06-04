using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pc.Dominio.Entities.Estabelecimentos;
using Pc.Servico.Interfaces;
using Pc.WebApi.Authorization;
using Pc.WebApi.DTOs;

namespace Pc.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LojaController : ControllerBase
    {

        private readonly ILojaServico _lojaServico;
        public LojaController(ILojaServico lojaServico)
        {
            _lojaServico = lojaServico;
        }

        [AllowAnonymous]
        [HttpGet("Listar")]
        public async Task<IActionResult> Listar()
        {
            var lojas = await _lojaServico.ListarLojasAsync();
            var resposta = lojas.Select(loja => new LojaRespostaDto
            {
                Id = loja.Id,
                NomeFantasia = loja.NomeFantasia,
                Telefone = loja.Telefone,
                Email = loja.Email,
                RazaoSocial = loja.RazaoSocial,
                Cnpj = loja.Cnpj,
                Endereco = loja.Endereco == null ? new EnderecoDto() : new EnderecoDto
                {
                    Cep = loja.Endereco.Cep,
                    Logradouro = loja.Endereco.Logradouro,
                    Numero = loja.Endereco.Numero,
                    Bairro = loja.Endereco.Bairro,
                    Cidade = loja.Endereco.Cidade,
                    Estado = loja.Endereco.Estado,
                    Latitude = loja.Endereco.Latitude,
                    Longitude = loja.Endereco.Longitude
                },

            });
            return Ok(resposta);
        }
        [AllowAnonymous]
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> ObterPorId(Guid id)
        {
            var loja = await _lojaServico.ObterLojaPorIdAsync(id);
            if (loja == null)
                return NotFound("Loja não encontrada");

            var resposta = new LojaRespostaDto
            {
                Id = loja.Id,
                NomeFantasia = loja.NomeFantasia,
                RazaoSocial = loja.RazaoSocial,
                Cnpj = loja.Cnpj,
                Telefone = loja.Telefone,
                Email = loja.Email,
                Endereco = loja.Endereco == null ? new EnderecoDto() : new EnderecoDto
                {
                    Cep = loja.Endereco.Cep,
                    Logradouro = loja.Endereco.Logradouro,
                    Numero = loja.Endereco.Numero,
                    Bairro = loja.Endereco.Bairro,
                    Cidade = loja.Endereco.Cidade,
                    Estado = loja.Endereco.Estado,
                    Latitude = loja.Endereco.Latitude,
                    Longitude = loja.Endereco.Longitude
                },
            };
            return Ok(resposta);
        }
        [AllowAnonymous]
        [HttpPost("Buscar")]
        public async Task<IActionResult> BuscarPorNome([FromBody] string nome)
        {
            var lojas = await _lojaServico.BuscarPorNomeAsync(nome);
            var resposta = lojas.Select(loja => new LojaRespostaDto
            {
                Id = loja.Id,
                NomeFantasia = loja.NomeFantasia,
                Telefone = loja.Telefone,
                Email = loja.Email,
                RazaoSocial = loja.RazaoSocial,
                Cnpj = loja.Cnpj,
                Endereco = loja.Endereco == null ? new EnderecoDto() : new EnderecoDto
                {
                    Cep = loja.Endereco.Cep,
                    Logradouro = loja.Endereco.Logradouro,
                    Numero = loja.Endereco.Numero,
                    Bairro = loja.Endereco.Bairro,
                    Cidade = loja.Endereco.Cidade,
                    Estado = loja.Endereco.Estado,
                    Latitude = loja.Endereco.Latitude,
                    Longitude = loja.Endereco.Longitude
                },
            });
            return Ok(resposta);
        }
        [Authorize(Policy = PoliticasAutorizacao.LojistaOuAdmin)]
        [HttpPost]
        public async Task<IActionResult> Adicionar([FromBody] LojaCriarDto lojaDto)

        {
            var loja = new Loja
            {
                NomeFantasia = lojaDto.NomeFantasia,
                RazaoSocial = lojaDto.RazaoSocial,
                Cnpj = lojaDto.Cnpj,
                Telefone = lojaDto.Telefone,
                Email = lojaDto.Email,
            };
            var novaLoja = await _lojaServico.AdicionarAsync(loja);
            var resposta = new LojaRespostaDto
            {
                Id = novaLoja.Id,
                NomeFantasia = novaLoja.NomeFantasia,
                RazaoSocial = novaLoja.RazaoSocial,
                Cnpj = novaLoja.Cnpj,
                Telefone = novaLoja.Telefone,
                Email = novaLoja.Email,
                Endereco = novaLoja.Endereco == null ? new EnderecoDto() : new EnderecoDto
                {
                    Cep = novaLoja.Endereco.Cep,
                    Logradouro = novaLoja.Endereco.Logradouro,
                    Numero = novaLoja.Endereco.Numero,
                    Bairro = novaLoja.Endereco.Bairro,
                    Cidade = novaLoja.Endereco.Cidade,
                    Estado = novaLoja.Endereco.Estado,
                    Latitude = novaLoja.Endereco.Latitude,
                    Longitude = novaLoja.Endereco.Longitude
                },
            };
            return CreatedAtAction(nameof(ObterPorId), new { id = resposta.Id }, resposta);
        }
        [Authorize(Policy = PoliticasAutorizacao.LojistaOuAdmin)]
        [HttpPost("{id:guid}")]
        public async Task<IActionResult> Atualizar(Guid id, [FromBody] LojaCriarDto lojaDto)
        {
            var lojaExistente = await _lojaServico.ObterLojaPorIdAsync(id);
            if (lojaExistente == null)
                return NotFound("Loja não encontrada");

            lojaExistente.NomeFantasia = lojaDto.NomeFantasia;
            lojaExistente.RazaoSocial = lojaDto.RazaoSocial;
            lojaExistente.Cnpj = lojaDto.Cnpj;
            lojaExistente.Telefone = lojaDto.Telefone;
            lojaExistente.Email = lojaDto.Email;
            lojaExistente.EnderecoId = (Guid)lojaDto.EnderecoId;
            lojaExistente.LojistaId = (Guid)lojaDto.LojistaId;


            await _lojaServico.AtualizarLojaAsync(lojaExistente);
            return NoContent();
        }
        [Authorize(Policy = PoliticasAutorizacao.LojistaOuAdmin)]
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Remover(Guid id)
        {
            var lojaExistente = await _lojaServico.ObterLojaPorIdAsync(id);

            if (lojaExistente == null)
                return NotFound("Loja não encontrada.");

            await _lojaServico.RemoverLojaAsync(id);
            return NoContent();
        }

    }
}
