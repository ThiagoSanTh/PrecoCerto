import test from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { executarCriacaoLoja } from '../cnpjLoja.js';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const MOBILE_SRC = path.resolve(__dirname, '..', '..');

const CNPJ_VALIDO = '11.222.333/0001-81';

function dadosValidos(override = {}) {
  return {
    nomeFantasia: 'Mercado Central',
    razaoSocial: 'Mercado Central Ltda',
    cnpj: CNPJ_VALIDO,
    telefone: '(22) 99999-9999',
    emailLoja: '',
    endereco: {
      cep: '28990-000',
      logradouro: 'Rua das Flores',
      numero: '120',
      bairro: '',
      cidade: 'Saquarema',
      estado: 'rj',
      latitude: -22.92,
      longitude: -42.51,
      ...(override.endereco || {}),
    },
    ...override,
    ...(override.endereco
      ? {
          endereco: {
            cep: '28990-000',
            logradouro: 'Rua das Flores',
            numero: '120',
            bairro: '',
            cidade: 'Saquarema',
            estado: 'rj',
            latitude: -22.92,
            longitude: -42.51,
            ...override.endereco,
          },
        }
      : {}),
  };
}

const SESSION_CLIENTE = {
  tipo: 'cliente',
  perfil: { id: 'user-1', email: 'dono@loja.com' },
};

test('issue #97: criação de loja com sucesso normaliza payload e finaliza para Login com aviso', async () => {
  const eventos = [];
  let payloadEnviado = null;

  const resultado = await executarCriacaoLoja({
    dados: dadosValidos(),
    session: SESSION_CLIENTE,
    garantirCnpjNaReceita: async () => {
      eventos.push('cnpj_ok');
      return true;
    },
    resolverCoordenadas: async () => {
      throw new Error('não deveria geocodificar quando já há coordenadas');
    },
    criarLojaFn: async (payload) => {
      eventos.push('api_post_lojas');
      payloadEnviado = payload;
      return { id: 'loja-1' };
    },
    concluirParaLogin: async (notice) => {
      eventos.push({ acao: 'concluir_login', notice });
    },
  });

  assert.equal(resultado.status, 'created');
  assert.equal(resultado.notice.variant, 'success');
  assert.equal(resultado.notice.title, 'Loja criada!');
  assert.equal(payloadEnviado.cnpj, '11222333000181');
  assert.equal(payloadEnviado.email, 'dono@loja.com');
  assert.equal(payloadEnviado.endereco.cep, '28990000');
  assert.equal(payloadEnviado.endereco.bairro, 'Centro');
  assert.equal(payloadEnviado.endereco.estado, 'RJ');
  assert.equal(eventos[0], 'cnpj_ok');
  assert.equal(eventos[1], 'api_post_lojas');
  assert.equal(eventos[2].acao, 'concluir_login');
});

test('issue #97: HTTP 409 finaliza para Login preservando a mensagem específica de conflito de loja', async () => {
  let noticeRecebido = null;
  const msgBackend =
    'Você já possui uma loja cadastrada. Saia e entre novamente para acessar o painel da loja.';

  const resultado = await executarCriacaoLoja({
    dados: dadosValidos(),
    session: SESSION_CLIENTE,
    garantirCnpjNaReceita: async () => true,
    resolverCoordenadas: async () => ({ latitude: null, longitude: null }),
    criarLojaFn: async () => {
      const err = new Error('Conflict');
      err.response = { status: 409, data: { message: msgBackend } };
      throw err;
    },
    concluirParaLogin: async (notice) => {
      noticeRecebido = notice;
    },
  });

  assert.equal(resultado.status, 'conflict_completed');
  assert.equal(noticeRecebido?.title, 'Loja já cadastrada');
  assert.equal(noticeRecebido?.message, msgBackend);
  assert.equal(noticeRecebido?.variant, 'info');
});

test('issue #97: erro da API (400/500/rede) retorna feedback visível sem deslogar o usuário', async () => {
  let concluiu = false;

  const resultado = await executarCriacaoLoja({
    dados: dadosValidos(),
    session: SESSION_CLIENTE,
    garantirCnpjNaReceita: async () => true,
    resolverCoordenadas: async () => ({ latitude: null, longitude: null }),
    criarLojaFn: async () => {
      const err = new Error('Bad Request');
      err.response = { status: 400, data: { message: 'CNPJ inativo na Receita Federal.' } };
      throw err;
    },
    concluirParaLogin: async () => {
      concluiu = true;
    },
  });

  assert.equal(concluiu, false);
  assert.equal(resultado.status, 'api_error');
  assert.equal(resultado.feedback.message, 'CNPJ inativo na Receita Federal.');
});

test('issue #97: falha de validação de telefone/email/CNPJ no passo 0 volta para step 0 (não step 1)', async () => {
  let chamouApi = false;

  const resultado = await executarCriacaoLoja({
    dados: dadosValidos({ telefone: '123' }),
    session: SESSION_CLIENTE,
    garantirCnpjNaReceita: async () => true,
    resolverCoordenadas: async () => ({ latitude: null, longitude: null }),
    criarLojaFn: async () => {
      chamouApi = true;
    },
    concluirParaLogin: async () => {},
  });

  assert.equal(chamouApi, false);
  assert.equal(resultado.status, 'validation_error');
  assert.equal(resultado.step, 0);
  assert.equal(resultado.feedback.title, 'Loja');
});

test('issue #97: falha na geocodificação automática não bloqueia a criação da loja', async () => {
  let payloadEnviado = null;
  let concluiu = false;

  const resultado = await executarCriacaoLoja({
    dados: dadosValidos({ endereco: { latitude: null, longitude: null } }),
    session: SESSION_CLIENTE,
    garantirCnpjNaReceita: async () => true,
    resolverCoordenadas: async () => {
      throw new Error('Nominatim timeout');
    },
    criarLojaFn: async (payload) => {
      payloadEnviado = payload;
    },
    concluirParaLogin: async () => {
      concluiu = true;
    },
  });

  assert.equal(resultado.status, 'created');
  assert.equal(concluiu, true);
  assert.equal(payloadEnviado.endereco.latitude, null);
  assert.equal(payloadEnviado.endereco.longitude, null);
});

test('guarda de regressão #97: CreateStoreScreen não usa Alert.alert (no-op no Web) e exibe feedback no FormScreen', () => {
  const createStoreSource = fs.readFileSync(
    path.join(MOBILE_SRC, 'screens/store/CreateStoreScreen.js'),
    'utf8'
  );
  const loginSource = fs.readFileSync(
    path.join(MOBILE_SRC, 'screens/auth/LoginScreen.js'),
    'utf8'
  );
  const formScreenSource = fs.readFileSync(
    path.join(MOBILE_SRC, 'components/form/FormScreen.js'),
    'utf8'
  );

  const createStoreSemComentarios = createStoreSource
    .replace(/\/\*[\s\S]*?\*\//g, '')
    .replace(/\/\/.*$/gm, '');

  // Alert.alert é no-op no react-native-web e impediu o botão Criar loja de finalizar (#97).
  assert.doesNotMatch(createStoreSemComentarios, /\bAlert\.alert\b/);
  assert.match(createStoreSource, /error=\{feedback\}/);
  assert.match(createStoreSource, /executarCriacaoLoja\(/);
  assert.match(createStoreSource, /clearPendingAuthAction\(\)/);
  // Navega para Login antes do logout() para o guard de rotas não apagar route.params.authNotice.
  const idxReset = createStoreSource.indexOf("navigation.reset(");
  const idxLogout = createStoreSource.indexOf("await logout()");
  assert.ok(idxReset > -1 && idxLogout > idxReset, 'navigation.reset deve rodar antes de await logout()');

  assert.match(loginSource, /route\?\.params\?\.authNotice/);
  assert.match(formScreenSource, /scrollTo\?\.\(\{\s*y:\s*0/);
});
