import { calcularDistanciaKm, formatarDistancia, estimarCaminhada } from './geo.js';

function normalizarTexto(str) {
  return String(str || '')
    .trim()
    .toLowerCase()
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '');
}

export function calcularEconomiaBusca(itens) {
  if (!Array.isArray(itens) || itens.length < 2) return null;

  const validos = itens
    .map((item) => ({
      item,
      preco: Number(item?.preco ?? item?.precoExibicao ?? item?.precoBase),
      lojaId: item?.lojaId ?? item?.LojaId ?? item?.lojaNome ?? item?.id,
    }))
    .filter((x) => Number.isFinite(x.preco) && x.preco > 0);

  if (validos.length < 2) return null;

  const lojasDistintas = new Set(validos.map((v) => String(v.lojaId)));
  if (lojasDistintas.size < 2) return null;

  validos.sort((a, b) => a.preco - b.preco);
  const menor = validos[0];
  const maior = validos[validos.length - 1];
  const economia = Number((maior.preco - menor.preco).toFixed(2));

  if (economia <= 0) return null;

  return {
    economia,
    menorPreco: menor.preco,
    maiorPreco: maior.preco,
    melhorLojaId: menor.item.lojaId ?? null,
    melhorLojaNome: menor.item.lojaNome || menor.item.lojaNomeFantasia || 'loja mais barata',
    melhorItem: menor.item,
  };
}

export function enriquecerEOrdenarResultadosBusca(
  itens,
  lojas = [],
  localizacaoCliente = null,
  { ordenarPor = 'preco', apenasPromocao = false } = {}
) {
  if (!Array.isArray(itens)) return [];

  const mapaLojas = new Map();
  (lojas || []).forEach((l) => {
    const id = l?.id ?? l?.Id;
    if (id != null) mapaLojas.set(String(id), l);
  });

  const filtrados = apenasPromocao
    ? itens.filter((item) => {
        const atual = Number(item?.preco ?? item?.precoExibicao);
        const ant = Number(item?.precoAnterior);
        return Boolean(item?.emPromocao) || (Number.isFinite(ant) && ant > atual);
      })
    : itens;

  let menorPrecoGlobal = Infinity;
  filtrados.forEach((item) => {
    const p = Number(item?.preco ?? item?.precoExibicao ?? item?.precoBase);
    if (Number.isFinite(p) && p > 0 && p < menorPrecoGlobal) {
      menorPrecoGlobal = p;
    }
  });

  const enriquecidos = filtrados.map((item) => {
    const loja = item?.lojaId ? mapaLojas.get(String(item.lojaId)) : null;
    const latLoja =
      loja?.latitude ??
      loja?.Latitude ??
      loja?.endereco?.latitude ??
      loja?.Endereco?.Latitude ??
      item?.latitude ??
      null;
    const lngLoja =
      loja?.longitude ??
      loja?.Longitude ??
      loja?.endereco?.longitude ??
      loja?.Endereco?.Longitude ??
      item?.longitude ??
      null;

    const distanciaKm = calcularDistanciaKm(
      localizacaoCliente?.latitude,
      localizacaoCliente?.longitude,
      latLoja,
      lngLoja
    );

    const notaBruta = loja?.mediaAvaliacoes ?? loja?.MediaAvaliacoes ?? null;
    const notaLoja =
      notaBruta != null && Number.isFinite(Number(notaBruta)) && Number(notaBruta) > 0
        ? Number(Number(notaBruta).toFixed(1))
        : null;

    const precoNum = Number(item?.preco ?? item?.precoExibicao ?? item?.precoBase ?? 0);

    return {
      ...item,
      preco: precoNum,
      lojaNome:
        item?.lojaNome ||
        item?.lojaNomeFantasia ||
        loja?.nomeFantasia ||
        loja?.NomeFantasia ||
        'Loja parceira',
      lojaCodigoPublico:
        item?.lojaCodigoPublico || loja?.codigoPublico || loja?.CodigoPublico || null,
      latitudeLoja: latLoja != null ? Number(latLoja) : null,
      longitudeLoja: lngLoja != null ? Number(lngLoja) : null,
      distanciaKm,
      distanciaFormatada: formatarDistancia(distanciaKm),
      caminhadaFormatada: estimarCaminhada(distanciaKm),
      notaLoja,
      melhorPreco:
        filtrados.length > 1 &&
        Number.isFinite(precoNum) &&
        precoNum > 0 &&
        Math.abs(precoNum - menorPrecoGlobal) < 0.005,
    };
  });

  enriquecidos.sort((a, b) => {
    if (ordenarPor === 'distancia') {
      const da = a.distanciaKm ?? Infinity;
      const db = b.distanciaKm ?? Infinity;
      if (da !== db) return da - db;
    }
    const pa = Number.isFinite(a.preco) && a.preco > 0 ? a.preco : Infinity;
    const pb = Number.isFinite(b.preco) && b.preco > 0 ? b.preco : Infinity;
    return pa - pb;
  });

  return enriquecidos;
}

export function calcularDescontoPercentual(precoAtual, precoAnterior) {
  const atual = Number(precoAtual);
  const anterior = Number(precoAnterior);
  if (!Number.isFinite(atual) || !Number.isFinite(anterior)) return 0;
  if (anterior <= 0 || atual <= 0 || anterior <= atual) return 0;
  return Number((((anterior - atual) / anterior) * 100).toFixed(1));
}

export function contarFavoritosEmPromocao(favoritos) {
  if (!Array.isArray(favoritos)) return 0;
  return favoritos.filter((f) => {
    const preco = Number(f?.precoExibicao ?? f?.oferta?.preco ?? f?.precoBase ?? f?.produto?.preco);
    const anterior = Number(f?.precoAnterior ?? f?.oferta?.precoAnterior);
    const emPromo = Boolean(f?.emPromocao ?? f?.oferta?.emPromocao);
    return emPromo || calcularDescontoPercentual(preco, anterior) > 0;
  }).length;
}

function chaveDiaLocal(date) {
  return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(
    date.getDate()
  ).padStart(2, '0')}`;
}

export function agruparHistoricoPorDia(historico, dataReferencia = new Date()) {
  if (!Array.isArray(historico) || historico.length === 0) return [];

  const ref = new Date(dataReferencia);
  const hojeKey = chaveDiaLocal(ref);
  const ontem = new Date(ref);
  ontem.setDate(ontem.getDate() - 1);
  const ontemKey = chaveDiaLocal(ontem);

  const gruposMap = new Map();

  historico.forEach((item) => {
    const dt = item?.dataPesquisa ? new Date(item.dataPesquisa) : null;
    const valido = dt && !Number.isNaN(dt.getTime());
    const key = valido ? chaveDiaLocal(dt) : 'Recentes';

    let rotulo = 'Recentes';
    if (valido) {
      if (key === hojeKey) rotulo = 'Hoje';
      else if (key === ontemKey) rotulo = 'Ontem';
      else {
        rotulo = dt.toLocaleDateString('pt-BR', {
          day: '2-digit',
          month: 'long',
        });
      }
    }

    const hora = valido
      ? dt.toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' })
      : '';

    if (!gruposMap.has(key)) {
      gruposMap.set(key, { dia: rotulo, items: [] });
    }
    gruposMap.get(key).items.push({
      ...item,
      horaFormatada: hora,
    });
  });

  return [...gruposMap.values()];
}

export function calcularIndicadoresHistorico(historico, dataReferencia = new Date()) {
  if (!Array.isArray(historico) || historico.length === 0) {
    return { buscasNoMes: 0, termosDiferentes: 0, lojasVisitadas: 0 };
  }

  const ref = new Date(dataReferencia);
  const mesRef = ref.getMonth();
  const anoRef = ref.getFullYear();

  let buscasNoMes = 0;
  const termos = new Set();
  const lojas = new Set();

  historico.forEach((h) => {
    const dt = h?.dataPesquisa ? new Date(h.dataPesquisa) : null;
    if (dt && !Number.isNaN(dt.getTime())) {
      if (dt.getMonth() === mesRef && dt.getFullYear() === anoRef) {
        buscasNoMes += 1;
      }
    } else {
      buscasNoMes += 1;
    }

    const termoNorm = normalizarTexto(h?.termoPesquisa);
    if (termoNorm) termos.add(termoNorm);

    if (h?.lojaId) lojas.add(String(h.lojaId));
  });

  return {
    buscasNoMes,
    termosDiferentes: termos.size,
    lojasVisitadas: lojas.size,
  };
}

export function calcularIndicadoresProdutosLoja(meusProdutos = [], todasOfertas = [], lojaId = null) {
  const lista = Array.isArray(meusProdutos) ? meusProdutos : [];
  const ofertas = Array.isArray(todasOfertas) ? todasOfertas : [];

  const ofertasDaLojaPorProduto = new Map();
  const todasOfertasPorProduto = new Map();

  ofertas.forEach((o) => {
    const pid = String(o?.produtoId ?? '');
    if (!pid) return;
    if (!todasOfertasPorProduto.has(pid)) todasOfertasPorProduto.set(pid, []);
    todasOfertasPorProduto.get(pid).push(o);

    if (lojaId && String(o?.lojaId) === String(lojaId)) {
      const atual = ofertasDaLojaPorProduto.get(pid);
      if (!atual || Number(o.preco) < Number(atual.preco)) {
        ofertasDaLojaPorProduto.set(pid, o);
      }
    }
  });

  let estoqueBaixo = 0;
  let semEstoque = 0;
  let primeiroLugarCount = 0;

  lista.forEach((p) => {
    const pid = String(p?.id ?? '');
    const ofertaMinha = ofertasDaLojaPorProduto.get(pid);

    if (ofertaMinha) {
      const qtd =
        ofertaMinha.quantidadeEstoque != null ? Number(ofertaMinha.quantidadeEstoque) : null;
      const disp = ofertaMinha.disponivel !== false;
      if (!disp || (qtd != null && qtd <= 0)) {
        semEstoque += 1;
      } else if (qtd != null && qtd > 0 && qtd < 10) {
        estoqueBaixo += 1;
      }
    }

    const meuPreco = Number(ofertaMinha?.preco ?? p?.preco);
    const ofertasDoProduto = todasOfertasPorProduto.get(pid) || [];
    if (ofertasDoProduto.length > 0 && Number.isFinite(meuPreco) && meuPreco > 0) {
      const menorMercado = Math.min(
        ...ofertasDoProduto
          .map((o) => Number(o.preco))
          .filter((v) => Number.isFinite(v) && v > 0)
      );
      if (meuPreco <= menorMercado + 0.005) {
        primeiroLugarCount += 1;
      }
    } else if (Number.isFinite(meuPreco) && meuPreco > 0) {
      primeiroLugarCount += 1;
    }
  });

  const percentualPrimeiroLugar =
    lista.length > 0 ? Math.round((primeiroLugarCount / lista.length) * 100) : 0;

  return {
    ativos: lista.length,
    estoqueBaixo,
    semEstoque,
    percentualPrimeiroLugar,
    ofertasDaLojaPorProduto,
    todasOfertasPorProduto,
  };
}

export function calcularCompetitividadePrecos(meusProdutos = [], todasOfertas = [], lojaId = null) {
  const { ofertasDaLojaPorProduto, todasOfertasPorProduto } = calcularIndicadoresProdutosLoja(
    meusProdutos,
    todasOfertas,
    lojaId
  );

  return (meusProdutos || [])
    .map((p) => {
      const pid = String(p?.id ?? '');
      const nome = p?.nome || p?.nomeProduto || 'Produto';
      const ofertaMinha = ofertasDaLojaPorProduto.get(pid);
      const meuPreco = Number(ofertaMinha?.preco ?? p?.preco ?? 0);

      const ofertasProduto = (todasOfertasPorProduto.get(pid) || []).filter(
        (o) => Number.isFinite(Number(o?.preco)) && Number(o.preco) > 0
      );

      const concorrentes = ofertasProduto.filter(
        (o) => !lojaId || String(o.lojaId) !== String(lojaId)
      );

      const baseComparacao = concorrentes.length > 0 ? concorrentes : ofertasProduto;
      const mediaMercado =
        baseComparacao.length > 0
          ? baseComparacao.reduce((acc, o) => acc + Number(o.preco), 0) / baseComparacao.length
          : meuPreco;

      const diferencaPercentual =
        mediaMercado > 0 ? Number((((meuPreco - mediaMercado) / mediaMercado) * 100).toFixed(1)) : 0;

      const precosOrdenados = [
        ...new Set(
          [meuPreco, ...ofertasProduto.map((o) => Number(o.preco))]
            .filter((v) => Number.isFinite(v) && v > 0)
            .sort((a, b) => a - b)
        ),
      ];
      const posicaoRanking =
        Math.max(
          1,
          precosOrdenados.findIndex((v) => Math.abs(v - meuPreco) < 0.005) + 1
        ) || 1;

      return {
        produtoId: pid,
        nome,
        meuPreco,
        mediaMercado: Number(mediaMercado.toFixed(2)),
        diferencaPercentual,
        posicaoRanking,
        temConcorrentes: concorrentes.length > 0,
      };
    })
    .filter((item) => item.meuPreco > 0);
}

export function cruzarTermosMaisProcuradosComCatalogo(relatorio = [], meusProdutos = []) {
  if (!Array.isArray(relatorio) || relatorio.length === 0) return [];
  const produtosNorm = (meusProdutos || []).map((p) => ({
    ...p,
    texto: normalizarTexto(`${p?.nome || p?.nomeProduto || ''} ${p?.marca || ''}`),
  }));

  const maxQtd = Math.max(
    1,
    ...relatorio.map((r) => Number(r?.quantidade ?? r?.Quantidade ?? 0))
  );

  return relatorio.map((r) => {
    const termo = r?.termo ?? r?.Termo ?? '';
    const qtd = Number(r?.quantidade ?? r?.Quantidade ?? 0);
    const termoNorm = normalizarTexto(termo);
    const emCatalogo =
      Boolean(termoNorm) &&
      produtosNorm.some(
        (p) => p.texto.includes(termoNorm) || termoNorm.includes(p.texto.split(' ')[0] || '__')
      );

    return {
      termo,
      quantidade: qtd,
      ultimaPesquisa: r?.ultimaPesquisa ?? r?.UltimaPesquisa ?? null,
      emCatalogo,
      percentualBarra: Math.max(8, Math.round((qtd / maxQtd) * 100)),
    };
  });
}
