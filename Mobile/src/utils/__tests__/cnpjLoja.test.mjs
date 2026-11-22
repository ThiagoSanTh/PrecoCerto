import test from 'node:test';
import assert from 'node:assert/strict';
import {
  ERROS_CNPJ,
  mapearRespostaCnpj,
  mensagemErroConsultaCnpj,
  planejarPreenchimento,
} from '../cnpjLoja.js';

const COMPLETA = {
  cnpj: '11222333000116',
  razaoSocial: 'Padaria São Jorge Ltda',
  nomeFantasia: 'Padaria São Jorge',
  situacao: 'ATIVA',
  endereco: {
    logradouro: 'RUA FULANO DE TAL',
    numero: '100',
    complemento: 'Loja 2',
    bairro: 'Centro',
    cidade: 'Saquarema',
    uf: 'rj',
    cep: '28990000',
  },
  email: 'contato@padaria.test',
  telefone: '21987654321',
};

test('resposta completa preenche os campos do formulário e mascara cep e telefone', () => {
  assert.deepEqual(mapearRespostaCnpj(COMPLETA), {
    nomeFantasia: 'Padaria São Jorge',
    razaoSocial: 'Padaria São Jorge Ltda',
    logradouro: 'RUA FULANO DE TAL',
    numero: '100',
    bairro: 'Centro',
    cidade: 'Saquarema',
    estado: 'RJ',
    cep: '28990-000',
    email: 'contato@padaria.test',
    telefone: '(21) 98765-4321',
  });
});

test('resposta parcial ignora null e usa a razão social quando não há nome fantasia', () => {
  const parcial = {
    cnpj: '11222333000116',
    razaoSocial: 'Só Razão Ltda',
    nomeFantasia: null,
    situacao: null,
    endereco: {
      logradouro: 'PAULISTA',
      numero: null,
      complemento: null,
      bairro: null,
      cidade: 'Rio',
      uf: null,
      cep: null,
    },
    email: null,
    telefone: '2133334444',
  };

  assert.deepEqual(mapearRespostaCnpj(parcial), {
    nomeFantasia: 'Só Razão Ltda',
    razaoSocial: 'Só Razão Ltda',
    logradouro: 'PAULISTA',
    cidade: 'Rio',
    telefone: '(21) 3333-4444',
  });
});

test('resposta antiga, só com os 4 campos, não inventa endereço, e-mail nem telefone', () => {
  const antiga = {
    cnpj: '11222333000116',
    razaoSocial: 'Antiga Ltda',
    nomeFantasia: 'Antiga',
    situacao: 'ATIVA',
  };

  assert.deepEqual(mapearRespostaCnpj(antiga), {
    nomeFantasia: 'Antiga',
    razaoSocial: 'Antiga Ltda',
  });
});

test('endereco null não preenche endereço e telefone só com dígitos ganha DDD', () => {
  const semEndereco = {
    cnpj: '11222333000116',
    razaoSocial: null,
    nomeFantasia: 'Sem Endereço',
    situacao: 'ATIVA',
    endereco: null,
    email: 'a@loja.test',
    telefone: '21987654321',
  };

  const campos = mapearRespostaCnpj(semEndereco);
  assert.deepEqual(campos, {
    nomeFantasia: 'Sem Endereço',
    email: 'a@loja.test',
    telefone: '(21) 98765-4321',
  });
  assert.equal('cep' in campos, false);
  assert.equal('logradouro' in campos, false);
});

test('cep e telefone nulos ou vazios ficam de fora', () => {
  const campos = mapearRespostaCnpj({
    nomeFantasia: 'Loja',
    endereco: { cep: null, logradouro: 'Rua A' },
    telefone: null,
    email: '  ',
  });

  assert.deepEqual(campos, {
    nomeFantasia: 'Loja',
    logradouro: 'Rua A',
  });
});

test('planejarPreenchimento só marca vazio e separa o que já foi digitado', () => {
  const plano = planejarPreenchimento(COMPLETA, {
    nomeFantasia: 'Minha Loja',
    razaoSocial: '',
    telefone: '(21) 98765-4321',
    email: '',
    cep: '',
    logradouro: '',
    numero: '',
    bairro: '',
    cidade: '',
    estado: '',
  });

  assert.equal(plano.vazios.nomeFantasia, undefined);
  assert.equal(plano.conflitos.nomeFantasia, 'Padaria São Jorge');
  assert.equal(plano.vazios.cep, '28990-000');
  assert.equal(plano.conflitos.telefone, undefined);
  assert.equal(plano.vazios.telefone, undefined);
});

function erroConsulta({ status, code, header, detail = 'DETALHE CRU BAIXADA INAPTA SUSPENSA' }) {
  return {
    response: {
      status,
      data: { type: 'about:blank', title: 'erro', status, detail, ...(code ? { code } : {}) },
      headers: header ? { 'x-error-code': header } : {},
    },
  };
}

function assertMensagemFixa(mensagem, esperada, cru = 'DETALHE CRU') {
  assert.equal(mensagem, esperada);
  assert.equal(mensagem.includes(cru), false);
  assert.equal(mensagem.includes('BAIXADA'), false);
}

test('400 cnpj_invalido vira a frase fixa e ignora o detail', () => {
  assertMensagemFixa(
    mensagemErroConsultaCnpj(erroConsulta({ status: 400, code: 'cnpj_invalido' })),
    ERROS_CNPJ.invalido
  );
});

test('404 cnpj_nao_encontrado vira a frase fixa e ignora o detail', () => {
  assertMensagemFixa(
    mensagemErroConsultaCnpj(erroConsulta({ status: 404, code: 'cnpj_nao_encontrado' })),
    ERROS_CNPJ.naoEncontrado
  );
});

test('422 cnpj_inativo cobre baixado, inapto e suspenso sem repetir o detail', () => {
  for (const detail of ['situação BAIXADA', 'empresa INAPTA', 'cadastro SUSPENSO']) {
    const mensagem = mensagemErroConsultaCnpj(
      erroConsulta({ status: 422, code: 'cnpj_inativo', detail })
    );
    assert.equal(mensagem, ERROS_CNPJ.inativo);
    assert.equal(mensagem.includes(detail), false);
  }
});

test('503 cnpj_fonte_indisponivel vira a frase fixa e ignora o detail', () => {
  assertMensagemFixa(
    mensagemErroConsultaCnpj(erroConsulta({ status: 503, code: 'cnpj_fonte_indisponivel' })),
    ERROS_CNPJ.indisponivel
  );
});

test('429 usa a frase de limite mesmo sem code e sem o texto da API', () => {
  assertMensagemFixa(
    mensagemErroConsultaCnpj(erroConsulta({ status: 429, detail: 'slow down' })),
    ERROS_CNPJ.limite,
    'slow down'
  );
});

test('sem code no corpo, o header X-Error-Code define a mensagem', () => {
  assert.equal(
    mensagemErroConsultaCnpj(erroConsulta({ status: 422, header: 'cnpj_inativo' })),
    ERROS_CNPJ.inativo
  );
});

test('o code do corpo ganha do header', () => {
  assert.equal(
    mensagemErroConsultaCnpj(erroConsulta({
      status: 400,
      code: 'cnpj_invalido',
      header: 'cnpj_inativo',
    })),
    ERROS_CNPJ.invalido
  );
});

test('code desconhecido, outro status ou falha de rede usam a mensagem genérica', () => {
  assertMensagemFixa(
    mensagemErroConsultaCnpj(erroConsulta({ status: 500, code: 'erro_interno' })),
    ERROS_CNPJ.generico
  );
  assert.equal(mensagemErroConsultaCnpj({ request: {}, message: 'Network Error' }), ERROS_CNPJ.generico);
  assert.equal(mensagemErroConsultaCnpj(erroConsulta({ status: 503 })), ERROS_CNPJ.generico);
});
