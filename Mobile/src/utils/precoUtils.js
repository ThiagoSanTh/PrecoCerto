export function selecionarOfertaPrincipal(ofertas, lojaId) {
  if (!Array.isArray(ofertas) || ofertas.length === 0) return null;
  if (lojaId) {
    const daLoja = ofertas.find((o) => String(o.lojaId) === String(lojaId));
    if (daLoja) return daLoja;
  }
  return ofertas[0];
}

export function montarHistoricoPrecos(produto, oferta) {
  const precoAtual = Number(oferta?.preco ?? produto?.preco ?? 0);
  const antigos = [];

  const precoAnterior = Number(oferta?.precoAnterior);
  if (Number.isFinite(precoAnterior) && precoAnterior !== precoAtual) {
    antigos.push({
      valor: precoAnterior,
      data: oferta?.dataAtualizacaoPreco ?? null,
    });
  }

  const precoProduto = Number(produto?.preco);
  if (
    Number.isFinite(precoProduto) &&
    precoProduto !== precoAtual &&
    !antigos.some((a) => a.valor === precoProduto)
  ) {
    antigos.push({ valor: precoProduto, data: null });
  }

  return {
    precoAtual,
    precosAntigos: antigos.slice(0, 2),
    emPromocao: Boolean(oferta?.emPromocao),
  };
}

export function formatarPrecoMl(valor) {
  const num = Number(valor);
  if (!Number.isFinite(num)) return { inteiro: '0', centavos: '00' };

  const partes = num.toFixed(2).split('.');
  return {
    inteiro: Number(partes[0]).toLocaleString('pt-BR'),
    centavos: partes[1],
  };
}

export function mapaOfertasPorProduto(ofertas) {
  const mapa = new Map();
  if (!Array.isArray(ofertas)) return mapa;

  ofertas.forEach((oferta) => {
    const id = oferta.produtoId;
    if (!id) return;
    const existente = mapa.get(id);
    if (!existente || Number(oferta.preco) < Number(existente.preco)) {
      mapa.set(id, oferta);
    }
  });

  return mapa;
}
