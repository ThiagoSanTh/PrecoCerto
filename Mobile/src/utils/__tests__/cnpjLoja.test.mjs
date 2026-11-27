import test from 'node:test';
import assert from 'node:assert/strict';
import {
  FRASES,
  FRASE_GENERICA,
  FRASE_LIMITE,
  mapearRespostaCnpj,
  mensagemErroConsultaCnpj,
  montarPayloadCriarLoja,
  planejarPreenchimento,
  validarDadosEndereco,
  validarDadosLoja,
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

test('validarDadosLoja valida nome fantasia (>=2), CNPJ, telefone e e-mail', () => {
  const cnpjValido = '11.222.333/0001-81';

  assert.equal(validarDadosLoja({ nomeFantasia: '', cnpj: cnpjValido })?.title, 'Loja');
  assert.equal(validarDadosLoja({ nomeFantasia: 'A', cnpj: cnpjValido })?.title, 'Loja');
  assert.equal(validarDadosLoja({ nomeFantasia: 'Padaria', cnpj: '' })?.title, 'Loja');
  assert.equal(validarDadosLoja({ nomeFantasia: 'Padaria', cnpj: '11111111111111' })?.title, 'Loja');
  assert.equal(
    validarDadosLoja({ nomeFantasia: 'Padaria', cnpj: cnpjValido, telefone: '123' })?.title,
    'Loja'
  );
  assert.equal(
    validarDadosLoja({ nomeFantasia: 'Padaria', cnpj: cnpjValido, email: 'invalido' })?.title,
    'Loja'
  );
  assert.equal(
    validarDadosLoja({
      nomeFantasia: 'Padaria Central',
      cnpj: cnpjValido,
      telefone: '(22) 99999-9999',
      email: 'contato@padaria.com',
    }),
    null
  );
});

test('validarDadosEndereco exige CEP de 8 dígitos, campos obrigatórios e UF com 2 letras', () => {
  assert.equal(
    validarDadosEndereco({ cep: '28990', logradouro: 'Rua A', numero: '10', cidade: 'Saquarema', estado: 'RJ' })?.title,
    'Endereço'
  );
  assert.equal(
    validarDadosEndereco({ cep: '28990-000', logradouro: 'Rua A', numero: '10', cidade: 'Saquarema', estado: 'R' })?.title,
    'Endereço'
  );
  assert.equal(
    validarDadosEndereco({ cep: '28990-000', logradouro: 'Rua A', numero: '10', cidade: 'Saquarema', estado: 'rj' }),
    null
  );
});

test('montarPayloadCriarLoja normaliza CNPJ, e-mail opcional, bairro padrão, UF e coordenadas', () => {
  const payload = montarPayloadCriarLoja({
    nomeFantasia: '  Mercado Central ',
    razaoSocial: '  ',
    cnpj: '11.222.333/0001-81',
    telefone: ' (22) 99999-9999 ',
    emailLoja: '   ',
    sessionEmail: 'dono@empresa.com',
    endereco: {
      cep: '28990-000',
      logradouro: ' Rua das Flores ',
      numero: ' 120 ',
      bairro: ' ',
      cidade: ' Saquarema ',
      estado: 'rj',
      latitude: -22.92,
      longitude: -42.51,
    },
  });

  assert.deepEqual(payload, {
    nomeFantasia: 'Mercado Central',
    razaoSocial: null,
    cnpj: '11222333000181',
    telefone: '(22) 99999-9999',
    email: 'dono@empresa.com',
    endereco: {
      cep: '28990000',
      logradouro: 'Rua das Flores',
      numero: '120',
      bairro: 'Centro',
      cidade: 'Saquarema',
      estado: 'RJ',
      latitude: -22.92,
      longitude: -42.51,
    },
  });

  const semEmailNemCoords = montarPayloadCriarLoja({
    nomeFantasia: 'Loja X',
    cnpj: '11.222.333/0001-81',
    emailLoja: '',
    sessionEmail: '',
    endereco: {
      cep: '28990000',
      logradouro: 'Av 1',
      numero: '1',
      bairro: 'Bacaxá',
      cidade: 'Saquarema',
      estado: 'RJ',
      latitude: Number.NaN,
      longitude: null,
    },
  });

  assert.equal(semEmailNemCoords.email, null);
  assert.equal(semEmailNemCoords.endereco.latitude, null);
  assert.equal(semEmailNemCoords.endereco.longitude, null);
});

