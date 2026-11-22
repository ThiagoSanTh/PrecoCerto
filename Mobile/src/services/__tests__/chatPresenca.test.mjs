import test, { mock } from 'node:test';
import assert from 'node:assert/strict';
import {
  HEARTBEAT_MS,
  calcularDesde,
  createPresencaCiclo,
  enviarFecharKeepalive,
  enviarPresenca,
} from '../chatPresenca.js';

test('desde usa o instante mais recente entre a última checagem e o fechamento', () => {
  assert.equal(
    calcularDesde({
      ultimoCheckEm: '2026-11-20T12:00:00.000Z',
      chatFechadoEm: '2026-11-20T15:04:05.000Z',
    }),
    '2026-11-20T15:04:05.000Z'
  );
  assert.equal(
    calcularDesde({
      ultimoCheckEm: '2026-11-21T08:00:00.000Z',
      chatFechadoEm: '2026-11-20T15:04:05.000Z',
    }),
    '2026-11-21T08:00:00.000Z'
  );
});

test('desde ignora valor ausente ou inválido e omite a query se não houver marco', () => {
  assert.equal(calcularDesde(null), null);
  assert.equal(calcularDesde({}), null);
  assert.equal(
    calcularDesde({ ultimoCheckEm: null, chatFechadoEm: '2026-11-20T15:04:05.000Z' }),
    '2026-11-20T15:04:05.000Z'
  );
  assert.equal(
    calcularDesde({ ultimoCheckEm: 'nao-e-data', chatFechadoEm: '2026-11-20T15:04:05.000Z' }),
    '2026-11-20T15:04:05.000Z'
  );
  assert.equal(calcularDesde({ ultimoCheckEm: 'ainda-nao', chatFechadoEm: null }), null);
});

function cicloFake(extra = {}) {
  const chamadas = [];
  const ciclo = createPresencaCiclo({
    abrir: () => {
      chamadas.push('abrir');
      return true;
    },
    fechar: ({ keepalive }) => {
      chamadas.push(keepalive ? 'fechar:keepalive' : 'fechar');
      return true;
    },
    ...extra,
  });
  return { ciclo, chamadas };
}

async function micro() {
  await Promise.resolve();
  await Promise.resolve();
}

test('foco abre uma vez, blur da última tela fecha, e o meio do caminho não duplica', async (t) => {
  t.mock.timers.enable({ apis: ['setTimeout', 'setInterval'] });
  const { ciclo, chamadas } = cicloFake();

  ciclo.foco();
  ciclo.foco();
  await micro();
  assert.deepEqual(chamadas, ['abrir']);

  ciclo.blur();
  await micro();
  t.mock.timers.tick(0);
  assert.deepEqual(chamadas, ['abrir']);

  ciclo.blur();
  ciclo.foco();
  t.mock.timers.tick(0);
  await micro();
  assert.deepEqual(chamadas, ['abrir']);

  ciclo.blur();
  t.mock.timers.tick(0);
  await micro();
  assert.deepEqual(chamadas, ['abrir', 'fechar']);
});

test('background fecha na hora e, ao voltar com a tela focada, abre de novo', async (t) => {
  t.mock.timers.enable({ apis: ['setTimeout', 'setInterval'] });
  const { ciclo, chamadas } = cicloFake();

  ciclo.background();
  assert.deepEqual(chamadas, []);
  ciclo.foreground();

  ciclo.foco();
  await micro();
  ciclo.background();
  ciclo.background();
  assert.deepEqual(chamadas, ['abrir', 'fechar']);

  ciclo.foreground();
  await micro();
  assert.deepEqual(chamadas, ['abrir', 'fechar', 'abrir']);
});

test('esconder a aba adia o fechar; pagehide manda um keepalive e não o POST normal', async (t) => {
  t.mock.timers.enable({ apis: ['setTimeout', 'setInterval'] });
  const { ciclo, chamadas } = cicloFake();

  ciclo.foco();
  await micro();
  ciclo.ocultar();
  ciclo.pagehide();
  t.mock.timers.tick(0);
  await micro();
  assert.deepEqual(chamadas, ['abrir', 'fechar:keepalive']);
});

test('trocar de aba sem descarregar a página fecha com POST normal', async (t) => {
  t.mock.timers.enable({ apis: ['setTimeout', 'setInterval'] });
  const { ciclo, chamadas } = cicloFake();

  ciclo.foco();
  await micro();
  ciclo.ocultar();
  t.mock.timers.tick(0);
  await micro();
  assert.deepEqual(chamadas, ['abrir', 'fechar']);

  ciclo.foreground();
  await micro();
  assert.deepEqual(chamadas, ['abrir', 'fechar', 'abrir']);
});

test('heartbeat repete abrir a cada 2 min e para no blur, no background e ao desmontar', async (t) => {
  t.mock.timers.enable({ apis: ['setTimeout', 'setInterval'] });
  assert.equal(HEARTBEAT_MS, 120_000);
  const { ciclo, chamadas } = cicloFake();

  ciclo.foco();
  await micro();
  t.mock.timers.tick(HEARTBEAT_MS - 1);
  assert.deepEqual(chamadas, ['abrir']);
  t.mock.timers.tick(1);
  await micro();
  assert.deepEqual(chamadas, ['abrir', 'abrir']);

  ciclo.blur();
  t.mock.timers.tick(0);
  t.mock.timers.tick(HEARTBEAT_MS);
  await micro();
  assert.deepEqual(chamadas, ['abrir', 'abrir', 'fechar']);

  ciclo.foco();
  await micro();
  ciclo.background();
  t.mock.timers.tick(HEARTBEAT_MS * 2);
  await micro();
  assert.deepEqual(chamadas, ['abrir', 'abrir', 'fechar', 'abrir', 'fechar']);

  ciclo.foreground();
  await micro();
  ciclo.reset();
  t.mock.timers.tick(HEARTBEAT_MS);
  await micro();
  assert.deepEqual(chamadas, ['abrir', 'abrir', 'fechar', 'abrir', 'fechar', 'abrir', 'fechar']);
});

test('401 ou 404 desliga a presença e o chat segue sem novas chamadas', async (t) => {
  t.mock.timers.enable({ apis: ['setTimeout', 'setInterval'] });
  const chamadas = [];
  const ciclo = createPresencaCiclo({
    abrir: () => {
      chamadas.push('abrir');
      return false;
    },
    fechar: () => {
      chamadas.push('fechar');
      return false;
    },
  });

  ciclo.foco();
  await micro();
  t.mock.timers.tick(HEARTBEAT_MS);
  ciclo.blur();
  t.mock.timers.tick(0);
  ciclo.foco();
  await micro();
  assert.deepEqual(chamadas, ['abrir']);

  ciclo.marcarSemSuporte();
  ciclo.reconciliar(true);
  t.mock.timers.tick(0);
  assert.deepEqual(chamadas, ['abrir']);
});

test('estado aberto no boot fecha se a tela não está em foco, sem abrir de novo se o foco chegar antes', async (t) => {
  t.mock.timers.enable({ apis: ['setTimeout', 'setInterval'] });
  const { ciclo, chamadas } = cicloFake();

  ciclo.reconciliar(true);
  t.mock.timers.tick(0);
  await micro();
  assert.deepEqual(chamadas, ['fechar']);

  const outro = cicloFake();
  outro.ciclo.reconciliar(true);
  outro.ciclo.foco();
  t.mock.timers.tick(0);
  await micro();
  assert.deepEqual(outro.chamadas, []);
  t.mock.timers.tick(HEARTBEAT_MS);
  await micro();
  assert.deepEqual(outro.chamadas, ['abrir']);
});

test('POST de presença não tem corpo nem Content-Type; pagehide usa fetch keepalive com Bearer', async () => {
  const pedidos = [];
  const original = globalThis.fetch;
  let beaconAnterior;
  globalThis.fetch = (url, init) => {
    pedidos.push({ url, init });
    return Promise.resolve(new Response(null, { status: 204 }));
  };
  try {
    assert.equal(await enviarPresenca('http://api/chat/abrir', ''), null);
    assert.equal(pedidos.length, 0);

    assert.equal(await enviarPresenca('http://api/chat/abrir', 'tok'), true);
    assert.equal(pedidos[0].init.method, 'POST');
    assert.equal(pedidos[0].init.body, undefined);
    assert.deepEqual(pedidos[0].init.headers, { Authorization: 'Bearer tok' });
    assert.equal(pedidos[0].init.keepalive, false);

    globalThis.fetch = (url, init) => {
      pedidos.push({ url, init });
      return Promise.resolve(new Response(null, { status: 404 }));
    };
    assert.equal(await enviarPresenca('http://api/chat/estado', 'tok', { method: 'GET' }), false);

    globalThis.fetch = (url, init) => {
      pedidos.push({ url, init });
      return Promise.resolve(
        new Response(JSON.stringify({ chatAberto: false, chatFechadoEm: '2026-11-20T15:04:05.000Z' }), {
          status: 200,
          headers: { 'Content-Type': 'application/json' },
        })
      );
    };
    assert.deepEqual(await enviarPresenca('http://api/chat/estado', 'tok', { method: 'GET' }), {
      chatAberto: false,
      chatFechadoEm: '2026-11-20T15:04:05.000Z',
    });

    const beacon = mock.fn();
    beaconAnterior = Object.getOwnPropertyDescriptor(globalThis.navigator, 'sendBeacon');
    Object.defineProperty(globalThis.navigator, 'sendBeacon', {
      configurable: true,
      writable: true,
      value: beacon,
    });
    const keepalive = [];
    globalThis.fetch = (url, init) => {
      keepalive.push({ url, init });
      return Promise.resolve(new Response(null, { status: 204 }));
    };
    assert.equal(enviarFecharKeepalive('http://api/chat/fechar', 'tok'), true);
    assert.equal(beacon.mock.calls.length, 0);
    assert.equal(keepalive[0].init.keepalive, true);
    assert.equal(keepalive[0].init.body, undefined);
    assert.deepEqual(keepalive[0].init.headers, { Authorization: 'Bearer tok' });
    assert.equal(keepalive[0].init.method, 'POST');
  } finally {
    globalThis.fetch = original;
    if (beaconAnterior) {
      Object.defineProperty(globalThis.navigator, 'sendBeacon', beaconAnterior);
    } else {
      delete globalThis.navigator.sendBeacon;
    }
  }
});
