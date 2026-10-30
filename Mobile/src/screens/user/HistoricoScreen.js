import {
  ScrollView,
  View,
  Text,
  Pressable,
  Alert,
  ActivityIndicator,
  Platform,
  StyleSheet,
} from 'react-native';
import { useCallback, useMemo, useState } from 'react';
import { useFocusEffect, useNavigation } from '@react-navigation/native';
import { Ionicons } from '@expo/vector-icons';
import {
  listarHistoricoCliente,
  limparHistoricoCliente,
  removerItemHistorico,
} from '../../services/historicoService';
import { useAuth } from '../../context/AuthContext';
import { useTheme } from '../../context/ThemeContext';
import { FormScreen, ListCardText } from '../../components/form';
import { PageHeader, SurfaceCard } from '../../components/ui/DesignPrimitives';
import {
  agruparHistoricoPorDia,
  calcularIndicadoresHistorico,
} from '../../utils/insights';
import { fonts, radii } from '../../theme';

export default function HistoricoScreen() {
  const navigation = useNavigation();
  const [historico, setHistorico] = useState([]);
  const [loading, setLoading] = useState(true);
  const { session } = useAuth();
  const { colors } = useTheme();
  const clienteId = session?.perfil?.id;

  const carregar = useCallback(async () => {
    if (!clienteId) {
      setLoading(false);
      return;
    }
    setLoading(true);
    try {
      const data = await listarHistoricoCliente(clienteId);
      setHistorico(Array.isArray(data) ? data : []);
    } catch {
      Alert.alert('Erro', 'Não foi possível carregar histórico.');
    } finally {
      setLoading(false);
    }
  }, [clienteId]);

  useFocusEffect(
    useCallback(() => {
      carregar();
    }, [carregar])
  );

  const indicadores = useMemo(
    () => calcularIndicadoresHistorico(historico),
    [historico]
  );

  const grupos = useMemo(
    () => agruparHistoricoPorDia(historico),
    [historico]
  );

  async function executarLimpeza() {
    try {
      await limparHistoricoCliente(clienteId);
      setHistorico([]);
    } catch {
      Alert.alert('Erro', 'Não foi possível limpar o histórico.');
    }
  }

  function handleLimpar() {
    if (!clienteId || historico.length === 0) return;
    if (Platform.OS === 'web' && typeof window !== 'undefined') {
      if (window.confirm('Deseja apagar todas as buscas do histórico?')) {
        executarLimpeza();
      }
      return;
    }
    Alert.alert('Limpar histórico', 'Deseja apagar todas as buscas?', [
      { text: 'Cancelar', style: 'cancel' },
      {
        text: 'Limpar',
        style: 'destructive',
        onPress: executarLimpeza,
      },
    ]);
  }

  async function handleRemoverItem(item) {
    if (!item?.id) return;
    const backup = [...historico];
    setHistorico((prev) => prev.filter((x) => String(x.id) !== String(item.id)));
    try {
      await removerItemHistorico(item.id);
    } catch {
      setHistorico(backup);
      Alert.alert('Histórico', 'Não foi possível excluir este item.');
    }
  }

  function handleRepetirBusca(termo) {
    if (!termo) return;
    navigation.navigate('Search', {
      screen: 'SearchMain',
      params: { termoInicial: termo },
    });
  }

  if (!clienteId) {
    return (
      <FormScreen title="Histórico" subtitle="Faça login como cliente">
        <ListCardText>Nenhuma sessão de cliente ativa.</ListCardText>
      </FormScreen>
    );
  }

  return (
    <FormScreen
      title="Histórico"
      hideHeader
      scrollable={false}
      maxContentWidth={820}
    >
      <ScrollView
        showsVerticalScrollIndicator={false}
        contentContainerStyle={styles.scrollContent}
      >
        <PageHeader
          eyebrow="Suas buscas"
          title="Histórico"
          action={
            historico.length > 0 ? (
              <Pressable
                onPress={handleLimpar}
                style={[
                  styles.clearAllBtn,
                  { backgroundColor: colors.card, borderColor: colors.border },
                ]}
                accessibilityRole="button"
                accessibilityLabel="Limpar histórico"
              >
                <Ionicons
                  name="trash-outline"
                  size={15}
                  color={colors.textMuted}
                />
                <Text
                  style={[styles.clearAllText, { color: colors.textMuted }]}
                >
                  Limpar
                </Text>
              </Pressable>
            ) : null
          }
        />

        {/* KPI Cards */}
        <View style={styles.kpiGrid}>
          <SurfaceCard style={styles.kpiCard}>
            <Text style={[styles.kpiValue, { color: colors.text }]}>
              {indicadores.buscasMes}
            </Text>
            <Text style={[styles.kpiLabel, { color: colors.textMuted }]}>
              buscas no mês
            </Text>
          </SurfaceCard>

          <SurfaceCard style={styles.kpiCard}>
            <Text style={[styles.kpiValue, { color: colors.text }]}>
              {indicadores.termosUnicos}
            </Text>
            <Text style={[styles.kpiLabel, { color: colors.textMuted }]}>
              termos buscados
            </Text>
          </SurfaceCard>

          <SurfaceCard style={styles.kpiCard}>
            <Text style={[styles.kpiValue, { color: colors.text }]}>
              {indicadores.lojasVisitadas}
            </Text>
            <Text style={[styles.kpiLabel, { color: colors.textMuted }]}>
              lojas visitadas
            </Text>
          </SurfaceCard>
        </View>

        {loading ? (
          <ActivityIndicator
            size="large"
            color={colors.primary}
            style={{ marginTop: 32 }}
          />
        ) : grupos.length === 0 ? (
          <ListCardText
            style={[styles.emptyText, { color: colors.textMuted }]}
          >
            Nenhuma busca registrada ainda.
          </ListCardText>
        ) : (
          grupos.map((grupo) => (
            <View key={grupo.day} style={styles.groupSection}>
              <Text style={[styles.groupTitle, { color: colors.textMuted }]}>
                {grupo.day}
              </Text>
              <SurfaceCard noPadding style={styles.groupCard}>
                {grupo.items.map((item, idx) => {
                  const dataObj = item.dataPesquisa
                    ? new Date(item.dataPesquisa)
                    : null;
                  const hora =
                    dataObj && !Number.isNaN(dataObj.getTime())
                      ? dataObj.toLocaleTimeString('pt-BR', {
                          hour: '2-digit',
                          minute: '2-digit',
                        })
                      : '';
                  const sub = [
                    hora,
                    item.nomeLoja ? `Loja: ${item.nomeLoja}` : null,
                    item.nomeProduto ? `Produto: ${item.nomeProduto}` : null,
                  ]
                    .filter(Boolean)
                    .join(' · ');

                  return (
                    <View
                      key={String(item.id || idx)}
                      style={[
                        styles.histRow,
                        idx < grupo.items.length - 1 && {
                          borderBottomWidth: 1,
                          borderBottomColor: colors.border,
                        },
                      ]}
                    >
                      <Pressable
                        onPress={() => handleRepetirBusca(item.termoPesquisa)}
                        style={styles.histMainBtn}
                        accessibilityRole="button"
                        accessibilityLabel={`Buscar novamente por ${item.termoPesquisa}`}
                      >
                        <View
                          style={[
                            styles.clockCircle,
                            { backgroundColor: colors.surfaceSubtle },
                          ]}
                        >
                          <Ionicons
                            name="time-outline"
                            size={16}
                            color={colors.textMuted}
                          />
                        </View>
                        <View style={{ flex: 1, minWidth: 0 }}>
                          <Text
                            style={[styles.termText, { color: colors.text }]}
                            numberOfLines={1}
                          >
                            {item.termoPesquisa}
                          </Text>
                          {sub ? (
                            <Text
                              style={[
                                styles.metaText,
                                { color: colors.textMuted },
                              ]}
                              numberOfLines={1}
                            >
                              {sub}
                            </Text>
                          ) : null}
                        </View>
                        <Ionicons
                          name="search-outline"
                          size={16}
                          color={colors.textMuted}
                        />
                      </Pressable>

                      <Pressable
                        onPress={() => handleRemoverItem(item)}
                        hitSlop={8}
                        style={styles.deleteItemBtn}
                        accessibilityRole="button"
                        accessibilityLabel={`Remover ${item.termoPesquisa} do histórico`}
                      >
                        <Ionicons
                          name="trash-outline"
                          size={16}
                          color={colors.textMuted}
                        />
                      </Pressable>
                    </View>
                  );
                })}
              </SurfaceCard>
            </View>
          ))
        )}
      </ScrollView>
    </FormScreen>
  );
}

const styles = StyleSheet.create({
  scrollContent: {
    paddingBottom: 100,
    gap: 18,
  },
  clearAllBtn: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 6,
    paddingHorizontal: 13,
    paddingVertical: 8,
    borderRadius: radii.md,
    borderWidth: 1,
  },
  clearAllText: {
    fontFamily: fonts.sansSemiBold,
    fontSize: 12,
    fontWeight: '600',
  },
  kpiGrid: {
    flexDirection: 'row',
    gap: 12,
  },
  kpiCard: {
    flex: 1,
    padding: 16,
  },
  kpiValue: {
    fontFamily: fonts.display,
    fontSize: 22,
    fontWeight: '700',
  },
  kpiLabel: {
    fontFamily: fonts.sans,
    fontSize: 12,
    marginTop: 2,
  },
  groupSection: {
    gap: 8,
  },
  groupTitle: {
    fontFamily: fonts.sansBold,
    fontSize: 11,
    fontWeight: '700',
    textTransform: 'uppercase',
    letterSpacing: 1.1,
  },
  groupCard: {
    overflow: 'hidden',
  },
  histRow: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingRight: 12,
  },
  histMainBtn: {
    flex: 1,
    flexDirection: 'row',
    alignItems: 'center',
    gap: 12,
    paddingHorizontal: 16,
    paddingVertical: 14,
  },
  clockCircle: {
    width: 36,
    height: 36,
    borderRadius: 18,
    alignItems: 'center',
    justifyContent: 'center',
  },
  termText: {
    fontFamily: fonts.sansSemiBold,
    fontSize: 14,
    fontWeight: '600',
  },
  metaText: {
    fontFamily: fonts.sans,
    fontSize: 12,
    marginTop: 2,
  },
  deleteItemBtn: {
    padding: 8,
  },
  emptyText: {
    textAlign: 'center',
    marginTop: 32,
  },
});
