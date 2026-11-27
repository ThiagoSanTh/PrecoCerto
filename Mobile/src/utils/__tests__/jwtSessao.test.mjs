import test from 'node:test';
import assert from 'node:assert/strict';
import { tokenInutilizavel } from '../jwtSessao.js';

function jwt(payload) {
  const parte = (valor) => Buffer.from(JSON.stringify(valor)).toString('base64url');
  return `${parte({ alg: 'none', typ: 'JWT' })}.${parte(payload)}.assinatura`;
}

const agora = Date.parse('2026-11-18T00:00:00Z');

test('token malformado ou com exp vencido não autentica o boot', () => {
  assert.equal(tokenInutilizavel('lixo', agora), true);
  assert.equal(tokenInutilizavel('a.b', agora), true);
  assert.equal(tokenInutilizavel('a.b.c', agora), true);
  assert.equal(tokenInutilizavel(jwt({ exp: 'nao-numero' }), agora), true);
  assert.equal(tokenInutilizavel(jwt([]), agora), true);
  assert.equal(tokenInutilizavel(jwt({ exp: Math.floor(agora / 1000) - 5 }), agora), true);
  assert.equal(tokenInutilizavel(jwt({ exp: Math.floor(agora / 1000) }), agora), true);
  assert.equal(tokenInutilizavel(jwt({ sub: 'cli-1', exp: Math.floor(agora / 1000) + 60 }), agora), false);
  assert.equal(tokenInutilizavel(jwt({ sub: 'cli-1' }), agora), false);
});
