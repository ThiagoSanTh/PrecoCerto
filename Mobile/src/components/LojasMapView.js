import { useEffect, useMemo, useRef, useState, useCallback } from 'react';
import { View, Text, StyleSheet } from 'react-native';
import LeafletMapFrame from './LeafletMapFrame';
import { obterLocalizacaoAtual } from '../services/locationService';
import {
  buildLojasMapHtml,
  normalizarLojaParaMapa,
  prepararDadosMapaLojas,
  sanitizarLojasParaHtml,
} from '../utils/leafletMapHtml';
import { styles as appStyles } from '../theme';

/**
 * Mapa do feed com todas as lojas (issue #36).
 * Enquanto o usuário digita, `lojaIdsDestaque` destaca os pins das lojas que
 * têm o produto buscado e oculta as demais — sem recriar o HTML do mapa.
 *
 * Props:
 * - lojas: lista de lojas da API (LojaRespostaDto)
 * - lojaIdsDestaque: null (todas) ou array de ids de loja a destacar
 * - produtosPorLoja: { [lojaId]: [{ id, nome, preco }] } para o popup do pin
 * - onProductPress: (productId) => void
 */
export default function LojasMapView({
  lojas,
  lojaIdsDestaque = null,
  produtosPorLoja = {},
  onProductPress,
}) {
  const [localizacaoCliente, setLocalizacaoCliente] = useState(null);
  const [erroGps, setErroGps] = useState(null);
  const frameRef = useRef(null);
  const mapaProntoRef = useRef(false);

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

  const lojasNoMapa = useMemo(
    () =>
      (lojas || [])
        .map(normalizarLojaParaMapa)
        .filter(
          (l) =>
            Number.isFinite(l.lat) &&
            Number.isFinite(l.lng) &&
            !(l.lat === 0 && l.lng === 0)
        ),
    [lojas]
  );

  const semCoordenadas = (lojas || []).length - lojasNoMapa.length;

  const mapHtml = useMemo(() => {
    const dados = prepararDadosMapaLojas(localizacaoCliente, lojasNoMapa);
    dados.marcadores = sanitizarLojasParaHtml(dados.marcadores);
    return buildLojasMapHtml(dados);
  }, [localizacaoCliente, lojasNoMapa]);

  const mapKey = useMemo(() => {
    const count = lojasNoMapa.length;
    const hash = lojasNoMapa.slice(0, 5).map((l) => l.id).join('-');
    return `${localizacaoCliente?.latitude ?? 'x'}-${localizacaoCliente?.longitude ?? 'y'}-n${count}-${hash}`;
  }, [localizacaoCliente, lojasNoMapa]);

  const enviarDestaques = useCallback(() => {
    if (!mapaProntoRef.current) return;
    frameRef.current?.enviarMensagem({
      type: 'destaques',
      payload: { lojaIds: lojaIdsDestaque, produtosPorLoja },
    });
  }, [lojaIdsDestaque, produtosPorLoja]);

  // Reenvia os destaques sempre que a busca muda (digitação com debounce).
  useEffect(() => {
    enviarDestaques();
  }, [enviarDestaques]);

  // O HTML foi recriado (mapKey mudou): aguarda novo "ready".
  useEffect(() => {
    mapaProntoRef.current = false;
  }, [mapKey]);

  function handleMessage(event) {
    try {
      const data = JSON.parse(event.nativeEvent.data);
      if (data.type === 'ready') {
        mapaProntoRef.current = true;
        enviarDestaques();
      } else if (data.type === 'product' && data.productId && onProductPress) {
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
          {semCoordenadas} loja(s) sem localização no mapa.
        </Text>
      ) : null}
      {erroGps ? <Text style={appStyles.hint}>{erroGps}</Text> : null}
      <LeafletMapFrame
        ref={frameRef}
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
