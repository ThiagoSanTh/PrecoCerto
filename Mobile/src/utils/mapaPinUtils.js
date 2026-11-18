import { nomeProduto } from './produtoUtils.js';
import { formatarPrecoBrl } from './mapaUtils.js';

function normalizarTexto(texto) {
  return String(texto ?? '')
    .trim()
    .toLowerCase()
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '');
}

/**
 * Pontua proximidade entre termo de busca e nome do produto.
 * Maior score = melhor match.
 */
export function pontuarMatchProduto(termo, nome) {
  const t = normalizarTexto(termo);
  const n = normalizarTexto(nome);

  if (!t || !n) return 0;
  if (n === t) return 100;
  if (n.startsWith(t)) return 80;

  const palavras = n.split(/\s+/).filter(Boolean);
  if (palavras.some((p) => p.startsWith(t))) return 60;
  if (n.includes(t)) return 40;

  const palavrasTermo = t.split(/\s+/).filter(Boolean);
  if (palavrasTermo.some((pt) => n.includes(pt))) return 20;

  return 0;
}

function compararProdutos(a, b, termo) {
  const scoreA = pontuarMatchProduto(termo, nomeProduto(a));
  const scoreB = pontuarMatchProduto(termo, nomeProduto(b));
  if (scoreB !== scoreA) return scoreB - scoreA;

  const precoA = Number(a.preco) || 0;
  const precoB = Number(b.preco) || 0;
  if (precoA !== precoB) return precoA - precoB;

  return 0;
}

/**
 * Escolhe o produto cuja foto aparece no pin da loja.
 * Prioridade: em promoção com melhor match; senão melhor match geral.
 */
export function selecionarProdutoParaPin(produtos, termo) {
  const lista = Array.isArray(produtos) ? produtos.filter(Boolean) : [];
  if (lista.length === 0) return null;

  const promocoes = lista.filter((p) => Boolean(p.emPromocao));
  const candidatos = promocoes.length > 0 ? promocoes : lista;

  return [...candidatos].sort((a, b) => compararProdutos(a, b, termo))[0] ?? null;
}

/**
 * Agrupa produtos da busca por loja para popup e imagem do pin.
 */
export function montarMapaBuscaPorLoja(produtos, termo, maxPorPin = 4) {
  const produtosPorLoja = {};
  const imagemPinPorLoja = {};
  const termoAtivo = String(termo ?? '').trim();

  if (!termoAtivo) {
    return { produtosPorLoja, imagemPinPorLoja };
  }

  const grupos = {};

  (produtos || []).forEach((p) => {
    const lojaId = p.lojaId;
    if (!lojaId) return;
    const chave = String(lojaId);
    if (!grupos[chave]) grupos[chave] = [];
    grupos[chave].push(p);
  });

  Object.entries(grupos).forEach(([lojaId, itens]) => {
    const escolhido = selecionarProdutoParaPin(itens, termoAtivo);
    const ordenados = escolhido
      ? [escolhido, ...itens.filter((p) => p.id !== escolhido.id)]
      : itens;

    produtosPorLoja[lojaId] = ordenados.slice(0, maxPorPin).map((p) => ({
      id: p.id,
      nome: nomeProduto(p),
      preco: formatarPrecoBrl(p.preco),
      imagemUrl: p.imagemUrl || null,
    }));

    imagemPinPorLoja[lojaId] = escolhido?.imagemUrl ?? null;
  });

  const contagemPorLoja = {};
  Object.entries(grupos).forEach(([lojaId, itens]) => {
    contagemPorLoja[lojaId] = itens.length;
  });

  return { produtosPorLoja, imagemPinPorLoja, contagemPorLoja };
}

/** Lojas no mesmo ponto (geocode da cidade) empilham num marcador só. */
export function separarCoordenadasCoincidentes(marcadores) {
  const grupos = new Map();
  (marcadores || []).forEach((m) => {
    if (!Number.isFinite(m?.lat) || !Number.isFinite(m?.lng)) return;
    const key = `${m.lat.toFixed(5)},${m.lng.toFixed(5)}`;
    if (!grupos.has(key)) grupos.set(key, []);
    grupos.get(key).push(m);
  });

  const saida = [];
  grupos.forEach((grupo) => {
    if (grupo.length === 1) {
      saida.push(grupo[0]);
      return;
    }
    grupo.forEach((m, i) => {
      const ang = (2 * Math.PI * i) / grupo.length;
      saida.push({
        ...m,
        lat: m.lat + 0.00035 * Math.cos(ang),
        lng: m.lng + 0.00035 * Math.sin(ang),
      });
    });
  });
  return saida;
}
