import { FlatList, ActivityIndicator, View, StyleSheet } from 'react-native';
import { useCallback, useState, useMemo, useEffect, useRef } from 'react';
import { useFocusEffect, useNavigation } from '@react-navigation/native';
import { listarFeedComStale, listarFeed } from '../../services/feedService';
import { listarLojasParaFeedMapa } from '../../services/lojaService';
import { registrarPesquisa } from '../../services/historicoService';
import {
  adicionarFavorito,
  listarFavoritosCliente,
  removerFavoritoProduto,
} from '../../services/favoritoService';
import { obterLocalizacaoAtual } from '../../services/locationService';
import { obterClima, normalizarCoordenadasClima } from '../../services/weatherService';
import { useAuth } from '../../context/AuthContext';
import { useTheme } from '../../context/ThemeContext';
import { useLayoutProfile } from '../../hooks/useLayoutProfile';
import { mapApiError } from '../../utils/apiErrorUtils';
import LojasMapView from '../../components/LojasMapView';
import ProductGridCard from '../../components/feed/ProductGridCard';
import MapSearchOverlay, {
  MODO_LISTA,
  MODO_MAPA,
  MAP_SEARCH_OVERLAY_HEIGHT,
} from '../../components/feed/MapSearchOverlay';
import WeatherCard, { WEATHER_CARD_SLOT_HEIGHT } from '../../components/feed/WeatherCard';
import {
  FormScreen,
  FormField,
  PrimaryButton,
  ListCardText,
  FormTabs,
} from '../../components/form';
import { montarMapaBuscaPorLoja } from '../../utils/mapaPinUtils';

const DEBOUNCE_BUSCA_MS = 400;
const DEBOUNCE_HISTORICO_MS = 1200;
const PAGE_SIZE = 20;
const MAX_PRODUTOS_POR_PIN = 4;
const GRID_PADDING_H = 8;
const GRID_GAP = 8;

function feedItemParaCard(item) {
  return {
    id: item.id,
    nome: item.nome,
    imagemUrl: item.imagemUrl,
    lojaId: item.lojaId,
    preco: item.preco,
    lojaNomeFantasia: item.lojaNome,
  };
}

function feedItemParaOferta(item) {
  return {
    preco: item.preco,
    precoAnterior: item.precoAnterior,
    emPromocao: item.emPromocao,
  };
}

export default function SearchScreen() {
  const navigation = useNavigation();
  const [termoBusca, setTermoBusca] = useState('');
  const [produtos, setProdutos] = useState([]);
  const [lojas, setLojas] = useState([]);
  const [localizacaoCliente, setLocalizacaoCliente] = useState(null);
  const [modoVisualizacao, setModoVisualizacao] = useState(MODO_MAPA);
  const [loading, setLoading] = useState(true);
  const [buscando, setBuscando] = useState(false);
  const [carregandoMais, setCarregandoMais] = useState(false);
  const [page, setPage] = useState(1);
  const [hasNext, setHasNext] = useState(false);
  const [feedError, setFeedError] = useState(null);
  const [favoritosIds, setFavoritosIds] = useState(() => new Set());
  const [clima, setClima] = useState({ status: 'loading', data: null });
  const [gpsResolvido, setGpsResolvido] = useState(false);
  const { session, isCliente, sincronizarGpsCliente } = useAuth();
  const { colors } = useTheme();
  const { gridColumns, useSidebarNav, width, sidebarWidth } = useLayoutProfile();

  const isSingleProduct = produtos.length === 1;
  const listColumns = isSingleProduct ? 1 : gridColumns;

  const singleCardWidth = useMemo(() => {
    const listAreaWidth = useSidebarNav ? width - sidebarWidth : width;
    return Math.floor(
      (listAreaWidth - GRID_PADDING_H * 2 - GRID_GAP * (gridColumns - 1)) / gridColumns
    );
  }, [width, sidebarWidth, useSidebarNav, gridColumns]);

  const clienteId = isCliente ? session?.perfil?.id : null;
  const buscaIdRef = useRef(0);
  const feedErroLogadoRef = useRef(false);
  const climaReqRef = useRef(0);
  const termoAtivo = termoBusca.trim();
  const termoAtivoRef = useRef(termoAtivo);
  termoAtivoRef.current = termoAtivo;
  const primeiraBuscaRef = useRef(true);

  const carregarLojasMapa = useCallback(async () => {
    try {
      const lista = await listarLojasParaFeedMapa();
      setLojas(Array.isArray(lista) ? lista : []);
    } catch {
      setLojas([]);
    }
  }, []);

  const carregarGps = useCallback(async () => {
    try {
      const coords = await obterLocalizacaoAtual();
      setLocalizacaoCliente(coords);
    } catch {
      const lat = session?.perfil?.latitudeAtual;
      const lng = session?.perfil?.longitudeAtual;
      if (lat != null && lng != null) {
        setLocalizacaoCliente({ latitude: Number(lat), longitude: Number(lng) });
      }
    } finally {
      setGpsResolvido(true);
    }
  }, [session?.perfil?.latitudeAtual, session?.perfil?.longitudeAtual]);

  const carregarFavoritos = useCallback(async () => {
    if (!clienteId) {
      setFavoritosIds(new Set());
      return;
    }
    try {
      const res = await listarFavoritosCliente(clienteId, 1, 200);
      const ids = new Set(
        (res.items || []).map((f) => String(f.produtoId)).filter((id) => id && id !== 'undefined')
      );
      setFavoritosIds(ids);
    } catch (error) {
      console.error('carregar favoritos:', error?.message || error);
    }
  }, [clienteId]);

  function registrarBuscaExecutada(termo, extra = {}) {
    if (!clienteId || !termo) return;
    registrarPesquisa(clienteId, termo, extra).catch((error) => {
      console.error('registrar pesquisa:', error?.message || error);
    });
  }

  const carregarFeed = useCallback(
    async (termo = '', pagina = 1, append = false, useStale = false) => {
      const loader = pagina === 1 && !append ? setLoading : setCarregandoMais;
      loader(true);
      try {
        let resultado;
        if (useStale && pagina === 1 && !append) {
          const res = await listarFeedComStale({
            page: pagina,
            pageSize: PAGE_SIZE,
            termo,
          });
          resultado = res.data;
        } else {
          resultado = await listarFeed({
            page: pagina,
            pageSize: PAGE_SIZE,
            termo,
            force: true,
          });
        }

        setPage(resultado.page);
        setHasNext(resultado.hasNext);
        setProdutos((prev) =>
          append ? [...prev, ...resultado.items] : resultado.items
        );
        setFeedError(null);
        feedErroLogadoRef.current = false;
      } catch (error) {
        if (!feedErroLogadoRef.current) {
          console.error('carregar feed:', error.message);
          feedErroLogadoRef.current = true;
        }
        if (pagina === 1 && !append) {
          setFeedError(mapApiError(error));
        }
      } finally {
        loader(false);
        setBuscando(false);
      }
    },
    []
  );

  useFocusEffect(
    useCallback(() => {
      carregarFeed(termoAtivoRef.current, 1, false, true);
      carregarLojasMapa();
      carregarGps();
      carregarFavoritos();
      if (isCliente) sincronizarGpsCliente();
    }, [isCliente, carregarFeed, carregarLojasMapa, carregarGps, carregarFavoritos, sincronizarGpsCliente])
  );

  const coordsClima = useMemo(
    () =>
      normalizarCoordenadasClima(
        localizacaoCliente?.latitude ?? session?.perfil?.latitudeAtual,
        localizacaoCliente?.longitude ?? session?.perfil?.longitudeAtual
      ),
    [
      localizacaoCliente?.latitude,
      localizacaoCliente?.longitude,
      session?.perfil?.latitudeAtual,
      session?.perfil?.longitudeAtual,
    ]
  );

  useFocusEffect(
    useCallback(() => {
      const req = ++climaReqRef.current;
      if (!coordsClima) {
        setClima({
          status: gpsResolvido ? 'sem-localizacao' : 'loading',
          data: null,
        });
        return undefined;
      }
      setClima((prev) => (prev.data ? prev : { status: 'loading', data: null }));
      obterClima(coordsClima).then((res) => {
        if (climaReqRef.current === req) setClima(res);
      });
      return undefined;
    }, [coordsClima, gpsResolvido])
  );

  const executarBusca = useCallback(
    async (termo) => {
      const buscaId = ++buscaIdRef.current;
      setBuscando(true);
      await carregarFeed(termo, 1, false, false);
      if (buscaId !== buscaIdRef.current) return;
    },
    [carregarFeed]
  );

  useEffect(() => {
    if (!termoAtivo) {
      buscaIdRef.current += 1;
      setBuscando(false);
      if (primeiraBuscaRef.current) {
        primeiraBuscaRef.current = false;
        return undefined;
      }
      carregarFeed('', 1, false, true);
      return undefined;
    }

    primeiraBuscaRef.current = false;
    const buscaTimer = setTimeout(() => executarBusca(termoAtivo), DEBOUNCE_BUSCA_MS);
    const historicoTimer = setTimeout(
      () => registrarBuscaExecutada(termoAtivo),
      DEBOUNCE_HISTORICO_MS
    );
    return () => {
      clearTimeout(buscaTimer);
      clearTimeout(historicoTimer);
    };
  }, [termoAtivo, executarBusca, carregarFeed]);

  async function handleSearch() {
    if (!termoAtivo) {
      await carregarFeed('', 1, false, true);
      return;
    }

    if (isCliente) sincronizarGpsCliente()?.catch?.(() => {});
    await executarBusca(termoAtivo);
    registrarBuscaExecutada(termoAtivo);
  }

  function carregarMais() {
    if (!hasNext || carregandoMais || loading) return;
    carregarFeed(termoAtivo, page + 1, true, false);
  }

  const lojaIdsDestaque = useMemo(() => {
    if (!termoAtivo) return null;
    const ids = new Set(produtos.map((p) => p.lojaId).filter(Boolean));
    return [...ids].map(String);
  }, [produtos, termoAtivo]);

  const { produtosPorLoja, imagemPinPorLoja } = useMemo(
    () => montarMapaBuscaPorLoja(produtos, termoAtivo, MAX_PRODUTOS_POR_PIN),
    [produtos, termoAtivo]
  );

  function abrirProduto(productId, produto) {
    const id = productId ?? produto?.id ?? produto?.Id;
    if (!id) return;

    if (clienteId && termoAtivo) {
      registrarBuscaExecutada(termoAtivo, {
        produtoId: id,
        lojaId: produto?.lojaId ?? null,
      });
    }
    navigation.navigate({
      name: 'ProductDetail',
      params: { productId: String(id) },
      merge: true,
    });
  }

  function abrirProdutoDoMapa(productId) {
    const produto = produtos.find((p) => String(p.id) === String(productId));
    abrirProduto(productId, produto);
  }

  function abrirCatalogoLoja(lojaId) {
    if (!lojaId) return;
    const loja = lojas.find((l) => String(l.id ?? l.Id) === String(lojaId));
    navigation.navigate({
      name: 'StoreCatalog',
      params: {
        lojaId: String(lojaId),
        lojaNome: loja?.nomeFantasia || loja?.NomeFantasia,
      },
      merge: true,
    });
  }

  async function handleFavorito(item) {
    if (!clienteId || !item?.id) return;
    const id = String(item.id);
    const jaFavorito = favoritosIds.has(id);
    setFavoritosIds((prev) => {
      const next = new Set(prev);
      if (jaFavorito) next.delete(id);
      else next.add(id);
      return next;
    });
    try {
      if (jaFavorito) {
        await removerFavoritoProduto(clienteId, id);
      } else {
        await adicionarFavorito({
          clienteId,
          produtoId: id,
          lojaId: item.lojaId ?? null,
        });
      }
    } catch (error) {
      console.error('favorito:', error?.message || error);
      setFavoritosIds((prev) => {
        const next = new Set(prev);
        if (jaFavorito) next.add(id);
        else next.delete(id);
        return next;
      });
    }
  }

  function renderItem({ item }) {
    const card = (
      <ProductGridCard
        produto={feedItemParaCard(item)}
        oferta={feedItemParaOferta(item)}
        onPress={() => abrirProduto(item.id, item)}
        fillCell={!isSingleProduct}
        ehFavorito={favoritosIds.has(String(item.id))}
        mostrarFavorito={Boolean(clienteId)}
        onFavorito={() => handleFavorito(item)}
      />
    );

    if (isSingleProduct) {
      return <View style={{ width: singleCardWidth }}>{card}</View>;
    }

    return card;
  }

  function renderClassicLayout() {
    return (
      <>
        <View style={styles.searchRow}>
          <View style={styles.searchInputWrap}>
            <FormField
              label=""
              value={termoBusca}
              onChangeText={setTermoBusca}
              placeholder="Buscar produtos..."
              onSubmitEditing={handleSearch}
              returnKeyType="search"
              compact
            />
          </View>
          <PrimaryButton label="Buscar" onPress={handleSearch} style={styles.searchButton} />
        </View>

        <WeatherCard
          status={clima.status}
          dados={clima.data}
          stale={clima.stale}
          style={styles.weatherClassic}
        />

        <FormTabs
          options={[
            { value: MODO_MAPA, label: 'Mapa' },
            { value: MODO_LISTA, label: 'Lista' },
          ]}
          value={modoVisualizacao}
          onChange={setModoVisualizacao}
        />

        {buscando ? (
          <ActivityIndicator size="small" color={colors.primary} style={styles.buscandoIndicator} />
        ) : null}

        {renderContent(false)}
      </>
    );
  }

  function renderSidebarLayout() {
    return (
      <View style={styles.mapShell}>
        {renderContent(true)}
        <MapSearchOverlay
          termoBusca={termoBusca}
          onChangeText={setTermoBusca}
          onSubmit={handleSearch}
          modoVisualizacao={modoVisualizacao}
          onModoChange={setModoVisualizacao}
          buscando={buscando}
        />
        <View style={styles.weatherOverlay} pointerEvents="none">
          <WeatherCard status={clima.status} dados={clima.data} stale={clima.stale} />
        </View>
      </View>
    );
  }

  function renderContent(edgeToEdge) {
    if (modoVisualizacao === MODO_MAPA) {
      return (
        <View style={edgeToEdge ? styles.mapShell : styles.mapWithSpinner}>
          <LojasMapView
            lojas={lojas}
            localizacaoCliente={localizacaoCliente}
            lojaIdsDestaque={lojaIdsDestaque}
            produtosPorLoja={produtosPorLoja}
            imagemPinPorLoja={imagemPinPorLoja}
            buscaAtiva={Boolean(termoAtivo)}
            onProductPress={abrirProdutoDoMapa}
            onStorePress={abrirCatalogoLoja}
            edgeToEdge={edgeToEdge}
          />
          {loading ? (
            <ActivityIndicator
              size="small"
              color={colors.primary}
              style={styles.mapFeedSpinner}
            />
          ) : null}
        </View>
      );
    }

    if (loading) {
      return (
        <ActivityIndicator
          size="large"
          color={colors.primary}
          style={edgeToEdge ? styles.loadingOverlay : { marginTop: 24 }}
        />
      );
    }

    return (
      <FlatList
        style={[styles.gridList, { backgroundColor: colors.listBackground }]}
        contentContainerStyle={[
          styles.gridContent,
          edgeToEdge && { paddingTop: MAP_SEARCH_OVERLAY_HEIGHT + WEATHER_CARD_SLOT_HEIGHT + 16 },
          isSingleProduct && styles.gridContentSingle,
        ]}
        data={produtos}
        keyExtractor={(item) => String(item.id)}
        renderItem={renderItem}
        numColumns={listColumns}
        key={`grid-${listColumns}-${isSingleProduct ? 'single' : 'multi'}`}
        columnWrapperStyle={listColumns > 1 ? styles.gridRow : undefined}
        showsVerticalScrollIndicator={false}
        keyboardShouldPersistTaps="handled"
        windowSize={5}
        maxToRenderPerBatch={10}
        removeClippedSubviews
        onEndReached={carregarMais}
        onEndReachedThreshold={0.4}
        ListFooterComponent={
          carregandoMais ? (
            <ActivityIndicator size="small" color={colors.primary} style={{ marginVertical: 12 }} />
          ) : null
        }
        ListEmptyComponent={
          <ListCardText style={[styles.emptyText, { color: colors.textMuted }]}>
            {termoAtivo
              ? 'Nenhum produto encontrado para essa busca.'
              : 'Nenhum produto cadastrado ainda.'}
          </ListCardText>
        }
      />
    );
  }

  return (
    <FormScreen
      title="Buscar produtos"
      subtitle="Encontre as melhores ofertas"
      scrollable={false}
      fullBleed={useSidebarNav}
      hideHeader={useSidebarNav}
      noBodyPadding={useSidebarNav}
      error={feedError}
    >
      {useSidebarNav ? renderSidebarLayout() : renderClassicLayout()}
    </FormScreen>
  );
}

const styles = StyleSheet.create({
  mapShell: {
    flex: 1,
    position: 'relative',
  },
  mapWithSpinner: {
    flex: 1,
    minHeight: 320,
  },
  mapFeedSpinner: {
    position: 'absolute',
    top: 12,
    right: 12,
    zIndex: 6,
  },
  searchRow: {
    flexDirection: 'row',
    alignItems: 'flex-end',
    gap: 8,
    marginBottom: 12,
  },
  searchInputWrap: { flex: 1 },
  searchButton: { marginBottom: 10, paddingHorizontal: 16, paddingVertical: 12 },
  weatherClassic: { marginBottom: 8 },
  weatherOverlay: {
    position: 'absolute',
    top: MAP_SEARCH_OVERLAY_HEIGHT,
    left: 16,
    right: 16,
    zIndex: 9,
  },
  buscandoIndicator: { marginVertical: 4 },
  loadingOverlay: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
  },
  gridList: { flex: 1 },
  gridContent: { paddingHorizontal: GRID_PADDING_H, paddingTop: 8, paddingBottom: 16 },
  gridContentSingle: { alignItems: 'center' },
  gridRow: { gap: GRID_GAP },
  emptyText: { textAlign: 'center', marginTop: 24 },
});
