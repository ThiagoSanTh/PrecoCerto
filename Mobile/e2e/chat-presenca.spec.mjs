import { test, expect } from '@playwright/test';

const FECHADO_EM = '2026-11-20T15:04:05.000Z';

function tokenDeTeste() {
  const parte = (valor) => Buffer.from(JSON.stringify(valor)).toString('base64url');
  const exp = Math.floor(Date.now() / 1000) + 60 * 60;
  return `${parte({ alg: 'none', typ: 'JWT' })}.${parte({ sub: 'user-1', exp })}.assinatura`;
}

function caminho(url) {
  const parsed = new URL(url);
  return `${parsed.pathname}${parsed.search}`;
}

test('presença do chat: entrar, sair, trocar de aba, esconder e heartbeat', async ({ page }) => {
  const sequencia = [];

  await page.addInitScript(() => {
    const original = window.setInterval.bind(window);
    const originalClear = window.clearInterval.bind(window);
    window.__chatHeartbeat = null;
    window.__chatHeartbeatId = null;
    window.setInterval = (fn, ms, ...rest) => {
      const id = original(fn, ms, ...rest);
      if (ms === 120000) {
        window.__chatHeartbeat = fn;
        window.__chatHeartbeatId = id;
      }
      return id;
    };
    window.clearInterval = (id) => {
      if (id != null && id === window.__chatHeartbeatId) window.__chatHeartbeat = null;
      return originalClear(id);
    };
  });

  await page.route('**/*', async (route) => {
    const url = route.request().url();
    if (!url.includes('/api/')) {
      await route.continue();
      return;
    }
    const path = new URL(url).pathname;
    if (path.endsWith('/Conversas/chat/estado')) {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ chatAberto: false, chatFechadoEm: FECHADO_EM }),
      });
      return;
    }
    if (path.endsWith('/Conversas/chat/abrir') || path.endsWith('/Conversas/chat/fechar')) {
      await route.fulfill({ status: 204, body: '' });
      return;
    }
    if (path.endsWith('/Conversas/tem-novas')) {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ temNovas: false }),
      });
      return;
    }
    if (path.endsWith('/Conversas')) {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([
          {
            codigoPublico: 'conv-1',
            nomeContato: 'Mercado Central',
            ultimaMensagemTexto: 'Tem tomate hoje?',
            ultimaMensagemEm: '2026-11-22T12:00:00.000Z',
            naoLidas: 1,
          },
        ]),
      });
      return;
    }
    if (/\/Conversas\/[^/]+\/mensagens$/.test(path)) {
      await route.fulfill({ status: 200, contentType: 'application/json', body: '[]' });
      return;
    }
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({ items: [], page: 1, pageSize: 20, total: 0, hasNext: false }),
    });
  });

  page.on('request', (request) => {
    const url = request.url();
    if (!url.includes('/api/Conversas')) return;
    sequencia.push({
      method: request.method(),
      path: caminho(url),
      contentType: request.headers()['content-type'] || null,
      bearer: (request.headers().authorization || '').startsWith('Bearer '),
      body: request.postData() ?? null,
    });
  });

  await page.addInitScript(
    ({ token, session }) => {
      localStorage.setItem('@auth_token', token);
      localStorage.setItem('@session', JSON.stringify(session));
      localStorage.setItem('@userMode', 'user');
      localStorage.setItem('@sw-migration-v3', 'done');
    },
    {
      token: tokenDeTeste(),
      session: {
        tipo: 'cliente',
        perfil: { id: 'user-1', codigoPublico: 'cli-1', nomeUsuario: 'Ana' },
      },
    }
  );

  const aba = (nome) => page.getByRole('button', { name: new RegExp(nome) });

  try {
  await page.goto('/');
  await expect(aba('Mensagens')).toBeVisible();
  await expect.poll(() => sequencia.some((item) => item.path.includes('/chat/estado'))).toBe(true);
  await expect.poll(() => sequencia.some((item) => item.path.includes('/tem-novas'))).toBe(true);

  const novas = sequencia.find((item) => item.path.includes('/tem-novas'));
  expect(decodeURIComponent(novas.path)).toContain(`desde=${FECHADO_EM}`);
  expect(sequencia.some((item) => item.path.includes('/chat/abrir'))).toBe(false);

  await aba('Mensagens').click();
  await expect(page.getByText('Mercado Central')).toBeVisible();
  await expect.poll(() => sequencia.filter((item) => item.path.endsWith('/chat/abrir')).length).toBe(1);

  await page.getByText('Mercado Central').click();
  await expect(page.getByLabel('Voltar para conversas')).toBeVisible();
  await page.waitForTimeout(50);
  expect(sequencia.filter((item) => item.path.endsWith('/chat/abrir')).length).toBe(1);
  expect(sequencia.some((item) => item.path.endsWith('/chat/fechar'))).toBe(false);

  await page.getByLabel('Voltar para conversas').click();
  await expect(page.getByText('Mercado Central')).toBeVisible();
  await page.waitForTimeout(50);
  expect(sequencia.some((item) => item.path.endsWith('/chat/fechar'))).toBe(false);

  await aba('Buscar').click();
  await expect.poll(() => sequencia.filter((item) => item.path.endsWith('/chat/fechar')).length).toBe(1);

  await aba('Mensagens').click();
  await expect(page.getByText('Mercado Central')).toBeVisible();
  await page.getByText('Mercado Central').click();
  await expect(page.getByLabel('Voltar para conversas')).toBeVisible();
  await expect.poll(() => sequencia.filter((item) => item.path.endsWith('/chat/abrir')).length).toBe(2);

  await page.evaluate(() => {
    Object.defineProperty(document, 'visibilityState', {
      configurable: true,
      get: () => 'hidden',
    });
    document.dispatchEvent(new Event('visibilitychange'));
  });
  await expect.poll(() => sequencia.filter((item) => item.path.endsWith('/chat/fechar')).length).toBe(2);

  await page.evaluate(() => {
    Object.defineProperty(document, 'visibilityState', {
      configurable: true,
      get: () => 'visible',
    });
    document.dispatchEvent(new Event('visibilitychange'));
  });
  await expect.poll(() => sequencia.filter((item) => item.path.endsWith('/chat/abrir')).length).toBe(3);

  const antesDoPulso = sequencia.filter((item) => item.path.endsWith('/chat/abrir')).length;
  const pulsou = await page.evaluate(() => {
    if (typeof window.__chatHeartbeat !== 'function') return false;
    window.__chatHeartbeat();
    return true;
  });
  expect(pulsou).toBe(true);
  await expect.poll(() => sequencia.filter((item) => item.path.endsWith('/chat/abrir')).length).toBe(antesDoPulso + 1);

  await page.evaluate(() => {
    window.dispatchEvent(new Event('pagehide'));
  });
  await expect.poll(() => sequencia.filter((item) => item.path.endsWith('/chat/fechar')).length).toBe(3);

  for (const item of sequencia) {
    if (!item.path.includes('/chat/abrir') && !item.path.includes('/chat/fechar')) continue;
    expect(item.method).toBe('POST');
    expect(item.body).toBeNull();
    expect(item.contentType).toBeNull();
    expect(item.bearer).toBe(true);
  }

  console.log(`SEQUENCIA ${JSON.stringify(sequencia)}`);
  } finally {
    console.log(`SEQUENCIA ${JSON.stringify(sequencia)}`);
  }
});
