import { test } from 'node:test';
import assert from 'node:assert/strict';
import {
  MODO_CLIENTE,
  MODO_LOJA,
  CONTEXTO_CLIENTE,
  CONTEXTO_LOJA,
  podeUsarModoLoja,
  modoPadraoParaSessao,
  resolverModoSalvo,
  emModoCliente,
  emModoLoja,
  clienteIdDaSessao,
  funcionalidadesClienteAtivas,
  funcionalidadesLojistaAtivas,
  sincronizarContextoOperacional,
  getContextoOperacional,
} from './modoUsuario.js';

function sessaoCliente() {
  return { tipo: 'cliente', perfil: { id: 'c1', lojaId: null } };
}

function sessaoLojista() {
  return { tipo: 'lojista', perfil: { id: 'l1', lojaId: 'loja-1' } };
}

test('cenário 1: usuário sem loja em modo cliente tem cliente completo', () => {
  const session = sessaoCliente();
  assert.equal(emModoCliente(MODO_CLIENTE, session), true);
  assert.equal(funcionalidadesClienteAtivas(session, MODO_CLIENTE), true);
  assert.equal(funcionalidadesLojistaAtivas(session, MODO_CLIENTE), false);
  assert.equal(clienteIdDaSessao(session, MODO_CLIENTE), 'c1');
  assert.equal(podeUsarModoLoja(session), false);
});

test('cenário 2: usuário com loja em modo cliente tem cliente completo', () => {
  const session = sessaoLojista();
  assert.equal(emModoCliente(MODO_CLIENTE, session), true);
  assert.equal(funcionalidadesClienteAtivas(session, MODO_CLIENTE), true);
  assert.equal(funcionalidadesLojistaAtivas(session, MODO_CLIENTE), false);
  assert.equal(clienteIdDaSessao(session, MODO_CLIENTE), 'l1');
  assert.equal(podeUsarModoLoja(session), true);
});

test('cenário 2 equivale ao cenário 1 nas funções de cliente', () => {
  const comum = sessaoCliente();
  const lojista = sessaoLojista();
  assert.equal(
    funcionalidadesClienteAtivas(comum, MODO_CLIENTE),
    funcionalidadesClienteAtivas(lojista, MODO_CLIENTE)
  );
  assert.ok(clienteIdDaSessao(comum, MODO_CLIENTE));
  assert.ok(clienteIdDaSessao(lojista, MODO_CLIENTE));
});

test('cenário 3: usuário com loja em modo lojista tem lojista completo', () => {
  const session = sessaoLojista();
  assert.equal(emModoLoja(MODO_LOJA, session), true);
  assert.equal(funcionalidadesLojistaAtivas(session, MODO_LOJA), true);
  assert.equal(funcionalidadesClienteAtivas(session, MODO_LOJA), false);
  assert.equal(clienteIdDaSessao(session, MODO_LOJA), null);
});

test('cenário 4: troca lojista → cliente libera cliente completo', () => {
  const session = sessaoLojista();
  assert.equal(funcionalidadesLojistaAtivas(session, MODO_LOJA), true);
  assert.equal(funcionalidadesClienteAtivas(session, MODO_CLIENTE), true);
  assert.equal(clienteIdDaSessao(session, MODO_CLIENTE), 'l1');
  assert.equal(sincronizarContextoOperacional(MODO_CLIENTE, session), CONTEXTO_CLIENTE);
  assert.equal(getContextoOperacional(), CONTEXTO_CLIENTE);
});

test('cenário 5: troca cliente → lojista libera lojista completo', () => {
  const session = sessaoLojista();
  assert.equal(funcionalidadesClienteAtivas(session, MODO_CLIENTE), true);
  assert.equal(funcionalidadesLojistaAtivas(session, MODO_LOJA), true);
  assert.equal(sincronizarContextoOperacional(MODO_LOJA, session), CONTEXTO_LOJA);
  assert.equal(getContextoOperacional(), CONTEXTO_LOJA);
});

test('preferência de modo cliente é respeitada no restart mesmo com loja', () => {
  const session = sessaoLojista();
  assert.equal(resolverModoSalvo(MODO_CLIENTE, session), MODO_CLIENTE);
  assert.equal(resolverModoSalvo(MODO_LOJA, session), MODO_LOJA);
  assert.equal(modoPadraoParaSessao(session), MODO_LOJA);
});

test('usuário sem loja não entra em modo loja', () => {
  const session = sessaoCliente();
  assert.equal(resolverModoSalvo(MODO_LOJA, session), MODO_CLIENTE);
  assert.equal(emModoLoja(MODO_LOJA, session), false);
});
