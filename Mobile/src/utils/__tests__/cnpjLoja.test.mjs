import test from 'node:test';
import assert from 'node:assert/strict';
import {
  FRASES,
  FRASE_GENERICA,
  FRASE_LIMITE,
  mapearRespostaCnpj,
  mensagemErroConsultaCnpj,
  planejarPreenchimento,
} from '../cnpjLoja.js';

test('mapeia completa, parcial, antiga e endereco null, com cep e telefone mascarados', () => {
  assert.deepEqual(mapearRespostaCnpj({
    razaoSocial: 'Padaria São Jorge Ltda',
    nomeFantasia: 'Padaria São Jorge',
    situacaoCadastral: 'ATIVA',
    ativo: true,
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
  }), {
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

  assert.deepEqual(mapearRespostaCnpj({
    razaoSocial: 'Só Razão Ltda',
    nomeFantasia: null,
    endereco: { logradouro: 'PAULISTA', numero: null, cidade: 'Rio', uf: null, cep: null },
    email: null,
    telefone: '2133334444',
  }), {
    nomeFantasia: 'Só Razão Ltda',
    razaoSocial: 'Só Razão Ltda',
    logradouro: 'PAULISTA',
    cidade: 'Rio',
    telefone: '(21) 3333-4444',
  });

  assert.deepEqual(
    mapearRespostaCnpj({ razaoSocial: 'Antiga Ltda', nomeFantasia: 'Antiga', situacao: 'ATIVA' }),
    { nomeFantasia: 'Antiga', razaoSocial: 'Antiga Ltda' }
  );

  assert.deepEqual(mapearRespostaCnpj({
    nomeFantasia: 'Sem Endereço',
    endereco: null,
    email: 'a@loja.test',
    telefone: null,
  }), { nomeFantasia: 'Sem Endereço', email: 'a@loja.test' });
});

test('campo preenchido vira conflito e campo vazio é preenchido', () => {
  const plano = planejarPreenchimento(
    { nomeFantasia: 'Outra', telefone: '21987654321' },
    { nomeFantasia: 'Minha Loja', telefone: '' }
  );
  assert.equal(plano.conflitos.nomeFantasia, 'Outra');
  assert.equal(plano.vazios.telefone, '(21) 98765-4321');
});

const STATUS = {
  cnpj_invalido: 400,
  cnpj_nao_encontrado: 404,
  cnpj_inativo: 422,
  cnpj_fonte_indisponivel: 503,
};

for (const [code, frase] of Object.entries(FRASES)) {
  test(`${code} vira a frase fixa e ignora o detail`, () => {
    const msg = mensagemErroConsultaCnpj({
      response: {
        status: STATUS[code],
        data: { title: 't', detail: 'CNPJ com situação cadastral: Baixada.', code },
        headers: { 'x-error-code': 'outro' },
      },
    });
    assert.equal(msg, frase);
    assert.equal(msg.includes('Baixada'), false);
  });
}

test('sem code no corpo usa o header; 429 e o resto usam frases próprias', () => {
  assert.equal(mensagemErroConsultaCnpj({
    response: { status: 422, data: { detail: 'Baixada' }, headers: { 'x-error-code': 'cnpj_inativo' } },
  }), FRASES.cnpj_inativo);
  assert.equal(mensagemErroConsultaCnpj({
    response: { status: 429, data: { detail: 'slow down' }, headers: {} },
  }), FRASE_LIMITE);
  assert.equal(mensagemErroConsultaCnpj({ request: {} }), FRASE_GENERICA);
});
