import {
  FlatList,
  ActivityIndicator,
  View,
  Text,
  TextInput,
  Pressable,
  Image,
  ScrollView,
  Linking,
  StyleSheet,
  Alert,
} from 'react-native';
import { useCallback, useState, useMemo, useEffect, useRef } from 'react';
import { useFocusEffect, useNavigation, useRoute } from '@react-navigation/native';
import { Ionicons } from '@expo/vector-icons';
import { listarFeedComStale, listarFeed } from '../../services/feedService';
import { listarLojasParaFeedMapa } from '../../services/lojaService';
import {
  registrarPesquisa,
  listarUltimosTermosCliente,
} from '../../services/historicoService';
import {
  adicionarFavorito,
  listarFavoritosCliente,
  removerFavoritoProduto,
} from '../../services/favoritoService';
import { abrirConversa } from '../../services/chatService';
import { navegarParaChat } from '../../navigation/chatNavigation';
import { obterLocalizacaoAtual } from '../../services/locationService';
import { obterClima, normalizarCoordenadasClima } from '../../services/weatherService';
import { useAuth } from '../../context/AuthContext';
import { useTheme } from '../../context/ThemeContext';
import { useLayoutProfile } from '../../hooks/useLayoutProfile';
import { mapApiError } from '../../utils/apiErrorUtils';
import LojasMapView from '../../components/LojasMapView';
import { MODO_LISTA, MODO_MAPA } from '../../components/feed/MapSearchOverlay';
import WeatherCard from '../../components/feed/WeatherCard';
import { FormScreen, ListCardText } from '../../components/form';
import {
  AiBannerCard,
  PillBadge,
  SegmentedControl,
} from '../../components/ui/DesignPrimitives';
import { montarMapaBuscaPorLoja } from '../../utils/mapaPinUtils';
import { COORDENADAS_SAQUAREMA, formatarPrecoBrl } from '../../utils/mapaUtils';
import { montarUrlRotaGoogleMaps } from '../../utils/geo';
import {
  calcularEconomiaBusca,
  ordenarEFiltrarResultadosBusca,
} from '../../utils/insights';
import { fonts, radii, shadows } from '../../theme';

const DEBOUNCE_BUSCA_MS = 400;
const DEBOUNCE_HISTORICO_MS = 1200;
const PAGE_SIZE = 20;
const MAX_PRODUTOS_POR_PIN = 4;
const DEFAULT_CHIPS = [
  'Café',
  'Arroz',
  'Leite',
  'Fralda',
  'Ração',
  'Limpeza',
];

export default function SearchScreen() {
  const navigation = useNavigation();
  const route = useRoute();
  const [termoBusca, setTermoBusca] = useState('');
  const [produtos, setProdutos] = useState([]);
  const [lojas, setLojas] = useState([]);
  const [localizacaoCliente, setLocalizacaoCliente] = useState(null);
  const [modoVisualizacao, setModoVisualizacao] = useState(MODO_LISTA);
  const [ordenarPor, setOrdenarPor] = useState('preco');
  const [apenasPromocao, setApenasPromocao] = useState(false);
  const [mostrarFiltros, setMostrarFiltros] = useState(false);
  const [selectedId, setSelectedId] = useState(null);
  const [chipsSugestao, setChipsSugestao] = useState(DEFAULT_CHIPS);
  const [abrindoChatId, setAbrindoChatId] = useState(null);
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
  const { isDesktopWeb, useSidebarNav } = useLayoutProfile();

  const clienteId = isCliente ? session?.perfil?.id : null;
  const buscaIdRef = useRef(0);
  const feedErroLogadoRef = useRef(false);
  const climaReqRef = useRef(0);
  const termoAtivo = termoBusca.trim();
  const termoAtivoRef = useRef(termoAtivo);
  termoAtivoRef.current = termoAtivo;
  const primeiraBuscaRef = useRef(true);

  useEffect(() => {
    const inicial = route?.params?.termoInicial;
    if (typeof inicial === 'string' && inicial.trim()) {
      setTermoBusca(inicial.trim());
      navigation.setParams?.({ termoInicial: undefined });
    }
  }, [route?.params?.termoInicial, navigation]);

  const carregarLojasMapa = useCallback(async () => {
    try {
      const lista = await listarLojasParaFeedMapa();
      setLojas(Array.isArray(lista) ? lista : []);
    } catch {
      setLojas([]);
    }
  }, []);

  const carregarTermosRecentes = useCallback(async () => {
    if (!clienteId) {
      setChipsSugestao(DEFAULT_CHIPS);
      return;
    }
    try {
      const recentes = await listarUltimosTermosCliente(clienteId, 6);
      const limpos = (Array.isArray(recentes) ? recentes : [])
        .map((t) => (typeof t === 'string' ? t.trim() : t?.termoPesquisa?.trim()))
        .filter(Boolean);
      const combinados = [...new Set([...limpos, ...DEFAULT_CHIPS])].slice(0, 6);
      setChipsSugestao(combinados.length > 0 ? combinados : DEFAULT_CHIPS);
    } catch {
      setChipsSugestao(DEFAULT_CHIPS);
    }
  }, [clienteId]);

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
        (res.items || [])
          .map((f) => String(f.produtoId))
          .filter((id) => id && id !== 'undefined')
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
      carregarTermosRecentes();
      if (isCliente) sincronizarGpsCliente();
    }, [
      isCliente,
      carregarFeed,
      carregarLojasMapa,
      carregarGps,
      carregarFavoritos,
      carregarTermosRecentes,
      sincronizarGpsCliente,
    ])
  );

  const coordsClima = useMemo(
    () =>
      normalizarCoordenadasClima(
        localizacaoCliente?.latitude ??
          session?.perfil?.latitudeAtual ??
          COORDENADAS_SAQUAREMA.latitude,
        localizacaoCliente?.longitude ??
          session?.perfil?.longitudeAtual ??
          COORDENADAS_SAQUAREMA.longitude
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

  function handleChipPress(chip) {
    if (termoBusca.trim().toLowerCase() === chip.toLowerCase()) {
      setTermoBusca('');
    } else {
      setTermoBusca(chip);
    }
  }

  function carregarMais() {
    if (!hasNext || carregandoMais || loading) return;
    carregarFeed(termoAtivo, page + 1, true, false);
  }

  const produtosEnriquecidos = useMemo(
    () =>
      ordenarEFiltrarResultadosBusca(produtos, lojas, coordsClima, {
        ordenarPor,
        apenasPromocao,
      }),
    [produtos, lojas, coordsClima, ordenarPor, apenasPromocao]
  );

  const economiaInfo = useMemo(
    () => calcularEconomiaBusca(produtosEnriquecidos),
    [produtosEnriquecidos]
  );

  useEffect(() => {
    if (produtosEnriquecidos.length === 0) {
      setSelectedId(null);
      return;
    }
    const existe = produtosEnriquecidos.some(
      (p) => String(p.id) === String(selectedId)
    );
    if (!existe) {
      setSelectedId(produtosEnriquecidos[0].id);
    }
  }, [produtosEnriquecidos, selectedId]);

  const selectedItem = useMemo(
    () =>
      produtosEnriquecidos.find((p) => String(p.id) === String(selectedId)) ||
      produtosEnriquecidos[0] ||
      null,
    [produtosEnriquecidos, selectedId]
  );

  const lojaIdsDestaque = useMemo(() => {
    if (selectedItem?.lojaId) return [String(selectedItem.lojaId)];
    if (!termoAtivo) return null;
    const ids = new Set(produtosEnriquecidos.map((p) => p.lojaId).filter(Boolean));
    return [...ids].map(String);
  }, [produtosEnriquecidos, termoAtivo, selectedItem]);

  const { produtosPorLoja, imagemPinPorLoja } = useMemo(
    () => montarMapaBuscaPorLoja(produtosEnriquecidos, termoAtivo, MAX_PRODUTOS_POR_PIN),
    [produtosEnriquecidos, termoAtivo]
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
    const produto = produtosEnriquecidos.find(
      (p) => String(p.id) === String(productId)
    );
    if (produto) {
      setSelectedId(produto.id);
    }
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

  async function abrirRotaParaItem(item) {
    if (!item) return;
    const url = montarUrlRotaGoogleMaps({
      latDestino: item.lojaLat,
      lngDestino: item.lojaLng,
      enderecoDestino: item.lojaEnderecoTexto || item.lojaNome || item.nome,
      latOrigem: coordsClima?.latitude,
      lngOrigem: coordsClima?.longitude,
    });
    if (!url) {
      Alert.alert('Rota', 'Endereço da loja não disponível para rota.');
      return;
    }
    try {
      await Linking.openURL(url);
    } catch {
      Alert.alert('Rota', 'Não foi possível abrir o aplicativo de mapas.');
    }
  }

  async function handleAbrirChatItem(item) {
    if (!item) return;
    if (!clienteId) {
      Alert.alert('Chat', 'Faça login como cliente para conversar com a loja.');
      return;
    }
    const loja = lojas.find((l) => String(l.id ?? l.Id) === String(item.lojaId));
    const lojaCodigo =
      item.lojaCodigoPublico ||
      loja?.codigoPublico ||
      loja?.CodigoPublico ||
      item.lojaId;

    if (!lojaCodigo) {
      Alert.alert('Chat', 'Loja não identificada para este produto.');
      return;
    }

    setAbrindoChatId(String(item.id));
    try {
      const data = await abrirConversa(lojaCodigo, { produtoId: item.id });
      navegarParaChat(navigation, {
        conversaCodigo: data.codigoPublico,
        titulo: item.lojaNome || loja?.nomeFantasia || 'Loja',
      });
    } catch (error) {
      const detalhe = error?.response?.data?.message;
      Alert.alert('Chat', detalhe || 'Não foi possível iniciar a conversa com a loja.');
    } finally {
      setAbrindoChatId(null);
    }
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
      const status = error?.response?.status;
      Alert.alert(
        'Favoritos',
        status === 403
          ? 'Não foi possível favoritar neste modo. Entre de novo e tente outra vez.'
          : 'Não foi possível atualizar os favoritos.'
      );
    }
  }

  const cidadeHeader = useMemo(() => {
    if (clima.data?.localidade?.cidade) {
      return [clima.data.localidade.cidade, clima.data.localidade.estado]
        .filter(Boolean)
        .join(', ');
    }
    return 'Centro, Saquarema';
  }, [clima.data]);

  function alternarOrdenacao() {
    setOrdenarPor((prev) => (prev === 'preco' ? 'distancia' : 'preco'));
  }

  function renderResultCard({ item }) {
    const isSelected = String(item.id) === String(selectedItem?.id);
    const ehFavorito = favoritosIds.has(String(item.id));
    const temDesconto =
      item.precoAnterior != null && Number(item.precoAnterior) > Number(item.preco);

    return (
      <Pressable
        onPress={() => {
          if (isDesktopWeb) {
            if (isSelected) {
              abrirProduto(item.id, item);
            } else {
              setSelectedId(item.id);
            }
          } else {
            setSelectedId(item.id);
            abrirProduto(item.id, item);
          }
        }}
        style={[
          styles.resultCard,
          {
            backgroundColor: colors.card,
            borderColor: isSelected ? colors.primary : colors.border,
          },
          isSelected && styles.resultCardSelected,
        ]}
      >
        <View style={styles.resultRow}>
          <View
            style={[
              styles.storeIconWrap,
              { backgroundColor: colors.surfaceSubtle },
            ]}
          >
            {item.imagemUrl ? (
              <Image source={{ uri: item.imagemUrl }} style={styles.productThumb} />
            ) : (
              <Ionicons name="storefront-outline" size={20} color={colors.primary} />
            )}
          </View>

          <View style={styles.resultMain}>
            <View style={styles.storeTitleRow}>
              <Text
                style={[styles.storeName, { color: colors.text }]}
                numberOfLines={1}
              >
                {item.lojaNome || 'Loja parceira'}
              </Text>
              {item.melhorPreco ? (
                <PillBadge label="MELHOR PREÇO" variant="highlight" />
              ) : null}
            </View>

            <Text
              style={[styles.productName, { color: colors.textSecondary }]}
              numberOfLines={1}
            >
              {item.nome}
            </Text>

            <View style={styles.metaRow}>
              {item.distanciaFormatada ? (
                <View style={styles.metaItem}>
                  <Ionicons
                    name="location-outline"
                    size={12}
                    color={colors.textMuted}
                  />
                  <Text style={[styles.metaText, { color: colors.textMuted }]}>
                    {item.distanciaFormatada}
                  </Text>
                </View>
              ) : null}

              {item.caminhadaFormatada ? (
                <View style={styles.metaItem}>
                  <Ionicons
                    name="walk-outline"
                    size={12}
                    color={colors.textMuted}
                  />
                  <Text style={[styles.metaText, { color: colors.textMuted }]}>
                    {item.caminhadaFormatada}
                  </Text>
                </View>
              ) : null}

              {item.notaLoja != null ? (
                <View style={styles.metaItem}>
                  <Ionicons name="star" size={12} color={colors.warning} />
                  <Text style={[styles.metaText, { color: colors.textMuted }]}>
                    {Number(item.notaLoja).toFixed(1)}
                  </Text>
                </View>
              ) : null}

              <Pressable
                onPress={(e) => {
                  e?.stopPropagation?.();
                  abrirProduto(item.id, item);
                }}
                hitSlop={6}
              >
                <Text style={[styles.verDetalhesLink, { color: colors.primary }]}>
                  Ver produto →
                </Text>
              </Pressable>
            </View>
          </View>

          <View style={styles.priceColumn}>
            <Text style={[styles.priceValue, { color: colors.text }]}>
              {formatarPrecoBrl(item.preco)}
            </Text>
            {temDesconto ? (
              <Text style={[styles.oldPriceValue, { color: colors.textMuted }]}>
                {formatarPrecoBrl(item.precoAnterior)}
              </Text>
            ) : null}
            {clienteId ? (
              <Pressable
                onPress={(e) => {
                  e?.stopPropagation?.();
                  handleFavorito(item);
                }}
                hitSlop={8}
                style={styles.cardFavBtn}
                accessibilityRole="button"
                accessibilityLabel={
                  ehFavorito ? 'Remover dos favoritos' : 'Adicionar aos favoritos'
                }
              >
                <Ionicons
                  name={ehFavorito ? 'heart' : 'heart-outline'}
                  size={17}
                  color={ehFavorito ? colors.danger : colors.textMuted}
                />
              </Pressable>
            ) : null}
          </View>
        </View>
      </Pressable>
    );
  }

  function renderMapFloatingCard() {
    if (!selectedItem) return null;
    const ehFavorito = favoritosIds.has(String(selectedItem.id));
    const distanciaTrilha = [
      selectedItem.distanciaFormatada,
      selectedItem.caminhadaFormatada,
    ]
      .filter(Boolean)
      .join(' · ');

    return (
      <View
        style={[
          styles.floatingStoreCard,
          { backgroundColor: colors.card, borderColor: colors.border },
        ]}
      >
        <View style={styles.floatingHeader}>
          <View style={{ flex: 1 }}>
            <Text style={[styles.floatingEyebrow, { color: colors.primary }]}>
              {distanciaTrilha || 'Disponível na região'}
            </Text>
            <Pressable
              onPress={() =>
                selectedItem.lojaId && abrirCatalogoLoja(selectedItem.lojaId)
              }
            >
              <Text
                style={[styles.floatingStoreTitle, { color: colors.text }]}
                numberOfLines={1}
              >
                {selectedItem.lojaNome || 'Loja parceira'}
              </Text>
            </Pressable>
            <Pressable onPress={() => abrirProduto(selectedItem.id, selectedItem)}>
              <Text
                style={[styles.floatingProductSub, { color: colors.textMuted }]}
                numberOfLines={1}
              >
                {selectedItem.nome} · ver detalhes
              </Text>
            </Pressable>
          </View>
          <Text style={[styles.floatingPrice, { color: colors.text }]}>
            {formatarPrecoBrl(selectedItem.preco)}
          </Text>
        </View>

        <View style={styles.floatingActions}>
          <Pressable
            onPress={() => abrirRotaParaItem(selectedItem)}
            style={[styles.routeBtn, { backgroundColor: colors.primary }]}
            accessibilityRole="button"
            accessibilityLabel="Como chegar"
          >
            <Ionicons
              name="navigate-outline"
              size={16}
              color={colors.primaryForeground}
            />
            <Text
              style={[
                styles.routeBtnText,
                { color: colors.primaryForeground },
              ]}
            >
              Como chegar
            </Text>
          </Pressable>

          <Pressable
            onPress={() => abrirProduto(selectedItem.id, selectedItem)}
            style={[
              styles.iconActionBtn,
              { borderColor: colors.border, backgroundColor: colors.surface },
            ]}
            accessibilityRole="button"
            accessibilityLabel="Ver produto"
          >
            <Ionicons name="pricetag-outline" size={17} color={colors.text} />
          </Pressable>

          {clienteId ? (
            <Pressable
              onPress={() => handleFavorito(selectedItem)}
              style={[
                styles.iconActionBtn,
                { borderColor: colors.border, backgroundColor: colors.surface },
              ]}
              accessibilityRole="button"
              accessibilityLabel={
                ehFavorito ? 'Remover dos favoritos' : 'Favoritar produto'
              }
            >
              <Ionicons
                name={ehFavorito ? 'heart' : 'heart-outline'}
                size={17}
                color={ehFavorito ? colors.danger : colors.text}
              />
            </Pressable>
          ) : null}

          <Pressable
            onPress={() => handleAbrirChatItem(selectedItem)}
            disabled={abrindoChatId === String(selectedItem.id)}
            style={[
              styles.iconActionBtn,
              { borderColor: colors.border, backgroundColor: colors.surface },
            ]}
            accessibilityRole="button"
            accessibilityLabel="Conversar com a loja"
          >
            <Ionicons
              name="chatbubble-ellipses-outline"
              size={17}
              color={colors.text}
            />
          </Pressable>
        </View>
      </View>
    );
  }

  function renderMapPanel() {
    return (
      <View style={[styles.mapPanel, { backgroundColor: colors.map }]}>
        <LojasMapView
          lojas={lojas}
          localizacaoCliente={localizacaoCliente}
          lojaIdsDestaque={lojaIdsDestaque}
          produtosPorLoja={produtosPorLoja}
          imagemPinPorLoja={imagemPinPorLoja}
          buscaAtiva={Boolean(termoAtivo)}
          onProductPress={abrirProdutoDoMapa}
          onStorePress={abrirCatalogoLoja}
          edgeToEdge
        />

        <View style={styles.weatherFloatingWrap} pointerEvents="box-none">
          <WeatherCard
            status={clima.status}
            dados={clima.data}
            stale={clima.stale}
            style={styles.weatherFloatingCard}
          />
        </View>

        {renderMapFloatingCard()}
      </View>
    );
  }

  function renderSearchHeaderControls() {
    return (
      <View style={styles.controlsSection}>
        {/* Header Location + Title */}
        <View style={styles.headerTop}>
          <View style={{ flex: 1 }}>
            <View style={styles.locationRow}>
              <Ionicons name="navigate" size={13} color={colors.primary} />
              <Text style={[styles.locationText, { color: colors.primary }]}>
                {cidadeHeader}
              </Text>
            </View>
            <Text style={[styles.heroTitle, { color: colors.text }]}>
              O que você procura hoje?
            </Text>
          </View>
        </View>

        {/* Search Input Box */}
        <View
          style={[
            styles.searchBox,
            { backgroundColor: colors.card, borderColor: colors.border },
          ]}
        >
          <Ionicons name="search-outline" size={20} color={colors.textMuted} />
          <TextInput
            value={termoBusca}
            onChangeText={setTermoBusca}
            placeholder="Buscar produtos, marcas ou lojas"
            placeholderTextColor={colors.placeholder}
            onSubmitEditing={handleSearch}
            returnKeyType="search"
            style={[styles.searchInput, { color: colors.text }]}
          />
          {termoBusca.length > 0 ? (
            <Pressable
              onPress={() => setTermoBusca('')}
              style={styles.clearBtn}
              accessibilityRole="button"
              accessibilityLabel="Limpar busca"
            >
              <Ionicons name="close-circle" size={18} color={colors.textMuted} />
            </Pressable>
          ) : null}
          <Pressable
            onPress={() => setMostrarFiltros((v) => !v)}
            style={[
              styles.filterBtn,
              {
                backgroundColor:
                  mostrarFiltros || apenasPromocao ? colors.primary : colors.ink,
              },
            ]}
            accessibilityRole="button"
            accessibilityLabel="Opções de ordenação e filtros"
          >
            <Ionicons
              name="options-outline"
              size={17}
              color={colors.inkForeground}
            />
          </Pressable>
        </View>

        {/* Collapsible Filter Panel */}
        {mostrarFiltros ? (
          <View
            style={[
              styles.filterDrawer,
              { backgroundColor: colors.card, borderColor: colors.border },
            ]}
          >
            <Pressable
              onPress={() => setOrdenarPor('preco')}
              style={[
                styles.filterOptionPill,
                ordenarPor === 'preco' && {
                  backgroundColor: colors.primarySoft,
                  borderColor: colors.primary,
                },
              ]}
            >
              <Text
                style={[
                  styles.filterOptionText,
                  {
                    color:
                      ordenarPor === 'preco'
                        ? colors.accentForeground
                        : colors.textMuted,
                  },
                ]}
              >
                Menor preço
              </Text>
            </Pressable>

            <Pressable
              onPress={() => setOrdenarPor('distancia')}
              style={[
                styles.filterOptionPill,
                ordenarPor === 'distancia' && {
                  backgroundColor: colors.primarySoft,
                  borderColor: colors.primary,
                },
              ]}
            >
              <Text
                style={[
                  styles.filterOptionText,
                  {
                    color:
                      ordenarPor === 'distancia'
                        ? colors.accentForeground
                        : colors.textMuted,
                  },
                ]}
              >
                Mais próximo
              </Text>
            </Pressable>

            <Pressable
              onPress={() => setApenasPromocao((v) => !v)}
              style={[
                styles.filterOptionPill,
                apenasPromocao && {
                  backgroundColor: colors.highlight,
                  borderColor: colors.highlight,
                },
              ]}
            >
              <Text
                style={[
                  styles.filterOptionText,
                  {
                    color: apenasPromocao
                      ? colors.highlightForeground
                      : colors.textMuted,
                  },
                ]}
              >
                Só promoções
              </Text>
            </Pressable>
          </View>
        ) : null}

        {/* Suggestion Chips */}
        <ScrollView
          horizontal
          showsHorizontalScrollIndicator={false}
          contentContainerStyle={styles.chipsRow}
        >
          {chipsSugestao.map((chip) => {
            const ativo =
              termoBusca.trim().toLowerCase() === chip.toLowerCase();
            return (
              <Pressable
                key={chip}
                onPress={() => handleChipPress(chip)}
                style={[
                  styles.chip,
                  {
                    backgroundColor: ativo ? colors.primarySoft : colors.card,
                    borderColor: ativo ? colors.primary : colors.border,
                  },
                ]}
              >
                <Text
                  style={[
                    styles.chipText,
                    {
                      color: ativo
                        ? colors.accentForeground
                        : colors.textMuted,
                    },
                  ]}
                >
                  {chip}
                </Text>
              </Pressable>
            );
          })}
        </ScrollView>

        {/* Mobile List / Map Switcher */}
        {!isDesktopWeb ? (
          <View style={styles.mobileModeRow}>
            <SegmentedControl
              options={[
                { value: MODO_LISTA, label: 'Ofertas' },
                { value: MODO_MAPA, label: 'Mapa' },
              ]}
              value={modoVisualizacao}
              onChange={setModoVisualizacao}
            />
          </View>
        ) : null}

        {/* AI Savings Banner */}
        {economiaInfo && economiaInfo.economia > 0 ? (
          <AiBannerCard
            onPress={() => {
              if (economiaInfo.melhorItem?.id) {
                setSelectedId(economiaInfo.melhorItem.id);
              }
            }}
          >
            <Text style={[styles.aiBannerText, { color: colors.inkForeground }]}>
              <Text style={styles.aiBannerBold}>
                Economize {formatarPrecoBrl(economiaInfo.economia)}
              </Text>{' '}
              comprando em {economiaInfo.melhorLojaNome}
              {economiaInfo.melhorCaminhada
                ? ` — a ${economiaInfo.melhorCaminhada} de você.`
                : '.'}
            </Text>
          </AiBannerCard>
        ) : null}

        {/* Count + Sort Header */}
        <View style={styles.resultsBar}>
          <Text style={[styles.resultsCount, { color: colors.text }]}>
            {produtosEnriquecidos.length}{' '}
            {produtosEnriquecidos.length === 1
              ? 'loja/oferta encontrada'
              : 'ofertas encontradas'}
          </Text>
          <Pressable onPress={alternarOrdenacao} hitSlop={8}>
            <Text style={[styles.sortTrigger, { color: colors.primary }]}>
              {ordenarPor === 'preco' ? 'Menor preço ▾' : 'Mais perto ▾'}
            </Text>
          </Pressable>
        </View>

        {buscando ? (
          <ActivityIndicator
            size="small"
            color={colors.primary}
            style={{ marginVertical: 4 }}
          />
        ) : null}
      </View>
    );
  }

  function renderLeftListColumn() {
    return (
      <View
        style={[
          styles.leftColumn,
          isDesktopWeb && [
            styles.leftColumnDesktop,
            { borderRightColor: colors.border },
          ],
        ]}
      >
        <FlatList
          data={produtosEnriquecidos}
          keyExtractor={(item) => String(item.id)}
          renderItem={renderResultCard}
          ListHeaderComponent={renderSearchHeaderControls()}
          contentContainerStyle={styles.listContent}
          showsVerticalScrollIndicator={false}
          keyboardShouldPersistTaps="handled"
          onEndReached={carregarMais}
          onEndReachedThreshold={0.4}
          ListFooterComponent={
            carregandoMais ? (
              <ActivityIndicator
                size="small"
                color={colors.primary}
                style={{ marginVertical: 14 }}
              />
            ) : null
          }
          ListEmptyComponent={
            loading ? (
              <ActivityIndicator
                size="large"
                color={colors.primary}
                style={{ marginTop: 32 }}
              />
            ) : (
              <ListCardText
                style={[styles.emptyText, { color: colors.textMuted }]}
              >
                {termoAtivo
                  ? 'Nenhuma oferta encontrada para essa busca.'
                  : 'Nenhum produto cadastrado ainda.'}
              </ListCardText>
            )
          }
        />
      </View>
    );
  }

  return (
    <FormScreen
      title="Buscar produtos"
      subtitle="Encontre as melhores ofertas"
      scrollable={false}
      fullBleed
      hideHeader
      noBodyPadding
      error={feedError}
    >
      {isDesktopWeb ? (
        <View style={styles.splitContainer}>
          {renderLeftListColumn()}
          {renderMapPanel()}
        </View>
      ) : modoVisualizacao === MODO_MAPA ? (
        <View style={styles.mobileSplitWrap}>
          <View style={styles.mobileTopControls}>{renderSearchHeaderControls()}</View>
          {renderMapPanel()}
        </View>
      ) : (
        renderLeftListColumn()
      )}
    </FormScreen>
  );
}

const styles = StyleSheet.create({
  splitContainer: {
    flex: 1,
    flexDirection: 'row',
  },
  leftColumn: {
    flex: 1,
  },
  leftColumnDesktop: {
    width: 440,
    maxWidth: 440,
    flexGrow: 0,
    flexShrink: 0,
    borderRightWidth: 1,
  },
  listContent: {
    padding: 20,
    paddingBottom: 110,
    gap: 12,
  },
  controlsSection: {
    gap: 16,
    marginBottom: 8,
  },
  headerTop: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
  },
  locationRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 5,
  },
  locationText: {
    fontFamily: fonts.sansSemiBold,
    fontSize: 12,
    fontWeight: '600',
  },
  heroTitle: {
    fontFamily: fonts.display,
    fontSize: 24,
    fontWeight: '700',
    marginTop: 4,
    letterSpacing: -0.4,
  },
  searchBox: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 10,
    borderWidth: 1,
    borderRadius: radii.xl,
    paddingLeft: 14,
    paddingRight: 8,
    paddingVertical: 7,
    ...shadows.soft,
  },
  searchInput: {
    flex: 1,
    fontFamily: fonts.sans,
    fontSize: 15,
    paddingVertical: 6,
    minWidth: 0,
  },
  clearBtn: {
    padding: 4,
  },
  filterBtn: {
    width: 36,
    height: 36,
    borderRadius: radii.md,
    alignItems: 'center',
    justifyContent: 'center',
  },
  filterDrawer: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: 8,
    padding: 10,
    borderRadius: radii.lg,
    borderWidth: 1,
  },
  filterOptionPill: {
    paddingHorizontal: 12,
    paddingVertical: 6,
    borderRadius: radii.full,
    borderWidth: 1,
    borderColor: 'transparent',
  },
  filterOptionText: {
    fontFamily: fonts.sansSemiBold,
    fontSize: 12,
    fontWeight: '600',
  },
  chipsRow: {
    gap: 8,
    paddingVertical: 2,
  },
  chip: {
    paddingHorizontal: 14,
    paddingVertical: 7,
    borderRadius: radii.full,
    borderWidth: 1,
  },
  chipText: {
    fontFamily: fonts.sansSemiBold,
    fontSize: 12,
    fontWeight: '600',
  },
  mobileModeRow: {
    alignItems: 'flex-start',
  },
  aiBannerText: {
    fontFamily: fonts.sans,
    fontSize: 13.5,
    lineHeight: 19,
  },
  aiBannerBold: {
    fontFamily: fonts.sansBold,
    fontWeight: '700',
  },
  resultsBar: {
    flexDirection: 'row',
    alignItems: 'baseline',
    justifyContent: 'space-between',
    marginTop: 2,
  },
  resultsCount: {
    fontFamily: fonts.display,
    fontSize: 16,
    fontWeight: '600',
  },
  sortTrigger: {
    fontFamily: fonts.sansSemiBold,
    fontSize: 12,
    fontWeight: '600',
  },
  resultCard: {
    borderWidth: 1,
    borderRadius: radii.xl,
    padding: 15,
    ...shadows.soft,
  },
  resultCardSelected: {
    borderWidth: 1.5,
  },
  resultRow: {
    flexDirection: 'row',
    alignItems: 'flex-start',
    gap: 12,
  },
  storeIconWrap: {
    width: 48,
    height: 48,
    borderRadius: radii.md,
    alignItems: 'center',
    justifyContent: 'center',
    overflow: 'hidden',
  },
  productThumb: {
    width: '100%',
    height: '100%',
  },
  resultMain: {
    flex: 1,
    minWidth: 0,
  },
  storeTitleRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 8,
    flexWrap: 'wrap',
  },
  storeName: {
    fontFamily: fonts.sansSemiBold,
    fontSize: 15,
    fontWeight: '600',
    flexShrink: 1,
  },
  productName: {
    fontFamily: fonts.sans,
    fontSize: 13,
    marginTop: 2,
  },
  metaRow: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    alignItems: 'center',
    gap: 10,
    marginTop: 6,
  },
  metaItem: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 3,
  },
  metaText: {
    fontFamily: fonts.sansMedium,
    fontSize: 12,
  },
  verDetalhesLink: {
    fontFamily: fonts.sansSemiBold,
    fontSize: 12,
    fontWeight: '600',
  },
  priceColumn: {
    alignItems: 'flex-end',
    justifyContent: 'space-between',
  },
  priceValue: {
    fontFamily: fonts.display,
    fontSize: 17,
    fontWeight: '700',
  },
  oldPriceValue: {
    fontFamily: fonts.sans,
    fontSize: 11.5,
    textDecorationLine: 'line-through',
    marginTop: 2,
  },
  cardFavBtn: {
    marginTop: 8,
    padding: 2,
  },
  mapPanel: {
    flex: 1,
    position: 'relative',
  },
  weatherFloatingWrap: {
    position: 'absolute',
    top: 18,
    right: 18,
    zIndex: 10,
    maxWidth: 260,
  },
  weatherFloatingCard: {
    borderRadius: radii.xl,
    ...shadows.soft,
  },
  floatingStoreCard: {
    position: 'absolute',
    left: 20,
    right: 20,
    bottom: 22,
    maxWidth: 440,
    alignSelf: 'center',
    width: '90%',
    borderRadius: radii['2xl'],
    borderWidth: 1,
    padding: 18,
    zIndex: 10,
    ...shadows.float,
  },
  floatingHeader: {
    flexDirection: 'row',
    alignItems: 'flex-start',
    justifyContent: 'space-between',
    gap: 12,
  },
  floatingEyebrow: {
    fontFamily: fonts.sansSemiBold,
    fontSize: 12,
    fontWeight: '600',
  },
  floatingStoreTitle: {
    fontFamily: fonts.display,
    fontSize: 18,
    fontWeight: '700',
    marginTop: 3,
  },
  floatingProductSub: {
    fontFamily: fonts.sans,
    fontSize: 13,
    marginTop: 2,
  },
  floatingPrice: {
    fontFamily: fonts.display,
    fontSize: 22,
    fontWeight: '700',
  },
  floatingActions: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 8,
    marginTop: 14,
  },
  routeBtn: {
    flex: 1,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: 7,
    paddingVertical: 11,
    paddingHorizontal: 14,
    borderRadius: radii.md,
  },
  routeBtnText: {
    fontFamily: fonts.sansSemiBold,
    fontSize: 13.5,
    fontWeight: '600',
  },
  iconActionBtn: {
    width: 44,
    height: 44,
    borderRadius: radii.md,
    borderWidth: 1,
    alignItems: 'center',
    justifyContent: 'center',
  },
  mobileSplitWrap: {
    flex: 1,
  },
  mobileTopControls: {
    paddingHorizontal: 16,
    paddingTop: 14,
    maxHeight: 280,
  },
  emptyText: {
    textAlign: 'center',
    marginTop: 28,
  },
});
