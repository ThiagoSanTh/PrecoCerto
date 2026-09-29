import { useEffect, useMemo, useState } from 'react';
import { View, Text, StyleSheet } from 'react-native';
import MapView, { Marker, Polyline, Callout } from 'react-native-maps';
import { obterLocalizacaoAtual } from '../services/locationService';
import { normalizarLojaParaMapa } from '../utils/mapaUtils';
import {
  MAX_ZOOM_SAQUAREMA,
  MIN_ZOOM_SAQUAREMA,
  regiaoParaPontos,
  regiaoInicialSaquarema,
  travarCoordenadaEmSaquarema,
} from '../utils/saquaremaLock';
import {
  buscarRota,
  escolherLojaDestino,
  distanciaKm,
  formatarDistanciaKm,
} from '../utils/rotaUtils';
import { useMapaSaquarema } from '../hooks/useMapaSaquarema';
import { styles as appStyles } from '../theme';

const COR_LOJA = '#14B8A6';
const COR_DESTAQUE = '#F59E0B';
const COR_DESTINO = '#EA580C';
const COR_CLIENTE = '#2563EB';
const COR_ROTA = '#0F766E';

/**
 * Mapa do feed com MapView e Marker.
 * Abre enquadrado em Saquarema/RJ. Durante a busca, desenha a rota
 * do ponto do cliente até a loja de destino.
 */
export default function LojasMapView({
  lojas,
  localizacaoCliente: localizacaoExterna,
  lojaIdsDestaque = null,
  produtosPorLoja = {},
  buscaAtiva = false,
  onProductPress,
  onStorePress,
  edgeToEdge = false,
}) {
  const [localizacaoInterna, setLocalizacaoInterna] = useState(null);
  const [erroGps, setErroGps] = useState(null);
  const [lojaDestinoId, setLojaDestinoId] = useState(null);
  const [rota, setRota] = useState([]);
  const { mapRef, aplicarLimites, mostrarRegiao, onRegionChangeComplete, regiaoInicial } =
    useMapaSaquarema();
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
        if (ativo) setErroGps(e.message || 'GPS indisponível');
      }
    })();

    return () => {
      ativo = false;
    };
  }, [gpsProprio]);

  useEffect(() => {
    if (!buscaAtiva) setLojaDestinoId(null);
  }, [buscaAtiva]);

  const localizacaoBruta = gpsProprio ? localizacaoInterna : localizacaoExterna;
  const cliente = useMemo(() => {
    if (localizacaoBruta?.latitude == null || localizacaoBruta?.longitude == null) return null;
    return travarCoordenadaEmSaquarema({
      latitude: Number(localizacaoBruta.latitude),
      longitude: Number(localizacaoBruta.longitude),
    });
  }, [localizacaoBruta?.latitude, localizacaoBruta?.longitude]);

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

  const destino = useMemo(() => {
    if (!buscaAtiva) return null;
    return escolherLojaDestino(lojasNoMapa, lojaIdsDestaque, cliente, lojaDestinoId);
  }, [buscaAtiva, lojasNoMapa, lojaIdsDestaque, cliente, lojaDestinoId]);

  useEffect(() => {
    if (!buscaAtiva || !cliente || !destino) {
      setRota([]);
      return undefined;
    }
    let ativo = true;
    buscarRota(cliente, { latitude: destino.lat, longitude: destino.lng }).then((pontos) => {
      if (ativo) setRota(pontos);
    });
    return () => {
      ativo = false;
    };
  }, [buscaAtiva, cliente, destino]);

  useEffect(() => {
    if (buscaAtiva && rota.length >= 2) {
      mostrarRegiao(regiaoParaPontos([rota[0], rota[rota.length - 1], {
        latitude: destino.lat,
        longitude: destino.lng,
      }]));
      return;
    }
    if (!buscaAtiva) mostrarRegiao(regiaoInicialSaquarema());
  }, [buscaAtiva, rota, destino, mostrarRegiao]);

  const semCoordenadas = (lojas || []).length - lojasNoMapa.length;
  const avisoTexto =
    [
      semCoordenadas > 0 ? `${semCoordenadas} loja(s) sem localização no mapa.` : null,
      erroGps || null,
    ]
      .filter(Boolean)
      .join(' ') || null;

  const idsDestaque = useMemo(() => {
    if (!Array.isArray(lojaIdsDestaque)) return null;
    return new Set(lojaIdsDestaque.map(String));
  }, [lojaIdsDestaque]);

  function abrirLoja(loja) {
    const produto = (produtosPorLoja?.[loja.id] || produtosPorLoja?.[String(loja.id)] || [])[0];
    if (produto?.id && onProductPress) onProductPress(produto.id);
    else if (onStorePress) onStorePress(loja.id);
  }

  function descricaoLoja(loja, produto, distancia) {
    const linhas = [];
    if (produto) {
      linhas.push(produto.nome);
      if (produto.preco) linhas.push(produto.preco);
    } else if (buscaAtiva) {
      linhas.push('Esta loja não possui resultado para sua busca.');
    } else if (loja.endereco) {
      linhas.push(loja.endereco);
    }
    if (distancia) linhas.push(distancia);
    if (destino && String(destino.id) === String(loja.id)) linhas.push('Rota a partir do seu ponto');
    linhas.push('Toque para abrir');
    return linhas.join('\n');
  }

  return (
    <View style={[styles.container, edgeToEdge && styles.containerEdgeToEdge]}>
      {destino && buscaAtiva ? (
        <View style={styles.rotaBanner} pointerEvents="none">
          <Text style={styles.rotaBannerText}>Rota até {destino.nome}</Text>
        </View>
      ) : null}
      {edgeToEdge && avisoTexto ? (
        <View style={styles.bannerOverlay} pointerEvents="none">
          <Text style={styles.bannerText}>{avisoTexto}</Text>
        </View>
      ) : null}
      {!edgeToEdge && semCoordenadas > 0 ? (
        <Text style={appStyles.hint}>{semCoordenadas} loja(s) sem localização no mapa.</Text>
      ) : null}
      {!edgeToEdge && erroGps ? <Text style={appStyles.hint}>{erroGps}</Text> : null}
      <MapView
        ref={mapRef}
        style={[styles.map, edgeToEdge && styles.mapEdgeToEdge]}
        initialRegion={regiaoInicial}
        minZoomLevel={MIN_ZOOM_SAQUAREMA}
        maxZoomLevel={MAX_ZOOM_SAQUAREMA}
        onMapReady={aplicarLimites}
        onRegionChangeComplete={onRegionChangeComplete}
        rotateEnabled={false}
        pitchEnabled={false}
        toolbarEnabled={false}
        moveOnMarkerPress={false}
      >
        {rota.length >= 2 ? (
          <Polyline coordinates={rota} strokeColor={COR_ROTA} strokeWidth={4} />
        ) : null}
        {cliente ? (
          <Marker
            coordinate={cliente}
            title="Você"
            description="Ponto da busca em Saquarema"
            pinColor={COR_CLIENTE}
            anchor={{ x: 0.5, y: 1 }}
          />
        ) : null}
        {lojasNoMapa.map((loja) => {
          const id = String(loja.id);
          const destaque = idsDestaque?.has(id) === true;
          const ehDestino = destino && String(destino.id) === id;
          const produto = (produtosPorLoja?.[loja.id] || produtosPorLoja?.[id] || [])[0];
          const distancia = cliente
            ? formatarDistanciaKm(
                distanciaKm(cliente, { latitude: loja.lat, longitude: loja.lng })
              )
            : '';
          return (
            <Marker
              key={id}
              coordinate={{ latitude: loja.lat, longitude: loja.lng }}
              title={loja.nome}
              description={descricaoLoja(loja, produto, distancia)}
              pinColor={ehDestino ? COR_DESTINO : destaque ? COR_DESTAQUE : COR_LOJA}
              anchor={{ x: 0.5, y: 1 }}
              onPress={() => {
                if (buscaAtiva) setLojaDestinoId(id);
              }}
            >
              <Callout onPress={() => abrirLoja(loja)}>
                <View style={styles.callout}>
                  <Text style={styles.calloutTitulo}>{loja.nome}</Text>
                  {produto ? (
                    <>
                      <Text style={styles.calloutProduto}>{produto.nome}</Text>
                      {produto.preco ? <Text style={styles.calloutPreco}>{produto.preco}</Text> : null}
                    </>
                  ) : buscaAtiva ? (
                    <Text style={styles.calloutMeta}>Esta loja não possui resultado para sua busca.</Text>
                  ) : loja.endereco ? (
                    <Text style={styles.calloutMeta}>{loja.endereco}</Text>
                  ) : null}
                  {distancia ? <Text style={styles.calloutMeta}>{distancia}</Text> : null}
                  <Text style={styles.calloutAcao}>Toque para abrir</Text>
                </View>
              </Callout>
            </Marker>
          );
        })}
      </MapView>
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
  rotaBanner: {
    position: 'absolute',
    top: 12,
    left: 12,
    right: 12,
    zIndex: 20,
    backgroundColor: 'rgba(15, 118, 110, 0.92)',
    borderRadius: 8,
    paddingHorizontal: 12,
    paddingVertical: 6,
  },
  rotaBannerText: {
    color: '#fff',
    fontSize: 13,
    fontWeight: '700',
    textAlign: 'center',
  },
  bannerOverlay: {
    position: 'absolute',
    bottom: 12,
    left: 12,
    right: 12,
    zIndex: 20,
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
  callout: {
    maxWidth: 220,
    padding: 2,
  },
  calloutTitulo: {
    fontWeight: '700',
    fontSize: 15,
    color: '#0f172a',
  },
  calloutProduto: {
    marginTop: 4,
    fontSize: 13,
    color: '#334155',
  },
  calloutPreco: {
    marginTop: 2,
    color: '#0D9488',
    fontWeight: '700',
    fontSize: 15,
  },
  calloutMeta: {
    marginTop: 4,
    color: '#64748b',
    fontSize: 12,
  },
  calloutAcao: {
    marginTop: 6,
    color: '#0F766E',
    fontWeight: '600',
    fontSize: 12,
  },
});
