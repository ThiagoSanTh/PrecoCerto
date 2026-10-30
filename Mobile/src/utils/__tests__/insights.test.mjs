import test from 'node:test';
import assert from 'node:assert/strict';
import {
  calcularEconomiaBusca,
  enriquecerEOrdenarResultadosBusca,
  calcularDescontoPercentual,
  contarFavoritosEmPromocao,
  agruparHistoricoPorDia,
  calcularIndicadoresHistorico,
  calcularIndicadoresProdutosLoja,
  calcularCompetitividadePrecos,
  cruzarTermosMaisProcuradosComCatalogo,
} from '../insights.js';

test('calcularEconomiaBusca returns savings between max and min price when at least 2 stores exist', () => {
  const itens = [
    { id: '1', lojaId: 'L1', lojaNome: 'Supermercado Lagoa', preco: 17.9 },
    { id: '2', lojaId: 'L2', lojaNome: 'Empório Porto Novo', preco: 22.3 },
  ];
  const res = calcularEconomiaBusca(itens);
  assert.ok(res);
  assert.equal(res.economia, 4.4);
  assert.equal(res.melhorLojaNome, 'Supermercado Lagoa');
  assert.equal(res.menorPreco, 17.9);
  assert.equal(calcularEconomiaBusca([itens[0]]), null);
});

test('enriquecerEOrdenarResultadosBusca marks best price, computes distance and sorts by price or distance', () => {
  const itens = [
    { id: 'p2', nome: 'Café', lojaId: 'L2', lojaNome: 'Mercadinho', preco: 19.5, emPromocao: false },
    { id: 'p1', nome: 'Café', lojaId: 'L1', lojaNome: 'Lagoa', preco: 17.9, precoAnterior: 21.5, emPromocao: true },
  ];
  const lojas = [
    { id: 'L1', nomeFantasia: 'Lagoa', latitude: -22.94, longitude: -42.52, mediaAvaliacoes: 4.8 },
    { id: 'L2', nomeFantasia: 'Mercadinho', latitude: -22.933, longitude: -42.511, mediaAvaliacoes: 4.5 },
  ];
  const cliente = { latitude: -22.9329, longitude: -42.5114 };

  const porPreco = enriquecerEOrdenarResultadosBusca(itens, lojas, cliente, {
    ordenarPor: 'preco',
    apenasPromocao: false,
  });
  assert.equal(porPreco[0].id, 'p1');
  assert.equal(porPreco[0].melhorPreco, true);
  assert.equal(porPreco[1].melhorPreco, false);
  assert.equal(porPreco[0].notaLoja, 4.8);

  const porDistancia = enriquecerEOrdenarResultadosBusca(itens, lojas, cliente, {
    ordenarPor: 'distancia',
    apenasPromocao: false,
  });
  assert.equal(porDistancia[0].id, 'p2');

  const soPromo = enriquecerEOrdenarResultadosBusca(itens, lojas, cliente, {
    ordenarPor: 'preco',
    apenasPromocao: true,
  });
  assert.equal(soPromo.length, 1);
  assert.equal(soPromo[0].id, 'p1');
});

test('calcularDescontoPercentual and contarFavoritosEmPromocao compute real promotion stats', () => {
  assert.equal(calcularDescontoPercentual(17.9, 21.5), 16.7);
  assert.equal(calcularDescontoPercentual(20, 20), 0);
  assert.equal(calcularDescontoPercentual(20, null), 0);

  const favs = [
    { id: 'f1', produtoId: 'p1', precoExibicao: 17.9, precoAnterior: 21.5, emPromocao: true },
    { id: 'f2', produtoId: 'p2', precoExibicao: 26.4, precoAnterior: 26.4, emPromocao: false },
    { id: 'f3', produtoId: 'p3', oferta: { preco: 10, precoAnterior: 12.5, emPromocao: true } },
  ];
  assert.equal(contarFavoritosEmPromocao(favs), 2);
});

test('agruparHistoricoPorDia and calcularIndicadoresHistorico group searches and count real stats', () => {
  const refNow = new Date('2026-10-29T15:00:00Z');
  const lista = [
    { id: 'h1', termoPesquisa: 'Café Pilão', dataPesquisa: '2026-10-29T13:42:00Z', lojaId: 'L1' },
    { id: 'h2', termoPesquisa: 'Arroz 5kg', dataPesquisa: '2026-10-28T18:30:00Z', lojaId: 'L2' },
    { id: 'h3', termoPesquisa: 'café pilão', dataPesquisa: '2026-10-20T11:20:00Z', lojaId: 'L1' },
  ];

  const grupos = agruparHistoricoPorDia(lista, refNow);
  assert.equal(grupos.length, 3);
  assert.equal(grupos[0].dia, 'Hoje');
  assert.equal(grupos[1].dia, 'Ontem');

  const kpis = calcularIndicadoresHistorico(lista, refNow);
  assert.equal(kpis.buscasNoMes, 3);
  assert.equal(kpis.termosDiferentes, 2);
  assert.equal(kpis.lojasVisitadas, 2);
});

test('calcularIndicadoresProdutosLoja and calcularCompetitividadePrecos compute merchant metrics', () => {
  const meusProdutos = [
    { id: 'p1', nome: 'Café Pilão 500g', preco: 17.9, lojaId: 'L1' },
    { id: 'p2', nome: 'Arroz 5kg', preco: 27.5, lojaId: 'L1' },
    { id: 'p3', nome: 'Óleo 900ml', preco: 7.99, lojaId: 'L1' },
  ];
  const todasOfertas = [
    { id: 'o1', produtoId: 'p1', nomeProduto: 'Café Pilão 500g', lojaId: 'L1', preco: 17.9, disponivel: true, quantidadeEstoque: 42 },
    { id: 'o2', produtoId: 'p1', nomeProduto: 'Café Pilão 500g', lojaId: 'L2', preco: 19.7, disponivel: true, quantidadeEstoque: 10 },
    { id: 'o3', produtoId: 'p2', nomeProduto: 'Arroz 5kg', lojaId: 'L1', preco: 27.5, disponivel: true, quantidadeEstoque: 5 },
    { id: 'o4', produtoId: 'p2', nomeProduto: 'Arroz 5kg', lojaId: 'L2', preco: 26.4, disponivel: true, quantidadeEstoque: 20 },
    { id: 'o5', produtoId: 'p3', nomeProduto: 'Óleo 900ml', lojaId: 'L1', preco: 7.99, disponivel: false, quantidadeEstoque: 0 },
  ];

  const ind = calcularIndicadoresProdutosLoja(meusProdutos, todasOfertas, 'L1');
  assert.equal(ind.ativos, 3);
  assert.equal(ind.estoqueBaixo, 1);
  assert.equal(ind.semEstoque, 1);
  assert.equal(ind.percentualPrimeiroLugar, 67);

  const comp = calcularCompetitividadePrecos(meusProdutos, todasOfertas, 'L1');
  assert.equal(comp.length, 3);
  const cafe = comp.find((c) => c.produtoId === 'p1');
  assert.ok(cafe.diferencaPercentual < 0);
});

test('cruzarTermosMaisProcuradosComCatalogo marks whether searched term exists in store catalog', () => {
  const relatorio = [
    { termo: 'protetor solar', quantidade: 20 },
    { termo: 'café pilão', quantidade: 10 },
  ];
  const produtos = [{ id: 'p1', nome: 'Café Pilão 500g', marca: 'Pilão' }];
  const cruzado = cruzarTermosMaisProcuradosComCatalogo(relatorio, produtos);
  assert.equal(cruzado[0].emCatalogo, false);
  assert.equal(cruzado[0].percentualBarra, 100);
  assert.equal(cruzado[1].emCatalogo, true);
  assert.equal(cruzado[1].percentualBarra, 50);
});
