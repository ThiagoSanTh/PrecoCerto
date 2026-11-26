import { View, Text, FlatList, Pressable, Alert, ActivityIndicator, StyleSheet } from 'react-native';
import { useCallback, useState } from 'react';
import { useFocusEffect } from '@react-navigation/native';
import { Ionicons } from '@expo/vector-icons';
import { listarOfertas } from '../../services/ofertaService';
import { obterLoja } from '../../services/lojaService';
import { useAuth } from '../../context/AuthContext';
import { useTheme } from '../../context/ThemeContext';
import { useLayoutProfile } from '../../hooks/useLayoutProfile';
import { podeCriarLoja } from '../../utils/modoUsuario';
import { formatarPrecoBrl } from '../../utils/mapaUtils';
import {
  FormScreen,
  PrimaryButton,
  ListCard,
  ListCardText,
} from '../../components/form';
import { PageHeader, SurfaceCard, PillBadge } from '../../components/ui/DesignPrimitives';
import { fonts, radii, tabularNums } from '../../theme';

export default function StoreScreen({ navigation }) {
  const { session, isLojista } = useAuth();
  const { colors } = useTheme();
  const { useSidebarNav } = useLayoutProfile();
  const lojaId = session?.perfil?.lojaId;
  const [loja, setLoja] = useState(null);
  const [ofertas, setOfertas] = useState([]);
  const [loading, setLoading] = useState(true);

  useFocusEffect(
    useCallback(() => {
      carregar();
    }, [lojaId])
  );

  async function carregar() {
    if (!lojaId) {
      setLoading(false);
      return;
    }

    setLoading(true);
    try {
      const [lojaData, ofertasRes] = await Promise.all([
        obterLoja(lojaId),
        listarOfertas(1, 100),
      ]);
      setLoja(lojaData);
      setOfertas(ofertasRes.items.filter((o) => String(o.lojaId) === String(lojaId)));
    } catch {
      Alert.alert('Erro', 'Não foi possível carregar dados da loja');
    } finally {
      setLoading(false);
    }
  }

  if (loading) {
    return (
      <FormScreen title="Minha loja" hideHeader scrollable={false} maxContentWidth={1020}>
        <ActivityIndicator color={colors.primary} style={{ marginTop: 40 }} />
      </FormScreen>
    );
  }

  if (!lojaId) {
    const podeCriar = podeCriarLoja(session);
    return (
      <FormScreen
        title="Minha loja"
        subtitle={podeCriar ? 'Vincule uma loja ao seu perfil' : 'Loja vinculada ao perfil'}
        scrollable={false}
        footer={
          podeCriar ? (
            <PrimaryButton label="Criar loja" onPress={() => navigation.navigate('CreateStore')} />
          ) : undefined
        }
      >
        <ListCard title={podeCriar ? 'Nenhuma loja vinculada' : 'Loja em carregamento'}>
          <ListCardText>
            {podeCriar
              ? 'Cadastre sua loja para começar a publicar ofertas.'
              : 'Sua conta já está vinculada a uma loja. Atualize a tela ou entre novamente se os dados não aparecerem.'}
          </ListCardText>
        </ListCard>
      </FormScreen>
    );
  }

  return (
    <FormScreen title="Minha loja" hideHeader scrollable={false} maxContentWidth={1020}>
      <FlatList
        data={ofertas}
        keyExtractor={(item) => String(item.id)}
        showsVerticalScrollIndicator={false}
        contentContainerStyle={[
          styles.listContent,
          useSidebarNav && styles.listContentDesktop,
        ]}
        ListHeaderComponent={
          <View style={styles.headerWrap}>
            <PageHeader
              eyebrow="Gestão da loja"
              title={loja?.nomeFantasia || 'Minha loja'}
              action={
                <View style={styles.headerActions}>
                  <Pressable
                    onPress={() => navigation.navigate('CreateOferta')}
                    style={[styles.primaryBtn, { backgroundColor: colors.primary }]}
                    accessibilityRole="button"
                    accessibilityLabel="Nova oferta"
                  >
                    <Ionicons name="add" size={16} color={colors.primaryForeground} />
                    <Text style={[styles.primaryBtnText, { color: colors.primaryForeground }]}>
                      Nova oferta
                    </Text>
                  </Pressable>
                  {isLojista ? (
                    <Pressable
                      onPress={() => navigation.navigate('Vendedores', { lojaId })}
                      style={[
                        styles.secondaryBtn,
                        { backgroundColor: colors.card, borderColor: colors.border },
                      ]}
                      accessibilityRole="button"
                      accessibilityLabel="Gerenciar vendedores"
                    >
                      <Ionicons name="people-outline" size={16} color={colors.text} />
                      <Text style={[styles.secondaryBtnText, { color: colors.text }]}>
                        Vendedores
                      </Text>
                    </Pressable>
                  ) : null}
                </View>
              }
            />

            {loja ? (
              <SurfaceCard style={styles.storeInfoCard}>
                <View style={styles.storeInfoRow}>
                  <View style={[styles.storeIconBox, { backgroundColor: colors.primarySoft }]}>
                    <Ionicons name="storefront-outline" size={20} color={colors.primary} />
                  </View>
                  <View style={{ flex: 1, minWidth: 0 }}>
                    <Text style={[styles.storeName, { color: colors.text }]} numberOfLines={1}>
                      {loja.nomeFantasia}
                    </Text>
                    <Text style={[styles.storeMeta, { color: colors.textMuted }]} numberOfLines={1}>
                      {[loja.email, loja.telefone].filter(Boolean).join(' · ') || 'Loja ativa'}
                    </Text>
                  </View>
                </View>
              </SurfaceCard>
            ) : null}

            <View style={styles.sectionBar}>
              <Text style={[styles.sectionTitle, { color: colors.text }]}>
                Ofertas cadastradas
              </Text>
              <Text style={[styles.sectionCount, tabularNums, { color: colors.textMuted }]}>
                {ofertas.length} {ofertas.length === 1 ? 'oferta' : 'ofertas'}
              </Text>
            </View>
          </View>
        }
        renderItem={({ item }) => (
          <SurfaceCard style={styles.offerCard}>
            <View style={styles.offerRow}>
              <View style={{ flex: 1, minWidth: 0 }}>
                <Text style={[styles.offerTitle, { color: colors.text }]} numberOfLines={1}>
                  {item.nomeProduto || 'Produto'}
                </Text>
                <View style={styles.offerBadgeWrap}>
                  <PillBadge
                    label={item.disponivel ? 'Disponível' : 'Indisponível'}
                    variant={item.disponivel ? 'success' : 'destructive'}
                  />
                </View>
              </View>
              <Text style={[styles.offerPrice, tabularNums, { color: colors.text }]}>
                {formatarPrecoBrl(item.preco)}
              </Text>
            </View>
          </SurfaceCard>
        )}
        ListEmptyComponent={
          <ListCardText style={{ textAlign: 'center', marginTop: 24 }}>
            Cadastre produtos e crie ofertas.
          </ListCardText>
        }
      />
    </FormScreen>
  );
}

const styles = StyleSheet.create({
  listContent: {
    gap: 10,
    paddingBottom: 104,
  },
  listContentDesktop: {
    paddingBottom: 32,
  },
  headerWrap: {
    gap: 16,
    marginBottom: 4,
  },
  headerActions: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: 8,
  },
  primaryBtn: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 6,
    borderRadius: radii.md,
    paddingHorizontal: 14,
    paddingVertical: 9,
  },
  primaryBtnText: {
    fontFamily: fonts.sansSemiBold,
    fontSize: 13,
    fontWeight: '600',
  },
  secondaryBtn: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 6,
    borderRadius: radii.md,
    borderWidth: 1,
    paddingHorizontal: 14,
    paddingVertical: 9,
  },
  secondaryBtnText: {
    fontFamily: fonts.sansSemiBold,
    fontSize: 13,
    fontWeight: '600',
  },
  storeInfoCard: {
    padding: 16,
  },
  storeInfoRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 12,
  },
  storeIconBox: {
    width: 44,
    height: 44,
    borderRadius: radii.md,
    alignItems: 'center',
    justifyContent: 'center',
  },
  storeName: {
    fontFamily: fonts.display,
    fontSize: 16,
    fontWeight: '700',
  },
  storeMeta: {
    fontFamily: fonts.sans,
    fontSize: 13,
    marginTop: 2,
  },
  sectionBar: {
    flexDirection: 'row',
    alignItems: 'baseline',
    justifyContent: 'space-between',
  },
  sectionTitle: {
    fontFamily: fonts.display,
    fontSize: 16,
    fontWeight: '600',
  },
  sectionCount: {
    fontFamily: fonts.sansSemiBold,
    fontSize: 12,
    fontWeight: '600',
  },
  offerCard: {
    padding: 15,
  },
  offerRow: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    gap: 12,
  },
  offerTitle: {
    fontFamily: fonts.sansSemiBold,
    fontSize: 14.5,
    fontWeight: '600',
  },
  offerBadgeWrap: {
    marginTop: 6,
  },
  offerPrice: {
    fontFamily: fonts.display,
    fontSize: 17,
    fontWeight: '700',
  },
});
