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
 * Mapa do feed com lojas.
 * Enquanto o usuário digita, `lojaIdsDestaque` destaca os pins das lojas que
 * têm o produto buscado e oculta as demais — sem recriar o HTML do mapa.
 */
export default function LojasMapView({
  lojas,
  localizacaoCliente: localizacaoExterna,
  lojaIdsDestaque = null,
  produtosPorLoja = {},
  imagemPinPorLoja = {},
  onProductPress,
  edgeToEdge = false,
}) {
  const [localizacaoInterna, setLocalizacaoInterna] = useState(null);
  const [erroGps, setErroGps] = useState(null);
  const frameRef = useRef(null);
  const mapaProntoRef = useRef(false);
  const gpsProprio = localizacaoExterna === undefined;

  useEffect(() => {
    if (!gpsProprio) return undefined;
    let ativo = true;

    (async () => {
      try {
        const coords = await obterLocalizacaoAtual();
        if (ativo) {
          setLocalizacaoInterna(coords);
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
  }, [gpsProprio]);

  const localizacaoCliente = gpsProprio ? localizacaoInterna : localizacaoExterna;

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
  const avisoTexto =
    [
      semCoordenadas > 0 ? `${semCoordenadas} loja(s) sem localização no mapa.` : null,
      erroGps || null,
    ]
      .filter(Boolean)
      .join(' ') || null;

  const mapHtml = useMemo(() => {
    const dados = prepararDadosMapaLojas(null, lojasNoMapa);
    dados.marcadores = sanitizarLojasParaHtml(dados.marcadores);
    return buildLojasMapHtml(dados);
  }, [lojasNoMapa]);

  const mapKey = useMemo(() => {
    const count = lojasNoMapa.length;
    const hash = lojasNoMapa.slice(0, 5).map((l) => l.id).join('-');
    return `n${count}-${hash}`;
  }, [lojasNoMapa]);

  const enviarDestaques = useCallback(() => {
    if (!mapaProntoRef.current) return;
    frameRef.current?.enviarMensagem({
      type: 'destaques',
      payload: { lojaIds: lojaIdsDestaque, produtosPorLoja, imagemPinPorLoja },
    });
  }, [lojaIdsDestaque, produtosPorLoja, imagemPinPorLoja]);

  const enviarCliente = useCallback(() => {
    if (!mapaProntoRef.current || !localizacaoCliente) return;
    frameRef.current?.enviarMensagem({
      type: 'cliente',
      payload: {
        lat: Number(localizacaoCliente.latitude),
        lng: Number(localizacaoCliente.longitude),
      },
    });
  }, [localizacaoCliente]);

  useEffect(() => {
    enviarDestaques();
  }, [enviarDestaques]);

  useEffect(() => {
    enviarCliente();
  }, [enviarCliente]);

  useEffect(() => {
    mapaProntoRef.current = false;
  }, [mapKey]);

  function handleMessage(event) {
    try {
      const data = JSON.parse(event.nativeEvent.data);
      if (data.type === 'ready') {
        mapaProntoRef.current = true;
        enviarDestaques();
        enviarCliente();
      } else if (data.type === 'product' && data.productId && onProductPress) {
        onProductPress(data.productId);
      }
    } catch {
      /* ignore */
    }
  }

  return (
    <View style={[styles.container, edgeToEdge && styles.containerEdgeToEdge]}>
      {edgeToEdge && avisoTexto ? (
        <View style={styles.bannerOverlay} pointerEvents="none">
          <Text style={styles.bannerText}>{avisoTexto}</Text>
        </View>
      ) : null}
      {!edgeToEdge && semCoordenadas > 0 ? (
        <Text style={appStyles.hint}>
          {semCoordenadas} loja(s) sem localização no mapa.
        </Text>
      ) : null}
      {!edgeToEdge && erroGps ? <Text style={appStyles.hint}>{erroGps}</Text> : null}
      <LeafletMapFrame
        ref={frameRef}
        key={mapKey}
        mapKey={mapKey}
        style={[styles.map, edgeToEdge && styles.mapEdgeToEdge]}
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
  containerEdgeToEdge: {
    minHeight: 0,
  },
  map: {
    flex: 1,
    borderRadius: 12,
    overflow: 'hidden',
    backgroundColor: '#e2e8f0',
  },
  mapEdgeToEdge: {
    borderRadius: 0,
  },
  bannerOverlay: {
    position: 'absolute',
    bottom: 12,
    left: 12,
    right: 12,
    zIndex: 5,
    backgroundColor: 'rgba(15, 23, 42, 0.75)',
    borderRadius: 8,
    paddingHorizontal: 12,
    paddingVertical: 6,
  },
  bannerText: {
    color: '#fff',
    fontSize: 12,
    textAlign: 'center',
  },
});
