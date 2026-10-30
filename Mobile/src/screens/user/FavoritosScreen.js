import {
  FlatList,
  View,
  Text,
  Image,
  Pressable,
  Alert,
  ActivityIndicator,
  StyleSheet,
} from 'react-native';
import { useCallback, useMemo, useState } from 'react';
import { useFocusEffect, useNavigation } from '@react-navigation/native';
import { Ionicons } from '@expo/vector-icons';
import {
  listarFavoritosCliente,
  removerFavorito,
  removerFavoritoLoja,
  removerFavoritoProduto,
} from '../../services/favoritoService';
import { listarLojasParaFeedMapa } from '../../services/lojaService';
import { useAuth } from '../../context/AuthContext';
import { useTheme } from '../../context/ThemeContext';
import { useLayoutProfile } from '../../hooks/useLayoutProfile';
import { FormScreen, ListCardText } from '../../components/form';
import {
  PageHeader,
  PillBadge,
  SegmentedControl,
} from '../../components/ui/DesignPrimitives';
import {
  calcularDescontoPercentual,
  contarFavoritosEmPromocao,
} from '../../utils/insights';
import { calcularDistanciaKm, formatarDistancia } from '../../utils/geo';
import { COORDENADAS_SAQUAREMA, formatarPrecoBrl } from '../../utils/mapaUtils';
import { fonts, radii, shadows } from '../../theme';

const PAGE_SIZE = 40;

export default function FavoritosScreen() {
  const navigation = useNavigation();
  const [aba, setAba] = useState('produtos');
  const [itens, setItens] = useState([]);
  const [lojasMapa, setLojasMapa] = useState([]);
  const [loading, setLoading] = useState(true);
  const [carregandoMais, setCarregandoMais] = useState(false);
  const [page, setPage] = useState(1);
  const [hasNext, setHasNext] = useState(false);
  const { session } = useAuth();
  const { colors } = useTheme();
  const { width, useSidebarNav, sidebarWidth } = useLayoutProfile();
  const clienteId = session?.perfil?.id;

  const contentWidth = useSidebarNav ? width - sidebarWidth : width;
  const numColumns = contentWidth >= 1100 ? 3 : contentWidth >= 680 ? 2 : 1;

  const coordsCliente = useMemo(
    () => ({
      latitude:
        Number(session?.perfil?.latitudeAtual) || COORDENADAS_SAQUAREMA.latitude,
      longitude:
        Number(session?.perfil?.longitudeAtual) ||
        COORDENADAS_SAQUAREMA.longitude,
    }),
    [session?.perfil?.latitudeAtual, session?.perfil?.longitudeAtual]
  );

  const carregar = useCallback(
    async (pagina = 1, append = false) => {
      if (!clienteId) {
        setLoading(false);
        return;
      }
      if (pagina === 1 && !append) setLoading(true);
      else setCarregandoMais(true);

      try {
        const [res, listaLojas] = await Promise.all([
          listarFavoritosCliente(clienteId, pagina, PAGE_SIZE),
          pagina === 1
            ? listarLojasParaFeedMapa().catch(() => [])
            : Promise.resolve(null),
        ]);
        setPage(res.page);
        setHasNext(res.hasNext);
        setItens((prev) => (append ? [...prev, ...res.items] : res.items));
        if (Array.isArray(listaLojas)) {
          setLojasMapa(listaLojas);
        }
      } catch {
        Alert.alert('Erro', 'Não foi possível carregar favoritos.');
      } finally {
        setLoading(false);
        setCarregandoMais(false);
      }
    },
    [clienteId]
  );

  useFocusEffect(
    useCallback(() => {
      carregar(1, false);
    }, [carregar])
  );

  const mapaLojas = useMemo(() => {
    const map = new Map();
    for (const l of lojasMapa) {
      const id = String(l.id ?? l.Id ?? '');
      if (id) map.set(id, l);
    }
    return map;
  }, [lojasMapa]);

  const favoritosProdutos = useMemo(
    () => itens.filter((i) => Boolean(i.produtoId)),
    [itens]
  );

  const favoritosLojas = useMemo(() => {
    const diretos = itens.filter((i) => !i.produtoId && Boolean(i.lojaId));
    if (diretos.length > 0) return diretos;
    // Se o cliente favoritou produtos de lojas, exibe também as lojas associadas
    const vistos = new Set();
    const derivados = [];
    for (const item of itens) {
      if (!item.lojaId) continue;
      const key = String(item.lojaId);
      if (vistos.has(key)) continue;
      vistos.add(key);
      derivados.push({
        id: `loja-${key}`,
        lojaId: item.lojaId,
        nomeLoja: item.nomeLoja,
        derivadoDeProduto: true,
      });
    }
    return derivados;
  }, [itens]);

  const totalEmPromocao = useMemo(
    () => contarFavoritosEmPromocao(favoritosProdutos),
    [favoritosProdutos]
  );

  function obterDistanciaLoja(lojaId) {
    if (!lojaId) return '';
    const loja = mapaLojas.get(String(lojaId));
    const lat = loja?.endereco?.latitude ?? loja?.latitude;
    const lng = loja?.endereco?.longitude ?? loja?.longitude;
    const km = calcularDistanciaKm(
      coordsCliente.latitude,
      coordsCliente.longitude,
      lat,
      lng
    );
    return formatarDistancia(km);
  }

  function carregarMais() {
    if (!hasNext || carregandoMais || loading) return;
    carregar(page + 1, true);
  }

  function abrirProdutoFavorito(item) {
    const id = item?.produto?.id ?? item?.produtoId ?? item?.ProdutoId;
    if (!id) return;
    navigation.navigate({
      name: 'ProductDetail',
      params: { productId: String(id) },
      merge: true,
    });
  }

  function abrirLojaFavorita(item) {
    const lojaId = item?.lojaId;
    if (!lojaId) return;
    navigation.navigate({
      name: 'StoreCatalog',
      params: {
        lojaId: String(lojaId),
        lojaNome: item.nomeLoja || 'Loja',
      },
      merge: true,
    });
  }

  async function handleRemoverFavorito(item) {
    if (!clienteId || !item) return;
    const backup = [...itens];
    setItens((prev) => prev.filter((x) => String(x.id) !== String(item.id)));

    try {
      if (item.produtoId) {
        await removerFavoritoProduto(clienteId, item.produtoId);
      } else if (item.lojaId && !item.derivadoDeProduto) {
        await removerFavoritoLoja(clienteId, item.lojaId);
      } else if (item.id && !String(item.id).startsWith('loja-')) {
        await removerFavorito(item.id);
      }
    } catch {
      setItens(backup);
      Alert.alert('Favoritos', 'Não foi possível remover este item dos favoritos.');
    }
  }

  function renderProdutoCard({ item }) {
    const preco = item.precoExibicao ?? item.precoBase ?? item.produto?.preco ?? 0;
    const precoAnt = item.precoAnterior ?? item.oferta?.precoAnterior ?? null;
    const drop = calcularDescontoPercentual(preco, precoAnt);
    const dist = obterDistanciaLoja(item.lojaId);
    const lojaTrilha = [item.nomeLoja || 'Loja parceira', dist]
      .filter(Boolean)
      .join(' · ');

    return (
      <Pressable
        onPress={() => abrirProdutoFavorito(item)}
        style={[
          styles.favCard,
          { backgroundColor: colors.card, borderColor: colors.border },
        ]}
      >
        <View style={styles.cardTopRow}>
          <View
            style={[
              styles.iconBox,
              { backgroundColor: colors.surfaceSubtle },
            ]}
          >
            {item.imagemUrl ? (
              <Image source={{ uri: item.imagemUrl }} style={styles.thumbImg} />
            ) : (
              <Ionicons name="cube-outline" size={20} color={colors.primary} />
            )}
          </View>
          <Pressable
            onPress={(e) => {
              e?.stopPropagation?.();
              handleRemoverFavorito(item);
            }}
            hitSlop={8}
            accessibilityRole="button"
            accessibilityLabel={`Remover ${item.nomeProduto || 'produto'} dos favoritos`}
          >
            <Ionicons name="heart" size={20} color={colors.danger} />
          </Pressable>
        </View>

        <Text
          style={[styles.cardTitle, { color: colors.text }]}
          numberOfLines={2}
        >
          {item.nomeProduto || item.produto?.nome || 'Produto'}
        </Text>

        <View style={styles.storeMetaRow}>
          <Ionicons
            name="storefront-outline"
            size={12}
            color={colors.textMuted}
          />
          <Text
            style={[styles.storeMetaText, { color: colors.textMuted }]}
            numberOfLines={1}
          >
            {lojaTrilha}
          </Text>
        </View>

        <View style={styles.cardBottomRow}>
          <View>
            <Text style={[styles.priceText, { color: colors.text }]}>
              {formatarPrecoBrl(preco)}
            </Text>
            {precoAnt && Number(precoAnt) > Number(preco) ? (
              <Text style={[styles.oldPriceText, { color: colors.textMuted }]}>
                {formatarPrecoBrl(precoAnt)}
              </Text>
            ) : null}
          </View>
          {drop > 0 ? (
            <PillBadge label={`−${drop}%`} variant="highlight" />
          ) : item.emPromocao ? (
            <PillBadge label="PROMO" variant="highlight" />
          ) : null}
        </View>
      </Pressable>
    );
  }

  function renderLojaCard({ item }) {
    const lojaInfo = mapaLojas.get(String(item.lojaId));
    const dist = obterDistanciaLoja(item.lojaId);
    const endereco = [
      lojaInfo?.endereco?.bairro,
      lojaInfo?.endereco?.cidade,
    ]
      .filter(Boolean)
      .join(', ');

    return (
      <Pressable
        onPress={() => abrirLojaFavorita(item)}
        style={[
          styles.favCard,
          { backgroundColor: colors.card, borderColor: colors.border },
        ]}
      >
        <View style={styles.cardTopRow}>
          <View
            style={[
              styles.iconBox,
              { backgroundColor: colors.primarySoft },
            ]}
          >
            <Ionicons
              name="storefront-outline"
              size={20}
              color={colors.primary}
            />
          </View>
          {!item.derivadoDeProduto ? (
            <Pressable
              onPress={(e) => {
                e?.stopPropagation?.();
                handleRemoverFavorito(item);
              }}
              hitSlop={8}
              accessibilityRole="button"
              accessibilityLabel={`Remover ${item.nomeLoja || 'loja'} dos favoritos`}
            >
              <Ionicons name="heart" size={20} color={colors.danger} />
            </Pressable>
          ) : (
            <PillBadge label="Catálogo" variant="primarySoft" />
          )}
        </View>

        <Text
          style={[styles.cardTitle, { color: colors.text }]}
          numberOfLines={1}
        >
          {item.nomeLoja || lojaInfo?.nomeFantasia || 'Loja parceira'}
        </Text>

        <View style={styles.storeMetaRow}>
          <Ionicons
            name="location-outline"
            size={12}
            color={colors.textMuted}
          />
          <Text
            style={[styles.storeMetaText, { color: colors.textMuted }]}
            numberOfLines={1}
          >
            {[endereco, dist].filter(Boolean).join(' · ') || 'Ver catálogo da loja'}
          </Text>
        </View>

        <View style={styles.cardBottomRow}>
          <Text style={[styles.verCatalogoText, { color: colors.primary }]}>
            Ver produtos da loja →
          </Text>
        </View>
      </Pressable>
    );
  }

  const dadosLista = aba === 'produtos' ? favoritosProdutos : favoritosLojas;

  return (
    <FormScreen
      title="Favoritos"
      hideHeader
      scrollable={false}
      maxContentWidth={1180}
    >
      <PageHeader
        eyebrow="Sua lista"
        title="Favoritos"
        action={
          <SegmentedControl
            options={[
              { value: 'produtos', label: 'Produtos' },
              { value: 'lojas', label: 'Lojas' },
            ]}
            value={aba}
            onChange={setAba}
          />
        }
      />

      {totalEmPromocao > 0 ? (
        <View
          style={[
            styles.promoBanner,
            { backgroundColor: colors.primarySoft },
          ]}
        >
          <Ionicons
            name="pricetag-outline"
            size={18}
            color={colors.primary}
          />
          <Text
            style={[
              styles.promoBannerText,
              { color: colors.accentForeground },
            ]}
          >
            {totalEmPromocao === 1
              ? '1 produto favorito está em promoção agora nas lojas parceiras.'
              : `${totalEmPromocao} favoritos estão em promoção agora nas lojas parceiras.`}
          </Text>
        </View>
      ) : null}

      {loading ? (
        <ActivityIndicator
          size="large"
          color={colors.primary}
          style={{ marginTop: 32 }}
        />
      ) : (
        <FlatList
          key={`fav-${aba}-cols-${numColumns}`}
          data={dadosLista}
          keyExtractor={(item) => String(item.id)}
          numColumns={numColumns}
          columnWrapperStyle={numColumns > 1 ? styles.gridRow : undefined}
          renderItem={aba === 'produtos' ? renderProdutoCard : renderLojaCard}
          contentContainerStyle={styles.list}
          showsVerticalScrollIndicator={false}
          onEndReached={carregarMais}
          onEndReachedThreshold={0.4}
          ListFooterComponent={
            carregandoMais ? (
              <ActivityIndicator
                size="small"
                color={colors.primary}
                style={{ marginVertical: 12 }}
              />
            ) : null
          }
          ListEmptyComponent={
            <ListCardText
              style={[styles.empty, { color: colors.textMuted }]}
            >
              {aba === 'produtos'
                ? 'Você ainda não salvou produtos nos favoritos.'
                : 'Nenhuma loja favorita salva ainda.'}
            </ListCardText>
          }
        />
      )}
    </FormScreen>
  );
}

const styles = StyleSheet.create({
  promoBanner: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 10,
    borderRadius: radii.xl,
    paddingHorizontal: 16,
    paddingVertical: 13,
    marginBottom: 16,
  },
  promoBannerText: {
    flex: 1,
    fontFamily: fonts.sansMedium,
    fontSize: 13.5,
    lineHeight: 19,
  },
  list: {
    paddingBottom: 100,
    gap: 14,
  },
  gridRow: {
    gap: 14,
  },
  favCard: {
    flex: 1,
    borderWidth: 1,
    borderRadius: radii.xl,
    padding: 18,
    minWidth: 240,
    ...shadows.soft,
  },
  cardTopRow: {
    flexDirection: 'row',
    alignItems: 'flex-start',
    justifyContent: 'space-between',
  },
  iconBox: {
    width: 48,
    height: 48,
    borderRadius: radii.md,
    alignItems: 'center',
    justifyContent: 'center',
    overflow: 'hidden',
  },
  thumbImg: {
    width: '100%',
    height: '100%',
  },
  cardTitle: {
    fontFamily: fonts.sansSemiBold,
    fontSize: 15,
    fontWeight: '600',
    marginTop: 14,
  },
  storeMetaRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 5,
    marginTop: 4,
  },
  storeMetaText: {
    fontFamily: fonts.sans,
    fontSize: 12,
    flex: 1,
  },
  cardBottomRow: {
    flexDirection: 'row',
    alignItems: 'flex-end',
    justifyContent: 'space-between',
    marginTop: 16,
  },
  priceText: {
    fontFamily: fonts.display,
    fontSize: 20,
    fontWeight: '700',
  },
  oldPriceText: {
    fontFamily: fonts.sans,
    fontSize: 11.5,
    textDecorationLine: 'line-through',
    marginTop: 2,
  },
  verCatalogoText: {
    fontFamily: fonts.sansSemiBold,
    fontSize: 13,
    fontWeight: '600',
  },
  empty: {
    textAlign: 'center',
    marginTop: 32,
  },
});
