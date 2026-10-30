import {
  View,
  Text,
  Pressable,
  Alert,
  StyleSheet,
} from 'react-native';
import { useCallback, useState } from 'react';
import { useFocusEffect } from '@react-navigation/native';
import { Ionicons } from '@expo/vector-icons';
import { useAuth } from '../../context/AuthContext';
import { useTheme } from '../../context/ThemeContext';
import { podeCriarLoja } from '../../utils/modoUsuario';
import { listarFavoritosCliente } from '../../services/favoritoService';
import { listarHistoricoCliente } from '../../services/historicoService';
import { obterLoja } from '../../services/lojaService';
import { FormScreen } from '../../components/form';
import { SurfaceCard } from '../../components/ui/DesignPrimitives';
import { fonts, radii } from '../../theme';

function obterIniciais(nome) {
  const limpo = String(nome || '').trim();
  if (!limpo) return 'PC';
  const partes = limpo.split(/\s+/);
  if (partes.length === 1) return partes[0].slice(0, 2).toUpperCase();
  return `${partes[0][0]}${partes[partes.length - 1][0]}`.toUpperCase();
}

export default function ProfileScreen({ navigation }) {
  const {
    session,
    logout,
    temModoLoja,
    emModoLoja,
    setAppMode,
    isCliente,
    sincronizarGpsCliente,
  } = useAuth();
  const { colors, isDark, alternarTema } = useTheme();
  const perfil = session?.perfil;
  const clienteId = isCliente ? perfil?.id : null;
  const lojaId = perfil?.lojaId || perfil?.lojaVinculadaId || null;

  const [totalFavoritos, setTotalFavoritos] = useState(0);
  const [totalBuscas, setTotalBuscas] = useState(0);
  const [nomeLoja, setNomeLoja] = useState(perfil?.nomeFantasia || '');

  useFocusEffect(
    useCallback(() => {
      let ativo = true;
      if (clienteId) {
        listarFavoritosCliente(clienteId, 1, 1)
          .then((res) => {
            if (ativo) setTotalFavoritos(Number(res?.total ?? res?.items?.length ?? 0));
          })
          .catch(() => {});
        listarHistoricoCliente(clienteId)
          .then((lista) => {
            if (ativo) setTotalBuscas(Array.isArray(lista) ? lista.length : 0);
          })
          .catch(() => {});
      }
      if (lojaId) {
        obterLoja(lojaId)
          .then((loja) => {
            if (ativo && loja?.nomeFantasia) setNomeLoja(loja.nomeFantasia);
          })
          .catch(() => {});
      }
      return () => {
        ativo = false;
      };
    }, [clienteId, lojaId])
  );

  async function handleLogout() {
    await logout();
    navigation.replace('Login');
  }

  async function handleAtualizarLocalizacao() {
    try {
      await sincronizarGpsCliente?.();
      Alert.alert(
        'Localização atualizada',
        'Suas coordenadas de GPS foram sincronizadas para mostrar as lojas mais próximas.'
      );
    } catch {
      Alert.alert(
        'Localização',
        'Verifique as permissões de localização do navegador ou aparelho.'
      );
    }
  }

  const rows = [
    {
      key: 'edit-profile',
      icon: 'person-outline',
      label: 'Editar dados pessoais',
      value: perfil?.telefone || 'Atualizar cadastro',
      onPress: () => navigation.navigate('EditProfile'),
    },
    {
      key: 'edit-email',
      icon: 'mail-outline',
      label: 'Alterar e-mail',
      value: perfil?.email || '',
      onPress: () => navigation.navigate('EditEmail'),
    },
    {
      key: 'change-password',
      icon: 'lock-closed-outline',
      label: 'Alterar senha',
      value: 'Segurança',
      onPress: () => navigation.navigate('ChangePassword'),
    },
    ...(!emModoLoja
      ? [
          {
            key: 'ai-assistant',
            icon: 'sparkles-outline',
            label: 'Assistente Preço Certo',
            value: 'Pesquisa com IA',
            onPress: () => navigation.navigate('AiChat'),
          },
          {
            key: 'gps-privacy',
            icon: 'location-outline',
            label: 'Privacidade e localização',
            value: 'GPS ao usar o app',
            onPress: handleAtualizarLocalizacao,
          },
        ]
      : []),
    {
      key: 'theme',
      icon: isDark ? 'moon-outline' : 'sunny-outline',
      label: 'Aparência do aplicativo',
      value: isDark ? 'Tema escuro' : 'Tema claro',
      onPress: alternarTema,
    },
  ];

  const iniciais = obterIniciais(perfil?.nomeUsuario);

  return (
    <FormScreen
      title={emModoLoja ? 'Conta lojista' : 'Meu perfil'}
      hideHeader
      scrollable
      maxContentWidth={780}
    >
      <View style={styles.container}>
        {/* Hero Ink Card */}
        <View style={[styles.heroCard, { backgroundColor: colors.ink }]}>
          <View style={styles.heroTopRow}>
            <View
              style={[
                styles.avatarBox,
                { backgroundColor: colors.highlight },
              ]}
            >
              <Text
                style={[
                  styles.avatarInitials,
                  { color: colors.highlightForeground },
                ]}
              >
                {iniciais}
              </Text>
            </View>

            <View style={{ flex: 1, minWidth: 0 }}>
              <Text
                style={[styles.userName, { color: colors.inkForeground }]}
                numberOfLines={1}
              >
                {perfil?.nomeUsuario || 'Usuário'}
              </Text>
              <Text
                style={[styles.userEmail, { color: colors.inkMuted }]}
                numberOfLines={1}
              >
                {perfil?.email || ''}
              </Text>
            </View>

            <Pressable
              onPress={() => navigation.navigate('EditProfile')}
              style={[
                styles.heroEditBtn,
                { backgroundColor: colors.sidebarAccent },
              ]}
              accessibilityRole="button"
              accessibilityLabel="Editar perfil"
            >
              <Ionicons
                name="pencil-outline"
                size={17}
                color={colors.inkForeground}
              />
            </Pressable>
          </View>

          <View style={styles.statsRow}>
            <View
              style={[
                styles.statBox,
                { backgroundColor: colors.sidebarAccent },
              ]}
            >
              <Text
                style={[styles.statValue, { color: colors.inkForeground }]}
              >
                {totalFavoritos}
              </Text>
              <Text style={[styles.statLabel, { color: colors.inkMuted }]}>
                favoritos
              </Text>
            </View>

            <View
              style={[
                styles.statBox,
                { backgroundColor: colors.sidebarAccent },
              ]}
            >
              <Text
                style={[styles.statValue, { color: colors.inkForeground }]}
              >
                {totalBuscas}
              </Text>
              <Text style={[styles.statLabel, { color: colors.inkMuted }]}>
                buscas salvas
              </Text>
            </View>

            <View
              style={[
                styles.statBox,
                { backgroundColor: colors.sidebarAccent },
              ]}
            >
              <Text
                style={[styles.statValue, { color: colors.inkForeground }]}
                numberOfLines={1}
              >
                {emModoLoja ? 'Lojista' : 'Cliente'}
              </Text>
              <Text style={[styles.statLabel, { color: colors.inkMuted }]}>
                modo ativo
              </Text>
            </View>
          </View>
        </View>

        {/* Action List Card */}
        <SurfaceCard noPadding style={styles.listCard}>
          {rows.map((row, idx) => (
            <Pressable
              key={row.key}
              onPress={row.onPress}
              style={[
                styles.settingRow,
                idx < rows.length - 1 && {
                  borderBottomWidth: 1,
                  borderBottomColor: colors.border,
                },
              ]}
              accessibilityRole="button"
              accessibilityLabel={row.label}
            >
              <View
                style={[
                  styles.settingIconBox,
                  { backgroundColor: colors.primarySoft },
                ]}
              >
                <Ionicons name={row.icon} size={17} color={colors.primary} />
              </View>
              <Text
                style={[styles.settingLabel, { color: colors.text }]}
                numberOfLines={1}
              >
                {row.label}
              </Text>
              <Text
                style={[styles.settingValue, { color: colors.textMuted }]}
                numberOfLines={1}
              >
                {row.value}
              </Text>
              <Ionicons
                name="chevron-forward"
                size={16}
                color={colors.textMuted}
              />
            </Pressable>
          ))}
        </SurfaceCard>

        {/* Store Mode / Create Store Highlight Card */}
        {temModoLoja && !emModoLoja ? (
          <Pressable
            onPress={() => setAppMode('store')}
            style={[
              styles.storeBanner,
              {
                backgroundColor: colors.primarySoft,
                borderColor: colors.primary,
              },
            ]}
            accessibilityRole="button"
            accessibilityLabel="Acessar painel do lojista"
          >
            <Ionicons
              name="storefront-outline"
              size={22}
              color={colors.primary}
            />
            <View style={{ flex: 1 }}>
              <Text style={[styles.storeBannerTitle, { color: colors.text }]}>
                {nomeLoja || 'Painel da Minha Loja'}
              </Text>
              <Text
                style={[
                  styles.storeBannerSub,
                  { color: colors.textSecondary },
                ]}
              >
                Acessar painel BI e gerenciar catálogo do lojista
              </Text>
            </View>
            <Ionicons
              name="chevron-forward"
              size={18}
              color={colors.primary}
            />
          </Pressable>
        ) : null}

        {temModoLoja && emModoLoja ? (
          <Pressable
            onPress={() => setAppMode('user')}
            style={[
              styles.storeBanner,
              {
                backgroundColor: colors.primarySoft,
                borderColor: colors.primary,
              },
            ]}
            accessibilityRole="button"
            accessibilityLabel="Alternar para modo cliente"
          >
            <Ionicons name="compass-outline" size={22} color={colors.primary} />
            <View style={{ flex: 1 }}>
              <Text style={[styles.storeBannerTitle, { color: colors.text }]}>
                Modo Cliente
              </Text>
              <Text
                style={[
                  styles.storeBannerSub,
                  { color: colors.textSecondary },
                ]}
              >
                Voltar para busca de produtos e mapa de ofertas
              </Text>
            </View>
            <Ionicons
              name="chevron-forward"
              size={18}
              color={colors.primary}
            />
          </Pressable>
        ) : null}

        {podeCriarLoja(session) ? (
          <Pressable
            onPress={() => navigation.navigate('CreateStore')}
            style={[
              styles.storeBanner,
              {
                backgroundColor: colors.primarySoft,
                borderColor: colors.primary,
              },
            ]}
            accessibilityRole="button"
            accessibilityLabel="Criar loja"
          >
            <Ionicons
              name="storefront-outline"
              size={22}
              color={colors.primary}
            />
            <View style={{ flex: 1 }}>
              <Text style={[styles.storeBannerTitle, { color: colors.text }]}>
                Tem um comércio local?
              </Text>
              <Text
                style={[
                  styles.storeBannerSub,
                  { color: colors.textSecondary },
                ]}
              >
                Cadastre sua loja e divulgue suas ofertas no Preço Certo
              </Text>
            </View>
            <Ionicons
              name="chevron-forward"
              size={18}
              color={colors.primary}
            />
          </Pressable>
        ) : null}

        {String(session?.tipo ?? '').toLowerCase() === 'admin' ? (
          <Text style={[styles.adminHint, { color: colors.textMuted }]}>
            Conta Administrador: o Assistente IA exibe diagnósticos avançados do
            MotorIA.
          </Text>
        ) : null}

        {/* Logout Button */}
        <Pressable
          onPress={handleLogout}
          style={[
            styles.logoutBtn,
            { backgroundColor: colors.card, borderColor: colors.border },
          ]}
          accessibilityRole="button"
          accessibilityLabel="Sair da conta"
        >
          <Ionicons name="log-out-outline" size={18} color={colors.danger} />
          <Text style={[styles.logoutText, { color: colors.danger }]}>
            Sair da conta
          </Text>
        </Pressable>
      </View>
    </FormScreen>
  );
}

const styles = StyleSheet.create({
  container: {
    gap: 18,
    paddingBottom: 90,
  },
  heroCard: {
    borderRadius: radii['2xl'],
    padding: 24,
  },
  heroTopRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 16,
  },
  avatarBox: {
    width: 64,
    height: 64,
    borderRadius: radii.xl,
    alignItems: 'center',
    justifyContent: 'center',
  },
  avatarInitials: {
    fontFamily: fonts.display,
    fontSize: 22,
    fontWeight: '700',
  },
  userName: {
    fontFamily: fonts.display,
    fontSize: 22,
    fontWeight: '700',
  },
  userEmail: {
    fontFamily: fonts.sans,
    fontSize: 13.5,
    marginTop: 2,
  },
  heroEditBtn: {
    width: 40,
    height: 40,
    borderRadius: radii.md,
    alignItems: 'center',
    justifyContent: 'center',
  },
  statsRow: {
    flexDirection: 'row',
    gap: 10,
    marginTop: 22,
  },
  statBox: {
    flex: 1,
    borderRadius: radii.md,
    padding: 12,
  },
  statValue: {
    fontFamily: fonts.display,
    fontSize: 16,
    fontWeight: '700',
  },
  statLabel: {
    fontFamily: fonts.sans,
    fontSize: 11.5,
    marginTop: 2,
  },
  listCard: {
    overflow: 'hidden',
  },
  settingRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 12,
    paddingHorizontal: 18,
    paddingVertical: 15,
  },
  settingIconBox: {
    width: 36,
    height: 36,
    borderRadius: radii.md,
    alignItems: 'center',
    justifyContent: 'center',
  },
  settingLabel: {
    flex: 1,
    fontFamily: fonts.sansSemiBold,
    fontSize: 14,
    fontWeight: '600',
  },
  settingValue: {
    fontFamily: fonts.sans,
    fontSize: 12,
    maxWidth: 180,
  },
  storeBanner: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 14,
    borderRadius: radii.xl,
    borderWidth: 1,
    padding: 18,
  },
  storeBannerTitle: {
    fontFamily: fonts.sansSemiBold,
    fontSize: 15,
    fontWeight: '600',
  },
  storeBannerSub: {
    fontFamily: fonts.sans,
    fontSize: 12.5,
    marginTop: 2,
  },
  adminHint: {
    fontFamily: fonts.sans,
    fontSize: 12,
    textAlign: 'center',
  },
  logoutBtn: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: 8,
    borderRadius: radii.md,
    borderWidth: 1,
    paddingVertical: 13,
  },
  logoutText: {
    fontFamily: fonts.sansSemiBold,
    fontSize: 14,
    fontWeight: '600',
  },
});
