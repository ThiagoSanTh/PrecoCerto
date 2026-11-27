import test from 'node:test';
import assert from 'node:assert/strict';
import {
  AUTH_REQUIRED_MESSAGE,
  isGuestTabAllowed,
  redirecionarParaCadastro,
} from '../../navigation/authGate.js';

test('isGuestTabAllowed permite apenas Buscar e bloqueia demais abas', () => {
  assert.equal(isGuestTabAllowed('Buscar'), true);
  for (const tab of ['IA', 'Favoritos', 'Histórico', 'Mensagens', 'Perfil', 'Painel', 'Produtos']) {
    assert.equal(isGuestTabAllowed(tab), false);
  }
});

test('redirecionarParaCadastro navega para Cadastro com mensagem padrão', () => {
  const calls = [];
  const nav = { navigate: (name, params) => calls.push({ name, params }) };

  redirecionarParaCadastro(nav);

  assert.deepEqual(calls, [
    { name: 'Cadastro', params: { authNotice: AUTH_REQUIRED_MESSAGE } },
  ]);
});
