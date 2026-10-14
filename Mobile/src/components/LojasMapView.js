import { useEffect, useMemo, useState } from 'react';
import { View, Text, Image, Pressable, StyleSheet } from 'react-native';
import MapView, { Marker, UrlTile, PROVIDER_DEFAULT } from 'react-native-maps';
import { obterLocalizacaoAtual } from '../services/locationService';
import { normalizarLojaParaMapa } from '../utils/leafletMapHtml';
import { SAQUAREMA_CENTER } from '../utils/mapaUtils';
import { styles as appStyles } from '../theme';

/**
 * Mapa do feed com lojas (Android/iOS) usando react-native-maps.
 * A versão web continua em LojasMapView.web.js (Leaflet).
 * Enquanto o usuário busca, pins com resultado ganham destaque.
 */
const REGIAO_INICIAL = {
  latitude: SAQUAREMA_CENTER[0],
  longitude: SAQUAREMA_CENTER[1],
  latitudeDelta: 0.08,
  longitudeDelta: 0.08,
};

function haversineKm(lat1, lng1, lat2, lng2) {
  const R = 6371;
  const toRad = (v) => (v * Math.PI) / 180;
  const dLat = toRad(lat2 - lat1);
  const dLng = toRad(lng2 - lng1);
  const a =
    Math.sin(dLat / 2) ** 2 +
    Math.cos(toRad(lat1)) * Math.cos(toRad(lat2)) * Math.sin(dLng / 2) ** 2;
  return R * 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));
}

function formatarDistancia(cliente, loja) {
  if (!cliente) return '';
  const km = haversineKm(
    Number(cliente.latitude),
    Number(cliente.longitude),
    loja.lat,
    loja.lng
  );
  if (!Number.isFinite(km)) return '';
  if (km < 1) return `${Math.round(km * 1000)} m`;
  return `${km.toFixed(1).replace('.', ',')} km`;
}

function PinLoja({ loja, destaque, imagemUrl }) {
  const size = destaque ? 52 : 40;
  return (
    <View
      style={[
        styles.pin,
        { width: size, height: size, borderRadius: size / 2 },
        destaque && styles.pinDestaque,
        imagemUrl && styles.pinImg,
      ]}
    >
      {imagemUrl ? (
        <Image source={{ uri: imagemUrl }} style={styles.pinImagem} />
      ) : (
        <Text style={[styles.pinTexto, destaque && styles.pinTextoDestaque]}>
          {loja.inicial}
        </Text>
      )}
    </View>
  );
}

export default function LojasMapView({
  lojas,
  localizacaoCliente: localizacaoExterna,
  lojaIdsDestaque = null,
  produtosPorLoja = {},
  imagemPinPorLoja = {},
  buscaAtiva = false,
  onProductPress,
  onStorePress,
  edgeToEdge = false,
}) {
  const [localizacaoInterna, setLocalizacaoInterna] = useState(null);
  const [erroGps, setErroGps] = useState(null);
  const [lojaSelecionadaId, setLojaSelecionadaId] = useState(null);
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

  const idsDestaque = useMemo(
    () => (lojaIdsDestaque ? new Set(lojaIdsDestaque.map(String)) : null),
    [lojaIdsDestaque]
  );

  const semCoordenadas = (lojas || []).length - lojasNoMapa.length;
  const avisoTexto =
    [
      semCoordenadas > 0 ? `${semCoordenadas} loja(s) sem localização no mapa.` : null,
      erroGps || null,
    ]
      .filter(Boolean)
      .join(' ') || null;

  const lojaSelecionada = lojasNoMapa.find(
    (l) => String(l.id) === String(lojaSelecionadaId)
  );

  function renderCard() {
    if (!lojaSelecionada) return null;
    const produto = (produtosPorLoja?.[lojaSelecionada.id] || [])[0];
    const dist = formatarDistancia(localizacaoCliente, lojaSelecionada);

    return (
      <View style={styles.card}>
        <Pressable style={styles.cardFechar} onPress={() => setLojaSelecionadaId(null)} hitSlop={10}>
          <Text style={styles.cardFecharTexto}>✕</Text>
        </Pressable>
        <Text style={styles.cardTitulo}>{lojaSelecionada.nome}</Text>
        {produto ? (
          <>
            {produto.imagemUrl ? (
              <Image source={{ uri: produto.imagemUrl }} style={styles.cardImg} />
            ) : null}
            <Text style={styles.cardProduto}>{produto.nome}</Text>
            <Text style={styles.cardPreco}>{produto.preco}</Text>
          </>
        ) : buscaAtiva ? (
          <Text style={styles.cardVazio}>Esta loja não possui resultado para sua busca.</Text>
        ) : lojaSelecionada.endereco ? (
          <Text style={styles.cardMeta}>{lojaSelecionada.endereco}</Text>
        ) : null}
        {dist ? <Text style={styles.cardMeta}>{dist}</Text> : null}
        <Pressable
          style={styles.cardBotao}
          onPress={() =>
            produto
              ? onProductPress?.(String(produto.id))
              : onStorePress?.(String(lojaSelecionada.id))
          }
        >
          <Text style={styles.cardBotaoTexto}>{produto ? 'Ver produto' : 'Ver produtos'}</Text>
        </Pressable>
      </View>
    );
  }

  return (
    <View style={[styles.container, edgeToEdge && styles.containerEdgeToEdge]}>
      {!edgeToEdge && semCoordenadas > 0 ? (
        <Text style={appStyles.hint}>{semCoordenadas} loja(s) sem localização no mapa.</Text>
      ) : null}
      {!edgeToEdge && erroGps ? <Text style={appStyles.hint}>{erroGps}</Text> : null}
      <View style={[styles.map, edgeToEdge && styles.mapEdgeToEdge]}>
        <MapView
          style={StyleSheet.absoluteFill}
          provider={PROVIDER_DEFAULT}
          mapType="none"
          initialRegion={REGIAO_INICIAL}
          showsUserLocation={Boolean(localizacaoCliente)}
          showsMyLocationButton={false}
          toolbarEnabled={false}
          onPress={() => setLojaSelecionadaId(null)}
        >
          <UrlTile
            urlTemplate="https://tile.openstreetmap.org/{z}/{x}/{y}.png"
            maximumZ={19}
            flipY={false}
            tileSize={256}
          />
          {lojasNoMapa.map((loja) => {
            const destaque = idsDestaque !== null && idsDestaque.has(String(loja.id));
            const imagemUrl = destaque ? imagemPinPorLoja?.[loja.id] || null : null;
            return (
              <Marker
                key={`${loja.id}-${destaque ? 'd' : 'n'}-${imagemUrl ? 'i' : ''}`}
                coordinate={{ latitude: loja.lat, longitude: loja.lng }}
                anchor={{ x: 0.5, y: 1 }}
                tracksViewChanges={Boolean(imagemUrl)}
                zIndex={destaque ? 2 : 1}
                onPress={(e) => {
                  e.stopPropagation?.();
                  setLojaSelecionadaId(loja.id);
                }}
              >
                <PinLoja loja={loja} destaque={destaque} imagemUrl={imagemUrl} />
              </Marker>
            );
          })}
        </MapView>
        <Text style={styles.atribuicao} pointerEvents="none">© OpenStreetMap</Text>
        {renderCard()}
      </View>
      {edgeToEdge && avisoTexto && !lojaSelecionada ? (
        <View style={styles.bannerOverlay} pointerEvents="none">
          <Text style={styles.bannerText}>{avisoTexto}</Text>
        </View>
      ) : null}
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, minHeight: 320 },
  containerEdgeToEdge: { minHeight: 0 },
  map: {
    flex: 1,
    borderRadius: 12,
    overflow: 'hidden',
    backgroundColor: '#e2e8f0',
  },
  mapEdgeToEdge: { borderRadius: 0 },
  pin: {
    borderWidth: 2,
    borderColor: '#2DD4BF',
    backgroundColor: '#14B8A6',
    alignItems: 'center',
    justifyContent: 'center',
    overflow: 'hidden',
  },
  pinDestaque: { backgroundColor: '#0D9488', borderWidth: 3, borderColor: '#F59E0B' },
  pinImg: { backgroundColor: '#fff' },
  pinImagem: { width: '100%', height: '100%' },
  pinTexto: { color: '#fff', fontWeight: 'bold', fontSize: 17 },
  pinTextoDestaque: { fontSize: 22 },
  atribuicao: {
    position: 'absolute',
    right: 4,
    bottom: 2,
    fontSize: 10,
    color: '#334155',
    backgroundColor: 'rgba(255,255,255,0.7)',
    paddingHorizontal: 4,
  },
  card: {
    position: 'absolute',
    left: 12,
    right: 12,
    bottom: 16,
    backgroundColor: '#fff',
    borderRadius: 12,
    padding: 14,
    elevation: 6,
    shadowColor: '#0f172a',
    shadowOpacity: 0.18,
    shadowRadius: 12,
    shadowOffset: { width: 0, height: 4 },
  },
  cardFechar: { position: 'absolute', top: 8, right: 10, zIndex: 1 },
  cardFecharTexto: { fontSize: 16, color: '#64748b' },
  cardTitulo: { fontWeight: '700', fontSize: 15, color: '#0f172a', paddingRight: 20 },
  cardImg: {
    width: '100%',
    height: 88,
    borderRadius: 8,
    marginTop: 8,
    backgroundColor: '#e2e8f0',
  },
  cardProduto: { marginTop: 6, fontSize: 13, color: '#334155' },
  cardPreco: { marginTop: 2, color: '#0D9488', fontWeight: '700', fontSize: 16 },
  cardMeta: { color: '#64748b', fontSize: 12, marginTop: 4 },
  cardVazio: { marginTop: 8, fontSize: 13, color: '#475569' },
  cardBotao: {
    marginTop: 10,
    paddingVertical: 8,
    backgroundColor: '#14B8A6',
    borderRadius: 8,
    alignItems: 'center',
  },
  cardBotaoTexto: { color: '#fff', fontWeight: '600', fontSize: 13 },
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
  bannerText: { color: '#fff', fontSize: 12, textAlign: 'center' },
});

