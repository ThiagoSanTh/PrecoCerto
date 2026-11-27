import { test, expect } from '@playwright/test';

const CNPJ_VALIDO = '11222333000181';

function tokenDeTeste() {
  const parte = (valor) => Buffer.from(JSON.stringify(valor)).toString('base64url');
  const exp = Math.floor(Date.now() / 1000) + 60 * 60;
  return `${parte({ alg: 'none', typ: 'JWT' })}.${parte({ sub: 'user-1', exp })}.assinatura`;
}

/**
 * Inicializa sessão de cliente autenticado no Web (token em sessionStorage, perfil em localStorage)
 * e stubba consultas externas para testar apenas o fluxo da tela Criar loja (#97).
 */
async function prepararSessaoEApi(page, responderPostLoja) {
  await page.addInitScript(
    ({ token, session }) => {
      sessionStorage.setItem('@auth_token', token);
      localStorage.setItem('@session', JSON.stringify(session));
      localStorage.setItem('@userMode', 'user');
      localStorage.setItem('@sw-migration-v3', 'done');
    },
    {
      token: tokenDeTeste(),
      session: {
        tipo: 'cliente',
        perfil: {
          id: 'user-1',
          codigoPublico: 'cli-1',
          nomeUsuario: 'Thiago',
          email: 'dono@empresa.com',
        },
      },
    }
  );

  let payloadEnviado = null;

  await page.route('**/*', async (route) => {
    const url = route.request().url();
    const method = route.request().method();

    if (url.includes('brasilapi.com.br/api/cep/v2/')) {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          location: { coordinates: { latitude: -22.92, longitude: -42.51 } },
        }),
      });
      return;
    }

    if (url.includes('nominatim.openstreetmap.org')) {
      await route.fulfill({ status: 200, contentType: 'application/json', body: '[]' });
      return;
    }

    if (!url.includes('/api/')) {
      await route.continue();
      return;
    }

    const path = new URL(url).pathname;

    if (path.includes(`/Consultas/cnpj/${CNPJ_VALIDO}`)) {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          cnpj: CNPJ_VALIDO,
          razaoSocial: 'Mercado Central Ltda',
          nomeFantasia: 'Mercado Central',
          situacaoCadastral: 'ATIVA',
          ativo: true,
          endereco: {
            cep: '28990000',
            logradouro: 'Rua das Flores',
            numero: '120',
            bairro: 'Centro',
            cidade: 'Saquarema',
            uf: 'RJ',
          },
          email: 'contato@mercado.com',
          telefone: '22999999999',
        }),
      });
      return;
    }

    if (path.endsWith('/Lojas') && method === 'POST') {
      payloadEnviado = route.request().postDataJSON();
      await responderPostLoja(route, payloadEnviado);
      return;
    }

    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({ items: [], page: 1, pageSize: 20, total: 0, hasNext: false }),
    });
  });

  return () => payloadEnviado;
}

async function preencherAtePassoFinal(page) {
  await page.goto('/criar-loja');
  await expect(page.getByRole('heading', { name: 'Criar loja' })).toBeVisible();

  await page.getByLabel('Nome fantasia *').fill('Mercado Central');
  await page.getByLabel('CNPJ').fill(CNPJ_VALIDO);
  await page.getByRole('button', { name: 'Continuar' }).click();

  await expect(page.getByLabel('CEP *')).toBeVisible();
  await page.getByLabel('CEP *').fill('28990-000');
  await page.getByLabel('Logradouro *').fill('Rua das Flores');
  await page.getByLabel('Número *').fill('120');
  await page.getByLabel('Cidade *').fill('Saquarema');
  await page.getByLabel('UF *').fill('RJ');
  await page.getByRole('button', { name: 'Continuar' }).click();

  await expect(page.getByRole('button', { name: 'Criar loja' })).toBeVisible();
}

test('regressão #97: clicar em Criar loja finaliza, limpa sessão e abre Login com confirmação', async ({ page }) => {
  const obterPayload = await prepararSessaoEApi(page, async (route) => {
    await route.fulfill({
      status: 201,
      contentType: 'application/json',
      body: JSON.stringify({ id: 'loja-97', nomeFantasia: 'Mercado Central' }),
    });
  });

  await preencherAtePassoFinal(page);
  await page.getByRole('button', { name: 'Criar loja' }).click();

  await expect(page.getByText('Loja criada!')).toBeVisible();
  await expect(page.getByText('Bem-vindo de volta')).toBeVisible();

  const payload = obterPayload();
  expect(payload?.cnpj).toBe(CNPJ_VALIDO);
  expect(payload?.endereco?.cep).toBe('28990000');

  const tokenRestante = await page.evaluate(() => sessionStorage.getItem('@auth_token'));
  expect(tokenRestante).toBeNull();
});

test('regressão #97: erro da API ao clicar em Criar loja exibe banner de erro visível na web', async ({ page }) => {
  await prepararSessaoEApi(page, async (route) => {
    await route.fulfill({
      status: 400,
      contentType: 'application/json',
      body: JSON.stringify({ message: 'CNPJ não encontrado na Receita Federal.' }),
    });
  });

  await preencherAtePassoFinal(page);
  await page.getByRole('button', { name: 'Criar loja' }).click();

  await expect(page.getByRole('alert')).toContainText('CNPJ não encontrado na Receita Federal.');
  await expect(page.getByRole('button', { name: 'Criar loja' })).toBeVisible();
});

test('regressão #97: conflito 409 finaliza para Login preservando mensagem de loja já cadastrada', async ({ page }) => {
  const msg = 'Você já possui uma loja cadastrada. Saia e entre novamente para acessar o painel da loja.';
  await prepararSessaoEApi(page, async (route) => {
    await route.fulfill({
      status: 409,
      contentType: 'application/json',
      body: JSON.stringify({ message: msg }),
    });
  });

  await preencherAtePassoFinal(page);
  await page.getByRole('button', { name: 'Criar loja' }).click();

  await expect(page.getByText('Loja já cadastrada')).toBeVisible();
  await expect(page.getByText(msg)).toBeVisible();
  await expect(page.getByText('Bem-vindo de volta')).toBeVisible();
});
