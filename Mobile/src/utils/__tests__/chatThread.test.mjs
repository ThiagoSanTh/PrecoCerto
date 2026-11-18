import test from 'node:test';
import assert from 'node:assert/strict';
import {
  enterEnviaMensagem,
  instanteMensagem,
  itensComSeparadorDeDia,
  ordenarMensagens,
  valorAtualDoInput,
} from '../chatThread.js';

test('ordena por instante e separa o dia, tratando UTC sem sufixo', () => {
  const msgs = [
    { codigoPublico: 'b', enviadaEm: '2026-11-17T23:00:00Z', texto: 'noite' },
    { codigoPublico: 'a', enviadaEm: '2026-11-17T00:24:00', texto: 'cedo' },
  ];
  assert.ok(instanteMensagem(msgs[1]) < instanteMensagem(msgs[0]));
  const ordem = ordenarMensagens(msgs).map((m) => m.codigoPublico);
  assert.deepEqual(ordem, ['a', 'b']);
  const itens = itensComSeparadorDeDia(msgs, new Date('2026-11-18T12:00:00Z'));
  assert.equal(itens[0].tipo, 'dia');
  assert.equal(itens.filter((i) => i.tipo === 'msg').length, 2);
});

test('Enter não envia durante composição de IME e lê o valor do input', () => {
  assert.equal(enterEnviaMensagem({ key: 'Enter', shiftKey: false }), true);
  assert.equal(enterEnviaMensagem({ key: 'Enter', isComposing: true }), false);
  assert.equal(enterEnviaMensagem({ key: 'Enter', keyCode: 229 }), false);
  assert.equal(enterEnviaMensagem({ key: 'Enter', shiftKey: true }), false);
  assert.equal(valorAtualDoInput('[Q', '[QA teste] mensagem de teste'), '[QA teste] mensagem de teste');
});
