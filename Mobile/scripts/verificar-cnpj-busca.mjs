import { chromium } from 'playwright';
import { mkdir } from 'node:fs/promises';

const BASE = process.env.CNPJ_PREVIEW_URL || 'http://127.0.0.1:3456';
const OUT = process.env.CNPJ_SHOTS || '/opt/cursor/artifacts';
const CNPJ_OK = '11222333000181';
const CNPJ_INATIVO = '19131243000197';
const DETAIL_CRU = 'situação cadastral BAIXADA no provedor';

const RESPOSTA = {
  cnpj: CNPJ_OK,
  razaoSocial: 'Padaria São Jorge Ltda',
  nomeFantasia: 'Padaria São Jorge',
  situacaoCadastral: 'ATIVA',
  situacao: 'ATIVA',
  ativo: true,
  endereco: {
    logradouro: 'RUA FULANO DE TAL',
    numero: '100',
    complemento: 'Loja 2',
    bairro: 'Centro',
    cidade: 'Saquarema',
    uf: 'RJ',
    cep: '28990000',
  },
  email: 'contato@padaria.test',
  telefone: '21987654321',
};

function jwt() {
  const b64 = (valor) => Buffer.from(JSON.stringify(valor)).toString('base64url');
  return `${b64({ alg: 'none', typ: 'JWT' })}.${b64({ sub: 'ana' })}.sig`;
}

function falhar(mensagem) {
  throw new Error(mensagem);
}

async function preparar(page) {
  let consultas = 0;

  const responder = async (route) => {
    const url = route.request().url();
    if (!url.includes('/Consultas/cnpj/')) {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ temNovas: false }),
      });
      return;
    }

    consultas += 1;
    const cnpj = decodeURIComponent(url.split('/').pop().split('?')[0]);
    if (cnpj === CNPJ_INATIVO) {
      await route.fulfill({
        status: 422,
        contentType: 'application/problem+json',
        headers: { 'X-Error-Code': 'cnpj_inativo' },
        body: JSON.stringify({
          title: 'CNPJ inativo',
          status: 422,
          detail: DETAIL_CRU,
          code: 'cnpj_inativo',
        }),
      });
      return;
    }

    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(RESPOSTA),
    });
  };

  await page.route('http://localhost:5132/**', responder);
  await page.route('http://127.0.0.1:5132/**', responder);
  await page.addInitScript((token) => {
    localStorage.setItem('@auth_token', token);
    localStorage.setItem('@session', JSON.stringify({
      tipo: 'cliente',
      perfil: {
        id: '11111111-1111-1111-1111-111111111111',
        email: 'ana@loja.test',
        nomeUsuario: 'Ana',
      },
    }));
    localStorage.setItem('@userMode', 'user');
    localStorage.setItem('@sw-migration-v3', 'done');
  }, jwt());

  return () => consultas;
}

async function abrir(browser, viewport) {
  const page = await browser.newPage({ viewport });
  const consultas = await preparar(page);
  await page.goto(`${BASE}/criar-loja`, { waitUntil: 'domcontentloaded' });
  await page.getByRole('button', { name: 'Buscar dados do CNPJ' }).waitFor({ timeout: 30000 });
  return { page, consultas };
}

async function caixaDoBotao(page) {
  const botao = page.getByRole('button', { name: 'Buscar dados do CNPJ' });
  const caixa = await botao.boundingBox();
  if (!caixa) falhar('botão da lupa sem caixa');
  const largura = Math.round(caixa.width);
  const altura = Math.round(caixa.height);
  if (largura !== 44 || altura !== 44) {
    falhar(`lupa deveria ter 44x44 e mediu ${largura}x${altura}`);
  }
}

async function fluxoCelular(browser) {
  const { page, consultas } = await abrir(browser, { width: 390, height: 844 });
  const botao = page.getByRole('button', { name: 'Buscar dados do CNPJ' });
  if (await botao.isEnabled()) falhar('lupa habilitada sem 14 dígitos');
  await caixaDoBotao(page);
  await page.screenshot({ path: `${OUT}/cnpj-390-desabilitado.png`, fullPage: true });

  await page.getByRole('textbox', { name: 'CNPJ' }).fill('11111111111111');
  await botao.click();
  await page.getByText('CNPJ inválido. Confira os números.').waitFor();
  if (consultas() !== 0) falhar(`CNPJ inválido no cliente chamou a API ${consultas()} vezes`);

  await page.getByRole('textbox', { name: 'CNPJ' }).fill(CNPJ_INATIVO);
  await botao.click();
  await page.getByText('Este CNPJ não está ativo na Receita.').waitFor();
  if (await page.getByText(DETAIL_CRU).count()) falhar('detail cru da API apareceu na tela');
  if (consultas() !== 1) falhar(`consulta inativa rodou ${consultas()} vezes`);
  await page.screenshot({ path: `${OUT}/cnpj-390-inativo.png`, fullPage: true });

  await page.getByRole('textbox', { name: 'CNPJ' }).fill(CNPJ_OK);
  await botao.click();
  await page.waitForFunction(() => {
    const inputs = [...document.querySelectorAll('input, textarea')];
    return inputs.some((el) => el.value === 'Padaria São Jorge')
      && inputs.some((el) => el.value === '(21) 98765-4321');
  });
  const valores = await page.locator('input, textarea').evaluateAll((els) => els.map((el) => el.value));
  if (!valores.includes('Padaria São Jorge')) falhar(`nome não preenchido: ${valores.join(' | ')}`);
  if (!valores.includes('Padaria São Jorge Ltda')) falhar(`razão não preenchida: ${valores.join(' | ')}`);
  if (!valores.includes('(21) 98765-4321')) falhar(`telefone sem máscara: ${valores.join(' | ')}`);
  if (!valores.includes('contato@padaria.test')) falhar(`e-mail não preenchido: ${valores.join(' | ')}`);
  if (consultas() !== 2) falhar(`sucesso deveria ser a 2ª consulta e foi ${consultas()}`);

  await page.getByRole('textbox', { name: 'E-mail da loja' }).click();
  await page.waitForTimeout(300);
  if (consultas() !== 2) falhar('o blur repetiu a consulta do mesmo CNPJ');
  await page.screenshot({ path: `${OUT}/cnpj-390-preenchido.png`, fullPage: true });

  await page.getByRole('button', { name: 'Continuar' }).click();
  await page.getByRole('textbox', { name: 'Logradouro *' }).waitFor();
  const endereco = await page.locator('input, textarea').evaluateAll((els) => els.map((el) => el.value));
  if (!endereco.includes('RUA FULANO DE TAL')) falhar(`logradouro alterado: ${endereco.join(' | ')}`);
  if (!endereco.includes('28990-000')) falhar(`CEP sem máscara: ${endereco.join(' | ')}`);
  if (endereco.some((valor) => valor.startsWith('AVENIDA') || valor.startsWith('RUA RUA'))) {
    falhar('o tipo do logradouro foi inventado');
  }
  await page.screenshot({ path: `${OUT}/cnpj-390-endereco.png`, fullPage: true });
  await page.close();
}

async function fluxoDesktop(browser) {
  const { page, consultas } = await abrir(browser, { width: 1280, height: 800 });
  await page.getByRole('textbox', { name: 'Nome fantasia *' }).fill('Minha Loja');
  await page.getByRole('textbox', { name: 'CNPJ' }).fill(CNPJ_OK);
  await page.getByRole('button', { name: 'Buscar dados do CNPJ' }).click();
  await page.getByText('Alguns campos já estão preenchidos. Substituir pelos dados do CNPJ?').waitFor();
  const antes = await page.locator('input, textarea').evaluateAll((els) => els.map((el) => el.value));
  if (!antes.includes('Minha Loja')) falhar('o nome digitado foi sobrescrito sem confirmação');
  if (!antes.includes('(21) 98765-4321')) falhar('campo vazio não foi preenchido');
  await caixaDoBotao(page);
  await page.screenshot({ path: `${OUT}/cnpj-1280-confirmar.png`, fullPage: true });

  await page.getByRole('button', { name: 'Substituir pelos dados do CNPJ' }).click();
  await page.waitForFunction(() => [...document.querySelectorAll('input, textarea')]
    .some((el) => el.value === 'Padaria São Jorge'));
  if (consultas() !== 1) falhar(`a confirmação buscou de novo (${consultas()})`);
  await page.screenshot({ path: `${OUT}/cnpj-1280-substituido.png`, fullPage: true });
  await page.close();
}

const browser = await chromium.launch({ headless: true });
await mkdir(OUT, { recursive: true });
try {
  await fluxoCelular(browser);
  await fluxoDesktop(browser);
  console.log('verificacao-cnpj-ok');
} finally {
  await browser.close();
}
