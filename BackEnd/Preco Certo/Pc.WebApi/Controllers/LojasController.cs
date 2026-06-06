using Microsoft.AspNetCore.Mvc;
using Pc.Dominio.Entities.Catalogo;
using Pc.Dominio.Entities.Estabelecimentos;
using Pc.Servico.Interfaces;
using Pc.WebApi.DTOs.Estabelecimentos;
using Pc.WebApi.Mappings;

namespace Pc.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LojasController : ControllerBase
    {
        private readonly ILojaServico _lojaServico;

        public LojasController(ILojaServico lojaServico)
        {
            _lojaServico = lojaServico;
        }

        [HttpGet]
        public async Task<IActionResult> Listar()
        {
            var lojas = await _lojaServico.ListarAsync();
            return Ok(lojas.Select(LojaMapper.ParaRespostaDto));
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> ObterPorId(Guid id)
        {
            var loja = await _lojaServico.ObterPorIdAsync(id);
            if (loja is null)
                return NotFound("Loja não encontrada.");

            return Ok(LojaMapper.ParaRespostaDto(loja));
        }

        [HttpPost("buscar")]
        public async Task<IActionResult> BuscarPorNome([FromBody] string nome)
        {
            var lojas = await _lojaServico.BuscarPorNomeAsync(nome);
            return Ok(lojas.Select(LojaMapper.ParaRespostaDto));
        }

        [HttpPost]
        public async Task<IActionResult> Adicionar([FromBody] LojaCriarDto dto)
        {
            var loja = new Loja
            {
                NomeFantasia = dto.NomeFantasia,
                RazaoSocial = dto.RazaoSocial,
                Cnpj = dto.Cnpj,
                Telefone = dto.Telefone,
                Email = dto.Email,
                Descricao = dto.Descricao,
                LojistaId = dto.LojistaId,
                Endereco = CriarEndereco(dto.Endereco)
            };

            var novaLoja = await _lojaServico.AdicionarAsync(loja);
            var resposta = LojaMapper.ParaRespostaDto(novaLoja);

            return CreatedAtAction(nameof(ObterPorId), new { id = resposta.Id }, resposta);
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Atualizar(Guid id, [FromBody] LojaCriarDto dto)
        {
            var lojaExistente = await _lojaServico.ObterPorIdAsync(id);
            if (lojaExistente is null)
                return NotFound("Loja não encontrada.");

            lojaExistente.NomeFantasia = dto.NomeFantasia;
            lojaExistente.RazaoSocial = dto.RazaoSocial;
            lojaExistente.Cnpj = dto.Cnpj;
            lojaExistente.Telefone = dto.Telefone;
            lojaExistente.Email = dto.Email;
            lojaExistente.Descricao = dto.Descricao;

            if (dto.EnderecoId.HasValue)
                lojaExistente.EnderecoId = dto.EnderecoId.Value;

            if (dto.LojistaId.HasValue)
                lojaExistente.LojistaId = dto.LojistaId.Value;

            if (dto.Endereco is not null)
                lojaExistente.Endereco = CriarEndereco(dto.Endereco);

            await _lojaServico.AtualizarAsync(lojaExistente);
            return NoContent();
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Remover(Guid id)
        {
            var lojaExistente = await _lojaServico.ObterPorIdAsync(id);
            if (lojaExistente is null)
                return NotFound("Loja não encontrada.");

            await _lojaServico.RemoverAsync(id);
            return NoContent();
        }

        private static Endereco CriarEndereco(EnderecoDto? endereco)
        {
            if (endereco is null)
                return new Endereco();

            return new Endereco
            {
                Cep = endereco.Cep,
                Logradouro = endereco.Logradouro,
                Numero = endereco.Numero,
                Bairro = endereco.Bairro,
                Cidade = endereco.Cidade,
                Estado = endereco.Estado,
                Latitude = endereco.Latitude,
                Longitude = endereco.Longitude
            };
        }
    }
}
