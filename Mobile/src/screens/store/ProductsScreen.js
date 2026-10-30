import {
  ScrollView,
  View,
  Text,
  TextInput,
  Pressable,
  ActivityIndicator,
  StyleSheet,
} from 'react-native';
import { useCallback, useState, useMemo } from 'react';
import { useFocusEffect } from '@react-navigation/native';
import { Ionicons } from '@expo/vector-icons';
import { listarProdutos } from '../../services/productService';
import { listarOfertas } from '../../services/ofertaService';
import { obterLoja } from '../../services/lojaService';
import { useAuth } from '../../context/AuthContext';
import { podeCriarLoja } from '../../utils/modoUsuario';
import { useTheme } from '../../context/ThemeContext';
import { useLayoutProfile } from '../../hooks/useLayoutProfile';
import { mapApiError } from '../../utils/apiErrorUtils';
import {
  FormScreen,
  PrimaryButton,
  ListCardText,
} from '../../components/form';
import {
  PageHeader,
  PillBadge,
  SurfaceCard,
} from '../../components/ui/DesignPrimitives';
import {
  filtrarProdutosPorTermo,
  nomeProduto,
  produtoPertenceALoja,
} from '../../utils/produtoUtils';
import { labelCategoria } from '../../utils/categoriasProduto';
import { formatarPrecoBrl } from '../../utils/mapaUtils';
import { calcularIndicadoresProdutosLoja } from '../../utils/insights';
import { fonts, radii } from '../../theme';

export default function ProductsScreen({ navigation }) {
  const { session } = useAuth();
  const { colors } = useTheme();
  const { useSidebarNav } = useLayoutProfile();
  const lojaId = session?.perfil?.lojaId;

  const [nomeLoja, setNomeLoja] = useState(
    session?.perfil?.nomeFantasia || 'Catálogo da loja'
  );
  const [termoBusca, setTermoBusca] = useState('');
  const [produtos, setProdutos] = useState([]);
  const [ofertasLoja, setOfertasLoja] = useState([]);
  const [todasOfertas, setTodasOfertas] = useState([]);
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState(null);

  const carregarProdutos = useCallback(async () => {
    if (!lojaId) {
      setProdutos([]);
      setLoading(false);
      return;
    }

    setLoading(true);
    try {
      const [dadosRes, ofertasRes, lojaInfo] = await Promise.all([
        listarProdutos(lojaId, 1, 150),
        listarOfertas(1, 250).catch(() => ({ items: [] })),
        obterLoja(lojaId).catch(() => null),
      ]);

      if (lojaInfo?.nomeFantasia) {
        setNomeLoja(lojaInfo.nomeFantasia);
      }

      const listaProdutos = Array.isArray(dadosRes?.items)
        ? dadosRes.items
        : [];
      const meusProdutos = listaProdutos.filter((p) =>
        produtoPertenceALoja(p, lojaId)
      );
      const listaOfertas = Array.isArray(ofertasRes?.items)
        ? ofertasRes.items
        : [];
      const ofertasDaLoja = listaOfertas.filter(
        (o) => String(o.lojaId) === String(lojaId)
      );

      setProdutos(meusProdutos);
      setOfertasLoja(ofertasDaLoja);
      setTodasOfertas(listaOfertas);
      setLoadError(null);
    } catch (error) {
      console.error(error?.response?.data || error.message);
      setLoadError(mapApiError(error));
    } finally {
      setLoading(false);
    }
  }, [lojaId]);

  useFocusEffect(
    useCallback(() => {
      carregarProdutos();
    }, [carregarProdutos])
  );

  const indicadores = useMemo(
    () =>
      calcularIndicadoresProdutosLoja(produtos, ofertasLoja, todasOfertas),
    [produtos, ofertasLoja, todasOfertas]
  );

  const produtosExibidos = useMemo(
    () => filtrarProdutosPorTermo(indicadores.produtosEnriquecidos, termoBusca),
    [indicadores.produtosEnriquecidos, termoBusca]
  );

  function abrirDetalhesOuEditar(item) {
    const id = item?.id ?? item?.Id;
    if (!id) return;
    navigation.navigate({
      name: 'ProductDetail',
      params: { productId: String(id) },
      merge: true,
    });
  }

  if (!lojaId) {
    const podeCriar = podeCriarLoja(session);
    return (
      <FormScreen
        title="Meus produtos"
        subtitle={
          podeCriar ? 'Vincule uma loja ao perfil' : 'Loja vinculada ao perfil'
        }
        scrollable={false}
        footer={
          podeCriar ? (
            <PrimaryButton
              label="Criar loja"
              onPress={() => navigation.navigate('CreateStore')}
            />
          ) : undefined
        }
      >
        <ListCardText>
          {podeCriar
            ? 'Cadastre sua loja para gerenciar produtos.'
            : 'Sua conta já está vinculada a uma loja. Atualize a tela ou entre novamente se os produtos não aparecerem.'}
        </ListCardText>
      </FormScreen>
    );
  }

  return (
    <FormScreen
      title="Meus produtos"
      hideHeader
      scrollable={false}
      maxContentWidth={1220}
      error={loadError}
    >
      <ScrollView
        showsVerticalScrollIndicator={false}
        contentContainerStyle={styles.scrollContent}
      >
        <PageHeader
          eyebrow={nomeLoja}
          title="Meus produtos"
          action={
            <View style={styles.headerActions}>
              <Pressable
                onPress={() => navigation.navigate('CreateOferta')}
                style={[
                  styles.secondaryActionBtn,
                  {
                    backgroundColor: colors.card,
                    borderColor: colors.border,
                  },
                ]}
                accessibilityRole="button"
                accessibilityLabel="Nova oferta"
              >
                <Ionicons
                  name="pricetag-outline"
                  size={15}
                  color={colors.text}
                />
                <Text
                  style={[styles.secondaryActionText, { color: colors.text }]}
                >
                  Nova oferta
                </Text>
              </Pressable>

              <Pressable
                onPress={() => navigation.navigate('CreateProduct')}
                style={[
                  styles.primaryActionBtn,
                  { backgroundColor: colors.primary },
                ]}
                accessibilityRole="button"
                accessibilityLabel="Novo produto"
              >
                <Ionicons
                  name="add"
                  size={17}
                  color={colors.primaryForeground}
                />
                <Text
                  style={[
                    styles.primaryActionText,
                    { color: colors.primaryForeground },
                  ]}
                >
                  Novo produto
                </Text>
              </Pressable>
            </View>
          }
        />

        {/* 4 KPI Cards */}
        <View style={styles.kpiRow}>
          <SurfaceCard style={styles.kpiCard}>
            <Text style={[styles.kpiVal, { color: colors.text }]}>
              {indicadores.produtosAtivos}
            </Text>
            <Text style={[styles.kpiLabel, { color: colors.textMuted }]}>
              produtos ativos
            </Text>
          </SurfaceCard>

          <SurfaceCard style={styles.kpiCard}>
            <Text style={[styles.kpiVal, { color: colors.text }]}>
              {indicadores.estoqueBaixo}
            </Text>
            <Text style={[styles.kpiLabel, { color: colors.textMuted }]}>
              estoque baixo
            </Text>
          </SurfaceCard>

          <SurfaceCard style={styles.kpiCard}>
            <Text style={[styles.kpiVal, { color: colors.text }]}>
              {indicadores.semEstoque}
            </Text>
            <Text style={[styles.kpiLabel, { color: colors.textMuted }]}>
              sem estoque
            </Text>
          </SurfaceCard>

          <SurfaceCard style={styles.kpiCard}>
            <Text style={[styles.kpiVal, { color: colors.text }]}>
              {indicadores.percentualPrimeiroLugar}%
            </Text>
            <Text style={[styles.kpiLabel, { color: colors.textMuted }]}>
              em 1º lugar no preço
            </Text>
          </SurfaceCard>
        </View>

        {/* Search + Table Card */}
        <SurfaceCard noPadding style={styles.tableCard}>
          <View
            style={[
              styles.searchBarRow,
              { borderBottomColor: colors.border },
            ]}
          >
            <Ionicons
              name="search-outline"
              size={18}
              color={colors.textMuted}
            />
            <TextInput
              value={termoBusca}
              onChangeText={setTermoBusca}
              placeholder="Buscar por nome, marca ou código de barras"
              placeholderTextColor={colors.placeholder}
              returnKeyType="search"
              style={[styles.searchInput, { color: colors.text }]}
            />
            {termoBusca.length > 0 ? (
              <Pressable onPress={() => setTermoBusca('')} hitSlop={6}>
                <Ionicons
                  name="close-circle"
                  size={17}
                  color={colors.textMuted}
                />
              </Pressable>
            ) : null}
          </View>

          {loading ? (
            <ActivityIndicator
              size="large"
              color={colors.primary}
              style={{ marginVertical: 32 }}
            />
          ) : produtosExibidos.length === 0 ? (
            <ListCardText
              style={[styles.emptyText, { color: colors.textMuted }]}
            >
              {termoBusca.trim()
                ? 'Nenhum produto encontrado para essa busca.'
                : 'Nenhum produto cadastrado ainda.'}
            </ListCardText>
          ) : useSidebarNav ? (
            <View>
              {/* Table Head */}
              <View
                style={[
                  styles.tHead,
                  {
                    backgroundColor: colors.surfaceSubtle,
                    borderBottomColor: colors.border,
                  },
                ]}
              >
                <Text
                  style={[
                    styles.thText,
                    { color: colors.textMuted, flex: 2.2 },
                  ]}
                >
                  Produto
                </Text>
                <Text
                  style={[
                    styles.thText,
                    { color: colors.textMuted, flex: 1.2 },
                  ]}
                >
                  Categoria
                </Text>
                <Text
                  style={[
                    styles.thText,
                    styles.textRight,
                    { color: colors.textMuted, flex: 1 },
                  ]}
                >
                  Preço
                </Text>
                <Text
                  style={[
                    styles.thText,
                    styles.textRight,
                    { color: colors.textMuted, flex: 1 },
                  ]}
                >
                  Estoque
                </Text>
                <Text
                  style={[
                    styles.thText,
                    styles.textCenter,
                    { color: colors.textMuted, width: 90 },
                  ]}
                >
                  Ranking
                </Text>
                <View style={{ width: 48 }} />
              </View>

              {/* Table Rows */}
              {produtosExibidos.map((p, idx) => {
                const nome = nomeProduto(p);
                const cat =
                  p.categoriaNome || labelCategoria(p.categoria) || 'Geral';
                const esgotado =
                  !p.disponivel ||
                  (p.estoque != null && Number(p.estoque) === 0);
                const baixo =
                  !esgotado &&
                  p.estoque != null &&
                  Number(p.estoque) > 0 &&
                  Number(p.estoque) < 10;

                return (
                  <Pressable
                    key={String(p.id)}
                    onPress={() => abrirDetalhesOuEditar(p)}
                    style={[
                      styles.tRow,
                      idx < produtosExibidos.length - 1 && {
                        borderBottomWidth: 1,
                        borderBottomColor: colors.border,
                      },
                    ]}
                  >
                    <View style={{ flex: 2.2, minWidth: 0 }}>
                      <Text
                        style={[styles.prodTitle, { color: colors.text }]}
                        numberOfLines={1}
                      >
                        {nome}
                      </Text>
                      <Text
                        style={[styles.prodSku, { color: colors.textMuted }]}
                        numberOfLines={1}
                      >
                        {p.codigoBarras || p.marca || 'Sem SKU'}
                      </Text>
                    </View>

                    <Text
                      style={[
                        styles.cellMuted,
                        { color: colors.textMuted, flex: 1.2 },
                      ]}
                      numberOfLines={1}
                    >
                      {cat}
                    </Text>

                    <Text
                      style={[
                        styles.cellPrice,
                        styles.textRight,
                        { color: colors.text, flex: 1 },
                      ]}
                    >
                      {formatarPrecoBrl(p.precoEfetivo)}
                    </Text>

                    <Text
                      style={[
                        styles.cellStock,
                        styles.textRight,
                        {
                          flex: 1,
                          color: esgotado
                            ? colors.danger
                            : baixo
                              ? colors.warning
                              : colors.text,
                        },
                      ]}
                    >
                      {esgotado
                        ? 'Esgotado'
                        : p.estoque != null
                          ? String(p.estoque)
                          : 'Ativo'}
                    </Text>

                    <View style={[styles.rankCol, { width: 90 }]}>
                      <PillBadge
                        label={`${p.posicaoRanking}º`}
                        variant={
                          p.posicaoRanking === 1 ? 'highlight' : 'muted'
                        }
                      />
                    </View>

                    <View style={styles.editCol}>
                      <Pressable
                        onPress={(e) => {
                          e?.stopPropagation?.();
                          abrirDetalhesOuEditar(p);
                        }}
                        style={[
                          styles.editIconBtn,
                          { backgroundColor: colors.surfaceSubtle },
                        ]}
                        accessibilityRole="button"
                        accessibilityLabel={`Editar ${nome}`}
                      >
                        <Ionicons
                          name="pencil-outline"
                          size={15}
                          color={colors.text}
                        />
                      </Pressable>
                    </View>
                  </Pressable>
                );
              })}
            </View>
          ) : (
            <View>
              {produtosExibidos.map((p, idx) => {
                const nome = nomeProduto(p);
                const cat =
                  p.categoriaNome || labelCategoria(p.categoria) || 'Geral';
                const esgotado =
                  !p.disponivel ||
                  (p.estoque != null && Number(p.estoque) === 0);

                return (
                  <Pressable
                    key={String(p.id)}
                    onPress={() => abrirDetalhesOuEditar(p)}
                    style={[
                      styles.mobileRow,
                      idx < produtosExibidos.length - 1 && {
                        borderBottomWidth: 1,
                        borderBottomColor: colors.border,
                      },
                    ]}
                  >
                    <View style={{ flex: 1, minWidth: 0 }}>
                      <View style={styles.mobileTopLine}>
                        <Text
                          style={[styles.prodTitle, { color: colors.text }]}
                          numberOfLines={1}
                        >
                          {nome}
                        </Text>
                        <PillBadge
                          label={`${p.posicaoRanking}º`}
                          variant={
                            p.posicaoRanking === 1 ? 'highlight' : 'muted'
                          }
                        />
                      </View>
                      <Text
                        style={[styles.prodSku, { color: colors.textMuted }]}
                      >
                        {cat} · {p.codigoBarras || p.marca || ''} ·{' '}
                        {esgotado
                          ? 'Esgotado'
                          : p.estoque != null
                            ? `Estoque: ${p.estoque}`
                            : 'Disponível'}
                      </Text>
                    </View>
                    <Text
                      style={[styles.cellPrice, { color: colors.text }]}
                    >
                      {formatarPrecoBrl(p.precoEfetivo)}
                    </Text>
                    <Ionicons
                      name="pencil-outline"
                      size={16}
                      color={colors.textMuted}
                    />
                  </Pressable>
                );
              })}
            </View>
          )}
        </SurfaceCard>
      </ScrollView>
    </FormScreen>
  );
}

const styles = StyleSheet.create({
  scrollContent: {
    paddingBottom: 100,
    gap: 18,
  },
  headerActions: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 10,
  },
  secondaryActionBtn: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 6,
    paddingHorizontal: 14,
    paddingVertical: 10,
    borderRadius: radii.md,
    borderWidth: 1,
  },
  secondaryActionText: {
    fontFamily: fonts.sansSemiBold,
    fontSize: 13,
    fontWeight: '600',
  },
  primaryActionBtn: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 6,
    paddingHorizontal: 15,
    paddingVertical: 10,
    borderRadius: radii.md,
  },
  primaryActionText: {
    fontFamily: fonts.sansSemiBold,
    fontSize: 13,
    fontWeight: '600',
  },
  kpiRow: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: 12,
  },
  kpiCard: {
    flex: 1,
    minWidth: 160,
    padding: 16,
  },
  kpiVal: {
    fontFamily: fonts.display,
    fontSize: 24,
    fontWeight: '700',
  },
  kpiLabel: {
    fontFamily: fonts.sans,
    fontSize: 12,
    marginTop: 2,
  },
  tableCard: {
    overflow: 'hidden',
  },
  searchBarRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 10,
    paddingHorizontal: 16,
    paddingVertical: 12,
    borderBottomWidth: 1,
  },
  searchInput: {
    flex: 1,
    fontFamily: fonts.sans,
    fontSize: 14,
    paddingVertical: 4,
    minWidth: 0,
  },
  tHead: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingHorizontal: 16,
    paddingVertical: 11,
    borderBottomWidth: 1,
    gap: 10,
  },
  thText: {
    fontFamily: fonts.sansBold,
    fontSize: 11,
    fontWeight: '700',
    textTransform: 'uppercase',
    letterSpacing: 0.7,
  },
  tRow: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingHorizontal: 16,
    paddingVertical: 13,
    gap: 10,
  },
  prodTitle: {
    fontFamily: fonts.sansSemiBold,
    fontSize: 14,
    fontWeight: '600',
  },
  prodSku: {
    fontFamily: fonts.sans,
    fontSize: 12,
    marginTop: 2,
  },
  cellMuted: {
    fontFamily: fonts.sans,
    fontSize: 13,
  },
  cellPrice: {
    fontFamily: fonts.display,
    fontSize: 14,
    fontWeight: '700',
  },
  cellStock: {
    fontFamily: fonts.sansSemiBold,
    fontSize: 13,
    fontWeight: '600',
  },
  rankCol: {
    alignItems: 'center',
    justifyContent: 'center',
  },
  editCol: {
    width: 48,
    alignItems: 'flex-end',
  },
  editIconBtn: {
    width: 32,
    height: 32,
    borderRadius: radii.sm,
    alignItems: 'center',
    justifyContent: 'center',
  },
  textRight: {
    textAlign: 'right',
  },
  textCenter: {
    textAlign: 'center',
  },
  mobileRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 12,
    paddingHorizontal: 16,
    paddingVertical: 14,
  },
  mobileTopLine: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 8,
  },
  emptyText: {
    textAlign: 'center',
    marginVertical: 28,
  },
});
