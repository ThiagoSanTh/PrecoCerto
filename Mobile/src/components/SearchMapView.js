import { useEffect, useMemo, useState } from 'react';
import { View, Text, StyleSheet } from 'react-native';
import LeafletMapFrame from './LeafletMapFrame';
import { obterLocalizacaoAtual } from '../services/locationService';
import {
  contarProdutosSemCoordenadas,
  filtrarProdutosComCoordenadas,
} from '../utils/mapaUtils';
import {
  buildLeafletMapHtml,
  prepararDadosMapaLeaflet,
  sanitizarMarcadoresParaHtml,
} from '../utils/leafletMapHtml';
import { styles as appStyles } from '../style';

export default function SearchMapView({ produtos, onProductPress }) {
  const [localizacaoCliente, setLocalizacaoCliente] = useState(null);
  const [erroGps, setErroGps] = useState(null);

  useEffect(() => {
    let ativo = true;

    (async () => {
      try {
        const coords = await obterLocalizacaoAtual();
        if (ativo) {
          setLocalizacaoCliente(coords);
          setErroGps(null);
        }
      } catch (e) {
        if (ativo) {
          setErroGps(e.message || 'GPS indisponível');
        }
      }
    })();

    return () => {
      ativo = false;
    };
  }, []);

  const produtosNoMapa = useMemo(
    () => filtrarProdutosComCoordenadas(produtos),
    [produtos]
  );

  const semCoordenadas = useMemo(
    () => contarProdutosSemCoordenadas(produtos),
    [produtos]
  );

  const mapHtml = useMemo(() => {
    const dados = prepararDadosMapaLeaflet(localizacaoCliente, produtosNoMapa);
    dados.marcadores = sanitizarMarcadoresParaHtml(dados.marcadores);
    return buildLeafletMapHtml(dados);
  }, [localizacaoCliente, produtosNoMapa]);

  const mapKey = useMemo(
    () =>
      `${localizacaoCliente?.latitude ?? 'x'}-${localizacaoCliente?.longitude ?? 'y'}-${produtosNoMapa.map((p) => p.id).join(',')}`,
    [localizacaoCliente, produtosNoMapa]
  );

  function handleMessage(event) {
    if (!onProductPress) return;
    try {
      const data = JSON.parse(event.nativeEvent.data);
      if (data.type === 'product' && data.productId) {
        onProductPress(data.productId);
      }
    } catch {
      /* ignore */
    }
  }

  return (
    <View style={styles.container}>
      {semCoordenadas > 0 ? (
        <Text style={appStyles.hint}>
          {semCoordenadas} produto(s) sem localização da loja no mapa.
        </Text>
      ) : null}
      {erroGps ? (
        <Text style={appStyles.hint}>{erroGps}</Text>
      ) : null}
      <LeafletMapFrame
        key={mapKey}
        mapKey={mapKey}
        style={styles.map}
        html={mapHtml}
        onMessage={handleMessage}
      />
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    minHeight: 320,
  },
  map: {
    flex: 1,
    borderRadius: 12,
    overflow: 'hidden',
    backgroundColor: '#e2e8f0',
  },
});
