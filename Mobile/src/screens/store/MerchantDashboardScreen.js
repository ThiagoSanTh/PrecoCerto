import {
  View,
  Text,
  ScrollView,
  Pressable,
  ActivityIndicator,
  StyleSheet,
  Platform,
} from 'react-native';
import { useCallback, useState, useMemo } from 'react';
import { useFocusEffect } from '@react-navigation/native';
import { Ionicons } from '@expo/vector-icons';
import { listarProdutos } from '../../services/productService';
import { listarOfertas } from '../../services/ofertaService';
import { obterLoja } from '../../services/lojaService';
import { obterRelatorioPesquisasLoja } from '../../services/historicoService';
import { enviarPerguntaIA } from '../../services/iaService';
import { useAuth } from '../../context/AuthContext';
import { useTheme } from '../../context/ThemeContext';
import { useLayoutProfile } from '../../hooks/useLayoutProfile';
import { podeCriarLoja } from '../../utils/modoUsuario';
import { produtoPertenceALoja } from '../../utils/produtoUtils';
import { formatarPrecoBrl } from '../../utils/mapaUtils';
import {
  calcularIndicadoresProdutosLoja,
  calcularCompetitividadePrecos,
  cruzarTermosMaisProcuradosComCatalogo,
} from '../../utils/insights';
import { FormScreen, PrimaryButton, ListCardText } from '../../components/form';
import { PageHeader, SurfaceCard, PillBadge } from '../../components/ui/DesignPrimitives';
import { fonts, radii } from '../../theme';

export default function MerchantDashboardScreen({ navigation }) {
  const { session, isLojista } = useAuth();
  const { colors } = useTheme();
  const { isDesktopWeb, isTabletWeb, height } = useLayoutProfile();
  const lojaId = session?.perfil?.lojaId || session?.perfil?.lojaVinculadaId || null;
  const listMaxHeight = Math.max(220, Math.min(300, Math.round((height || 760) * 0.36)));

  const [loja, setLoja] = useState(null);
  const [meusProdutos, setMeusProdutos] = useState([]);
  const [todasOfertas, setTodasOfertas] = useState([]);
  const [relatorioTermos, setRelatorioTermos] = useState([]);
  const [loading, setLoading] = useState(true);
  const [iaInsights, setIaInsights] = useState([]);
  const [carregandoIa, setCarregandoIa] = useState(false);

  const carregarDados = useCallback(async () => {
    if (!lojaId) {
      setLoading(false);
      return;
    }
    setLoading(true);
    try {
      const [lojaRes, prodRes, ofertasRes, relRes] = await Promise.all([
        obterLoja(lojaId).catch(() => null),
        listarProdutos(lojaId, 1, 100).catch(() => ({ items: [] })),
        listarOfertas(1, 200).catch(() => ({ items: [] })),
        obterRelatorioPesquisasLoja(lojaId).catch(() => []),
      ]);

      const prods = (prodRes?.items || []).filter((p) => produtoPertenceALoja(p, lojaId));
      setLoja(lojaRes);
      setMeusProdutos(prods);
      setTodasOfertas(ofertasRes?.items || []);
      setRelatorioTermos(Array.isArray(relRes) ? relRes : []);
    } finally {
      setLoading(false);
    }
  }, [lojaId]);

  const consultarInsightsIA = useCallback(async () => {
    if (!lojaId || carregandoIa) return;
    setCarregandoIa(true);
    try {
      const data = await enviarPerguntaIA(
        'Analise meus preços frente à concorrência e indique oportunidades de estoque',
        {
          incluirLocalizacao: false,
          usuarioId: session?.perfil?.id || null,
          lojaId,
          modoOperacional: 'loja',
        }
      );

      const linhas = [];
      if (data?.resposta) {
        const partes = String(data.resposta)
          .split(/\n+/)
          .map((s) => s.replace(/^[-•*]\s*/, '').trim())
          .filter((s) => s.length > 10);
        linhas.push(...partes.slice(0, 3));
      }
      if (Array.isArray(data?.motivos) && linhas.length < 3) {
        data.motivos.forEach((m) => {
          if (m && linhas.length < 3 && !linhas.includes(m)) linhas.push(m);
        });
      }
      setIaInsights(linhas);
    } catch {
      setIaInsights([]);
    } finally {
      setCarregandoIa(false);
    }
  }, [lojaId, session?.perfil?.id, carregandoIa]);

  useFocusEffect(
    useCallback(() => {
      carregarDados();
    }, [carregarDados])
  );

  const indicadores = useMemo(
    () => calcularIndicadoresProdutosLoja(meusProdutos, todasOfertas, lojaId),
    [meusProdutos, todasOfertas, lojaId]
  );

  const competitividade = useMemo(
    () => calcularCompetitividadePrecos(meusProdutos, todasOfertas, lojaId),
    [meusProdutos, todasOfertas, lojaId]
  );

  const termosCruzados = useMemo(
    () => cruzarTermosMaisProcuradosComCatalogo(relatorioTermos, meusProdutos),
    [relatorioTermos, meusProdutos]
  );

  const totalBuscasLoja = useMemo(
    () => relatorioTermos.reduce((acc, r) => acc + Number(r?.quantidade ?? r?.Quantidade ?? 0), 0),
    [relatorioTermos]
  );

  const ofertasDaLojaAtivas = useMemo(
    () =>
      todasOfertas.filter(
        (o) => String(o?.lojaId) === String(lojaId) && o?.disponivel !== false
      ).length,
    [todasOfertas, lojaId]
  );

  const insightsDerivados = useMemo(() => {
    if (iaInsights.length > 0) return iaInsights;
    const list = [];
    const semCadastro = termosCruzados.find((t) => !t.emCatalogo);
    if (semCadastro) {
      list.push(
        `"${semCadastro.termo}" teve ${semCadastro.quantidade} busca(s) na região e ainda não consta no seu catálogo.`
      );
    }
    const maisCompetitivo = competitividade.find((c) => c.diferencaPercentual < -2);
    if (maisCompetitivo) {
      list.push(
        `Seu "${maisCompetitivo.nome}" está ${Math.abs(maisCompetitivo.diferencaPercentual).toFixed(1)}% abaixo da média das lojas.`
      );
    }
    if (indicadores.estoqueBaixo > 0 || indicadores.semEstoque > 0) {
      list.push(
        `Você possui ${indicadores.estoqueBaixo} produto(s) com estoque baixo e ${indicadores.semEstoque} esgotado(s).`
      );
    }
    if (list.length === 0) {
      list.push(
        'Cadastre produtos e ofertas para acompanhar análises de preço e demanda real da sua loja.'
      );
    }
    return list.slice(0, 3);
  }, [iaInsights, termosCruzados, competitividade, indicadores]);

  if (!lojaId) {
    const podeCriar = podeCriarLoja(session);
    return (
      <FormScreen
        title="Inteligência de vendas"
        subtitle="Vincule uma loja para acessar os indicadores"
        scrollable={false}
        footer={
          podeCriar ? (
            <PrimaryButton
              label="Criar loja"
              onPress={() => navigation.navigate('CreateStore')}
            />
          ) : null
        }
      >
        <ListCardText>
          {podeCriar
            ? 'Cadastre sua loja para visualizar a demanda de pesquisas e a competitividade de preços.'
            : 'Sua conta não possui uma loja ativa no momento.'}
        </ListCardText>
      </FormScreen>
    );
  }

  const kpis = [
    {
      label: 'Buscas pela sua loja',
      value: totalBuscasLoja.toLocaleString('pt-BR'),
      badge: `${relatorioTermos.length} termos`,
      icon: 'search-outline',
      positive: true,
    },
    {
      label: 'Produtos ativos',
      value: indicadores.ativos.toLocaleString('pt-BR'),
      badge: `${ofertasDaLojaAtivas} ofertas`,
      icon: 'cube-outline',
      positive: true,
    },
    {
      label: 'Em 1º lugar no preço',
      value: `${indicadores.percentualPrimeiroLugar}%`,
      badge: 'Catálogo',
      icon: 'trending-up-outline',
      positive: indicadores.percentualPrimeiroLugar >= 50,
    },
    {
      label: 'Atenção no estoque',
      value: String(indicadores.estoqueBaixo + indicadores.semEstoque),
      badge: `${indicadores.semEstoque} sem estoque`,
      icon: 'alert-circle-outline',
      positive: indicadores.semEstoque === 0,
    },
  ];

  const isWide = isDesktopWeb || isTabletWeb;

  return (
    <FormScreen
      title="Inteligência de vendas"
      hideHeader
      scrollable
    >
      <View style={styles.pageWrap}>
        <PageHeader
          eyebrow={loja?.nomeFantasia || session?.perfil?.nomeLoja || 'Painel do Lojista'}
          title="Inteligência de vendas"
          action={
            <View style={styles.headerActions}>
              <Pressable
                accessibilityRole="button"
                onPress={() => navigation.navigate('CreateOferta')}
                style={[
                  styles.actionBtnOutline,
                  { backgroundColor: colors.card, borderColor: colors.border },
                ]}
              >
                <Ionicons name="pricetag-outline" size={15} color={colors.text} />
                <Text style={[styles.actionBtnOutlineText, { color: colors.text }]}>
                  Nova oferta
                </Text>
              </Pressable>
              {isLojista ? (
                <Pressable
                  accessibilityRole="button"
                  onPress={() => navigation.navigate('Vendedores', { lojaId })}
                  style={[
                    styles.actionBtnOutline,
                    { backgroundColor: colors.card, borderColor: colors.border },
                  ]}
                >
                  <Ionicons name="people-outline" size={15} color={colors.text} />
                  <Text style={[styles.actionBtnOutlineText, { color: colors.text }]}>
                    Equipe
                  </Text>
                </Pressable>
              ) : null}
            </View>
          }
        />

        {loading ? (
          <ActivityIndicator size="large" color={colors.primary} style={{ marginVertical: 36 }} />
        ) : (
          <>
            <View style={[styles.kpiGrid, isWide && styles.kpiGridWide]}>
              {kpis.map((k) => (
                <SurfaceCard key={k.label} style={[styles.kpiCard, isWide && styles.kpiCardWide]}>
                  <View style={styles.kpiTopRow}>
                    <View
                      style={[
                        styles.kpiIconBox,
                        { backgroundColor: colors.primarySoft },
                      ]}
                    >
                      <Ionicons name={k.icon} size={17} color={colors.primary} />
                    </View>
                    <PillBadge
                      label={k.badge}
                      variant={k.positive ? 'soft' : 'destructive'}
                    />
                  </View>
                  <Text style={[styles.kpiValue, { color: colors.text }]}>{k.value}</Text>
                  <Text style={[styles.kpiLabel, { color: colors.textMuted }]}>{k.label}</Text>
                </SurfaceCard>
              ))}
            </View>

            <View style={[styles.middleGrid, isWide && styles.middleGridWide]}>
              <SurfaceCard style={[styles.demandCard, isWide && { flex: 2 }]}>
                <View style={styles.sectionHeaderRow}>
                  <View style={styles.sectionHeaderTextCol}>
                    <Text style={[styles.sectionTitle, { color: colors.text }]}>
                      Demanda registrada na sua loja
                    </Text>
                    <Text style={[styles.sectionSub, { color: colors.textMuted }]}>
                      Volume real de pesquisas por termo vinculadas à sua loja
                    </Text>
                  </View>
                  <Text style={[styles.totalBuscasBadge, { color: colors.primary }]}>
                    {totalBuscasLoja} buscas
                  </Text>
                </View>

                {termosCruzados.length === 0 ? (
                  <Text style={[styles.emptyBlockText, { color: colors.textMuted }]}>
                    Ainda não há buscas registradas para os produtos da sua loja.
                  </Text>
                ) : (
                  <View style={styles.barsChartRow}>
                    {termosCruzados.slice(0, 7).map((t, idx) => (
                      <View key={t.termo} style={styles.barColumn}>
                        <Text style={[styles.barValueLabel, { color: colors.textMuted }]}>
                          {t.quantidade}
                        </Text>
                        <View style={styles.barTrackVertical}>
                          <View
                            style={[
                              styles.barFillVertical,
                              {
                                height: `${Math.max(14, t.percentualBarra)}%`,
                                backgroundColor:
                                  idx === 0 ? colors.primary : colors.primarySoft,
                              },
                            ]}
                          />
                        </View>
                        <Text
                          style={[styles.barCaption, { color: colors.textMuted }]}
                          numberOfLines={1}
                        >
                          {t.termo}
                        </Text>
                      </View>
                    ))}
                  </View>
                )}
              </SurfaceCard>

              <View
                style={[
                  styles.aiInsightsCard,
                  isWide && { flex: 1 },
                  { backgroundColor: colors.ink },
                  Platform.OS === 'web'
                    ? {
                        backgroundImage:
                          'linear-gradient(135deg, #0F1D22 0%, #103A35 100%)',
                      }
                    : null,
                ]}
              >
                <View style={styles.aiHeaderRow}>
                  <View style={styles.aiHeaderLeft}>
                    <Ionicons name="sparkles" size={16} color={colors.highlight} />
                    <Text style={[styles.aiEyebrow, { color: colors.highlight }]}>
                      INSIGHTS DA IA
                    </Text>
                  </View>
                  <Pressable
                    accessibilityRole="button"
                    disabled={carregandoIa}
                    onPress={consultarInsightsIA}
                    style={[
                      styles.aiRefreshBtn,
                      { backgroundColor: colors.sidebarAccent },
                    ]}
                  >
                    {carregandoIa ? (
                      <ActivityIndicator size="small" color={colors.highlight} />
                    ) : (
                      <Text style={[styles.aiRefreshText, { color: colors.inkForeground }]}>
                        Analisar com IA
                      </Text>
                    )}
                  </Pressable>
                </View>

                <View style={styles.aiInsightsList}>
                  {insightsDerivados.map((texto, idx) => (
                    <View
                      key={`insight-${idx}`}
                      style={[
                        styles.aiInsightItem,
                        { backgroundColor: colors.sidebarAccent },
                      ]}
                    >
                      <Text style={[styles.aiInsightText, { color: colors.inkForeground }]}>
                        {texto}
                      </Text>
                    </View>
                  ))}
                </View>
              </View>
            </View>

            <View style={[styles.bottomGrid, isWide && styles.bottomGridWide]}>
              <SurfaceCard style={[styles.halfCard, isWide && styles.halfCardWide]}>
                <Text style={[styles.sectionTitle, { color: colors.text }]}>
                  Competitividade de preço
                </Text>
                <Text style={[styles.sectionSub, { color: colors.textMuted }]}>
                  Seus produtos vs. média das ofertas cadastradas
                </Text>

                {competitividade.length === 0 ? (
                  <Text style={[styles.emptyBlockText, { color: colors.textMuted }]}>
                    Cadastre produtos e ofertas para comparar seus preços.
                  </Text>
                ) : (
                  <ScrollView
                    nestedScrollEnabled
                    showsVerticalScrollIndicator
                    style={[styles.cardInnerScroll, { maxHeight: listMaxHeight }]}
                    contentContainerStyle={styles.compList}
                  >
                    {competitividade.map((item, idx) => {
                      const diff = item.diferencaPercentual;
                      const menorOuIgual = diff <= 0;
                      return (
                        <View
                          key={item.produtoId}
                          style={[
                            styles.compRow,
                            isWide && styles.compRowWide,
                            idx > 0 && {
                              borderTopWidth: 1,
                              borderTopColor: colors.border,
                            },
                          ]}
                        >
                          <View style={styles.compTopLine}>
                            <Text
                              style={[styles.compName, { color: colors.text }]}
                              numberOfLines={2}
                            >
                              {item.nome}
                            </Text>
                            {!isWide ? (
                              <PillBadge
                                label={`${diff > 0 ? '+' : ''}${diff.toFixed(1)}%`}
                                variant={menorOuIgual ? 'success' : 'destructive'}
                                style={styles.compBadge}
                              />
                            ) : null}
                          </View>
                          <View style={[styles.compNumbers, !isWide && styles.compNumbersMobile]}>
                            <Text style={[styles.compAvg, { color: colors.textMuted }]}>
                              Média {formatarPrecoBrl(item.mediaMercado)}
                            </Text>
                            <Text style={[styles.compMe, { color: colors.text }]}>
                              {formatarPrecoBrl(item.meuPreco)}
                            </Text>
                            {isWide ? (
                              <PillBadge
                                label={`${diff > 0 ? '+' : ''}${diff.toFixed(1)}%`}
                                variant={menorOuIgual ? 'success' : 'destructive'}
                                style={styles.compBadge}
                              />
                            ) : null}
                          </View>
                        </View>
                      );
                    })}
                  </ScrollView>
                )}
              </SurfaceCard>

              <SurfaceCard style={[styles.halfCard, isWide && styles.halfCardWide]}>
                <Text style={[styles.sectionTitle, { color: colors.text }]}>
                  Mais procurados na sua loja
                </Text>
                <Text style={[styles.sectionSub, { color: colors.textMuted }]}>
                  Oportunidades de estoque a partir das buscas reais
                </Text>

                {termosCruzados.length === 0 ? (
                  <Text style={[styles.emptyBlockText, { color: colors.textMuted }]}>
                    Nenhuma pesquisa registrada até o momento.
                  </Text>
                ) : (
                  <ScrollView
                    nestedScrollEnabled
                    showsVerticalScrollIndicator
                    style={[styles.cardInnerScroll, { maxHeight: listMaxHeight }]}
                    contentContainerStyle={styles.procuradosList}
                  >
                    {termosCruzados.map((t) => (
                      <View key={t.termo} style={styles.procuradoItem}>
                        <View style={styles.procuradoTop}>
                          <View style={styles.procuradoLabelRow}>
                            <Ionicons
                              name="bag-handle-outline"
                              size={14}
                              color={colors.textMuted}
                            />
                            <Text
                              style={[styles.procuradoTermo, { color: colors.text }]}
                              numberOfLines={2}
                            >
                              {t.termo} ({t.quantidade})
                            </Text>
                          </View>
                          <Text
                            style={[
                              styles.procuradoStatus,
                              {
                                color: t.emCatalogo
                                  ? colors.textMuted
                                  : colors.destructive,
                              },
                            ]}
                          >
                            {t.emCatalogo ? 'No catálogo' : 'Não cadastrado'}
                          </Text>
                        </View>
                        <View
                          style={[
                            styles.progressTrack,
                            { backgroundColor: colors.cardMuted },
                          ]}
                        >
                          <View
                            style={[
                              styles.progressFill,
                              {
                                width: `${t.percentualBarra}%`,
                                backgroundColor: t.emCatalogo
                                  ? colors.primary
                                  : colors.highlight,
                              },
                            ]}
                          />
                        </View>
                      </View>
                    ))}
                  </ScrollView>
                )}
              </SurfaceCard>
            </View>
          </>
        )}
      </View>
    </FormScreen>
  );
}

const styles = StyleSheet.create({
  pageWrap: {
    gap: 20,
    paddingBottom: 24,
  },
  headerActions: {
    flexDirection: 'row',
    gap: 8,
  },
  actionBtnOutline: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 6,
    borderWidth: 1,
    borderRadius: radii.md,
    paddingHorizontal: 12,
    paddingVertical: 8,
  },
  actionBtnOutlineText: {
    fontFamily: fonts.sans,
    fontSize: 12,
    fontWeight: '700',
  },
  kpiGrid: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: 10,
  },
  kpiGridWide: {
    gap: 12,
  },
  kpiCard: {
    flexGrow: 1,
    flexBasis: '46%',
    minWidth: 140,
    padding: 14,
  },
  kpiCardWide: {
    flex: 1,
    minWidth: 190,
    padding: 18,
  },
  kpiTopRow: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    gap: 6,
  },
  kpiIconBox: {
    width: 32,
    height: 32,
    borderRadius: radii.md,
    alignItems: 'center',
    justifyContent: 'center',
  },
  kpiValue: {
    fontFamily: fonts.display,
    fontSize: 22,
    fontWeight: '700',
    marginTop: 10,
  },
  kpiLabel: {
    fontFamily: fonts.sans,
    fontSize: 12,
    marginTop: 3,
  },
  middleGrid: {
    gap: 14,
  },
  middleGridWide: {
    flexDirection: 'row',
    alignItems: 'stretch',
  },
  demandCard: {
    padding: 18,
    justifyContent: 'space-between',
  },
  sectionHeaderRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'flex-start',
    gap: 12,
  },
  sectionHeaderTextCol: {
    flex: 1,
    minWidth: 0,
  },
  sectionTitle: {
    fontFamily: fonts.display,
    fontSize: 16,
    fontWeight: '700',
  },
  sectionSub: {
    fontFamily: fonts.sans,
    fontSize: 12,
    marginTop: 2,
  },
  totalBuscasBadge: {
    fontFamily: fonts.display,
    fontSize: 18,
    fontWeight: '700',
  },
  barsChartRow: {
    flexDirection: 'row',
    alignItems: 'flex-end',
    height: 170,
    gap: 10,
    marginTop: 20,
    paddingTop: 10,
  },
  barColumn: {
    flex: 1,
    height: '100%',
    alignItems: 'center',
    justifyContent: 'flex-end',
    gap: 6,
  },
  barValueLabel: {
    fontFamily: fonts.sans,
    fontSize: 11,
    fontWeight: '700',
  },
  barTrackVertical: {
    flex: 1,
    width: '100%',
    justifyContent: 'flex-end',
  },
  barFillVertical: {
    width: '100%',
    borderTopLeftRadius: 8,
    borderTopRightRadius: 8,
  },
  barCaption: {
    fontFamily: fonts.sans,
    fontSize: 11,
    maxWidth: 72,
    textAlign: 'center',
  },
  aiInsightsCard: {
    borderRadius: radii.xl,
    padding: 18,
    gap: 14,
  },
  aiHeaderRow: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    gap: 8,
  },
  aiHeaderLeft: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 6,
  },
  aiEyebrow: {
    fontFamily: fonts.sans,
    fontSize: 11,
    fontWeight: '800',
    letterSpacing: 1.2,
  },
  aiRefreshBtn: {
    borderRadius: radii.sm,
    paddingHorizontal: 10,
    paddingVertical: 5,
  },
  aiRefreshText: {
    fontFamily: fonts.sans,
    fontSize: 11,
    fontWeight: '700',
  },
  aiInsightsList: {
    gap: 10,
  },
  aiInsightItem: {
    borderRadius: radii.md,
    padding: 12,
  },
  aiInsightText: {
    fontFamily: fonts.sans,
    fontSize: 13,
    lineHeight: 19,
  },
  bottomGrid: {
    gap: 14,
  },
  bottomGridWide: {
    flexDirection: 'row',
    alignItems: 'stretch',
  },
  halfCard: {
    padding: 18,
    overflow: 'hidden',
  },
  halfCardWide: {
    flex: 1,
  },
  cardInnerScroll: {
    marginTop: 12,
  },
  compList: {
    paddingRight: 2,
  },
  compRow: {
    flexDirection: 'column',
    paddingVertical: 10,
    gap: 6,
  },
  compRowWide: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    gap: 8,
  },
  compTopLine: {
    flexDirection: 'row',
    alignItems: 'flex-start',
    justifyContent: 'space-between',
    gap: 8,
    flex: 1,
    minWidth: 0,
  },
  compName: {
    flex: 1,
    fontFamily: fonts.sans,
    fontSize: 13.5,
    fontWeight: '600',
    lineHeight: 18,
  },
  compNumbers: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 10,
  },
  compNumbersMobile: {
    justifyContent: 'space-between',
  },
  compAvg: {
    fontFamily: fonts.sans,
    fontSize: 12,
  },
  compMe: {
    fontFamily: fonts.display,
    fontSize: 13.5,
    fontWeight: '700',
    minWidth: 68,
    textAlign: 'right',
  },
  compBadge: {
    minWidth: 56,
    alignItems: 'center',
    flexShrink: 0,
  },
  procuradosList: {
    gap: 14,
    paddingRight: 2,
  },
  procuradoItem: {
    gap: 6,
  },
  procuradoTop: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'flex-start',
    gap: 8,
  },
  procuradoLabelRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 6,
    flex: 1,
    minWidth: 0,
  },
  procuradoTermo: {
    flex: 1,
    fontFamily: fonts.sans,
    fontSize: 13,
    fontWeight: '600',
    lineHeight: 18,
  },
  procuradoStatus: {
    fontFamily: fonts.sans,
    fontSize: 11,
    fontWeight: '700',
    flexShrink: 0,
  },
  progressTrack: {
    height: 8,
    borderRadius: 999,
    overflow: 'hidden',
  },
  progressFill: {
    height: 8,
    borderRadius: 999,
  },
  emptyBlockText: {
    fontFamily: fonts.sans,
    fontSize: 13,
    marginTop: 18,
  },
});
