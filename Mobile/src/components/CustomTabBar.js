import { View, Text, Pressable, StyleSheet, ScrollView, Platform } from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { CommonActions } from '@react-navigation/native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { useTheme } from '../context/ThemeContext';
import { useAuth } from '../context/AuthContext';
import { useLayoutProfile } from '../hooks/useLayoutProfile';
import { BrandLogo } from './ui/DesignPrimitives';
import { fonts, radii, shadows } from '../theme';

const ICONS = {
  Buscar: 'search',
  IA: 'sparkles',
  Favoritos: 'heart',
  Histórico: 'time',
  Mensagens: 'chatbubble-ellipses',
  Perfil: 'person',
  Painel: 'bar-chart',
  Produtos: 'storefront',
  Loja: 'pricetag',
  Conta: 'person',
};

const MOBILE_USER_ROUTES = new Set(['Buscar', 'IA', 'Favoritos', 'Mensagens', 'Perfil']);
const MOBILE_STORE_ROUTES = new Set(['Painel', 'Produtos', 'IA', 'Mensagens', 'Conta']);

function iniciaisUsuario(nome) {
  const partes = String(nome || 'Usuário')
    .trim()
    .split(/\s+/)
    .filter(Boolean);
  if (partes.length === 0) return 'PC';
  if (partes.length === 1) return partes[0].slice(0, 2).toUpperCase();
  return `${partes[0][0]}${partes[partes.length - 1][0]}`.toUpperCase();
}

function SidebarNavItem({
  label,
  iconName,
  focused,
  badgeText,
  onPress,
  colors,
}) {
  return (
    <Pressable
      accessibilityRole="button"
      accessibilityState={{ selected: focused }}
      onPress={onPress}
      style={[
        styles.sidebarItem,
        focused && { backgroundColor: colors.sidebarAccent },
      ]}
    >
      <Ionicons
        name={focused ? iconName : `${iconName}-outline`}
        size={18}
        color={focused ? colors.sidebarPrimary : colors.sidebarForeground}
      />
      <Text
        style={[
          styles.sidebarItemLabel,
          {
            color: focused
              ? colors.sidebarAccentForeground
              : colors.sidebarForeground,
          },
        ]}
        numberOfLines={1}
      >
        {label}
      </Text>
      {badgeText ? (
        <View
          style={[
            styles.sidebarBadge,
            { backgroundColor: colors.sidebarPrimary },
          ]}
        >
          <Text
            style={[
              styles.sidebarBadgeText,
              { color: colors.sidebarPrimaryForeground },
            ]}
          >
            {badgeText}
          </Text>
        </View>
      ) : null}
    </Pressable>
  );
}

export default function CustomTabBar({ state, descriptors, navigation, badgeCount = 0 }) {
  const { colors } = useTheme();
  const {
    session,
    emModoLoja,
    temModoLoja,
    setAppMode,
    logout,
  } = useAuth();
  const { useSidebarNav, sidebarWidth, isCompactPhone } = useLayoutProfile();
  const insets = useSafeAreaInsets();

  const perfil = session?.perfil;
  const nomeExibicao = perfil?.nomeUsuario || 'Visitante';
  const subtituloPerfil = emModoLoja
    ? perfil?.nomeLoja || 'Modo Lojista'
    : temModoLoja
      ? 'Modo Cliente · Loja vinculada'
      : session
        ? 'Conta Cliente'
        : 'Convidado';

  async function handleLogout() {
    await logout();
    navigation.dispatch(
      CommonActions.reset({
        index: 0,
        routes: [{ name: 'Login' }],
      })
    );
  }

  function handleRoutePress(route, focused) {
    const event = navigation.emit({
      type: 'tabPress',
      target: route.key,
      canPreventDefault: true,
    });
    if (!focused && !event.defaultPrevented) {
      navigation.navigate(route.name);
    }
  }

  if (useSidebarNav) {
    const clienteCrossLinks = [
      { key: 'c-buscar', route: 'Buscar', label: 'Buscar', icon: 'search' },
      { key: 'c-ia', route: 'IA', label: 'Assistente IA', icon: 'sparkles', badge: 'Novo' },
      { key: 'c-fav', route: 'Favoritos', label: 'Favoritos', icon: 'heart' },
      { key: 'c-hist', route: 'Histórico', label: 'Histórico', icon: 'time' },
    ];
    const lojistaCrossLinks = [
      { key: 's-painel', route: 'Painel', label: 'Painel BI', icon: 'bar-chart' },
      { key: 's-prod', route: 'Produtos', label: 'Meus produtos', icon: 'storefront' },
    ];

    return (
      <View
        style={[
          styles.sidebar,
          {
            width: sidebarWidth,
            backgroundColor: colors.sidebar,
            borderRightColor: colors.sidebarBorder,
          },
        ]}
      >
        <View style={styles.logoWrap}>
          <BrandLogo dark />
        </View>

        <ScrollView
          style={styles.sidebarScroll}
          contentContainerStyle={styles.sidebarScrollContent}
          showsVerticalScrollIndicator={false}
        >
          <Text
            style={[
              styles.sectionHeading,
              { color: 'rgba(201, 216, 219, 0.5)' },
            ]}
          >
            {emModoLoja ? 'LOJISTA' : 'CLIENTE'}
          </Text>

          <View style={styles.navGroup}>
            {state.routes.map((route, index) => {
              const { options } = descriptors[route.key];
              const focused = state.index === index;
              const label = options.tabBarLabel ?? options.title ?? route.name;
              const iconName = ICONS[route.name] || 'ellipse';
              let badgeText = null;
              if (route.name === 'IA') badgeText = 'Novo';
              if (route.name === 'Mensagens' && badgeCount > 0) {
                badgeText = badgeCount > 99 ? '99+' : String(badgeCount);
              }

              return (
                <SidebarNavItem
                  key={route.key}
                  label={label}
                  iconName={iconName}
                  focused={focused}
                  badgeText={badgeText}
                  onPress={() => handleRoutePress(route, focused)}
                  colors={colors}
                />
              );
            })}
          </View>

          {temModoLoja ? (
            <>
              <Text
                style={[
                  styles.sectionHeading,
                  { color: 'rgba(201, 216, 219, 0.5)', marginTop: 22 },
                ]}
              >
                {emModoLoja ? 'CLIENTE' : 'LOJISTA'}
              </Text>
              <View style={styles.navGroup}>
                {(emModoLoja ? clienteCrossLinks : lojistaCrossLinks).map((item) => (
                  <SidebarNavItem
                    key={item.key}
                    label={item.label}
                    iconName={item.icon}
                    focused={false}
                    badgeText={item.badge}
                    onPress={async () => {
                      await setAppMode(emModoLoja ? 'user' : 'store');
                      setTimeout(() => {
                        try {
                          navigation.navigate(item.route);
                        } catch {
                          /* rota tratada após re-render das abas */
                        }
                      }, 50);
                    }}
                    colors={colors}
                  />
                ))}
              </View>
            </>
          ) : null}
        </ScrollView>

        <View
          style={[
            styles.userCard,
            { backgroundColor: colors.sidebarAccent },
          ]}
        >
          <View
            style={[
              styles.userAvatar,
              { backgroundColor: colors.primary },
            ]}
          >
            <Text
              style={[
                styles.userAvatarText,
                { color: colors.primaryForeground },
              ]}
            >
              {iniciaisUsuario(nomeExibicao)}
            </Text>
          </View>
          <View style={styles.userMeta}>
            <Text
              style={[
                styles.userName,
                { color: colors.sidebarAccentForeground },
              ]}
              numberOfLines={1}
            >
              {nomeExibicao}
            </Text>
            <Text
              style={[
                styles.userPlan,
                { color: 'rgba(201, 216, 219, 0.65)' },
              ]}
              numberOfLines={1}
            >
              {subtituloPerfil}
            </Text>
          </View>
          <Pressable
            accessibilityRole="button"
            accessibilityLabel="Sair da conta"
            onPress={handleLogout}
            hitSlop={10}
            style={styles.logoutBtn}
          >
            <Ionicons
              name="log-out-outline"
              size={18}
              color="rgba(201, 216, 219, 0.7)"
            />
          </Pressable>
        </View>
      </View>
    );
  }

  const allowedMobile = emModoLoja ? MOBILE_STORE_ROUTES : MOBILE_USER_ROUTES;
  const mobileRoutes = state.routes.filter((r) => allowedMobile.has(r.name));
  const safeBottom = Math.max(insets.bottom || 0, 12);
  const safeHorizontal = Math.max(insets.left || 0, insets.right || 0, isCompactPhone ? 10 : 14);

  return (
    <View
      style={[
        styles.mobileFloatWrap,
        {
          paddingBottom: safeBottom,
          paddingHorizontal: safeHorizontal,
          backgroundColor: colors.background,
        },
        Platform.OS === 'web'
          ? {
              paddingBottom: `calc(env(safe-area-inset-bottom, 0px) + ${safeBottom}px)`,
            }
          : null,
      ]}
      pointerEvents="box-none"
    >
      <View
        style={[
          styles.mobilePillBar,
          isCompactPhone && styles.mobilePillBarCompact,
          { backgroundColor: colors.ink },
        ]}
      >
        {mobileRoutes.map((route) => {
          const realIndex = state.routes.findIndex((r) => r.key === route.key);
          const focused = state.index === realIndex;
          const { options } = descriptors[route.key];
          const rawLabel = options.tabBarLabel ?? options.title ?? route.name;
          const shortLabel = String(rawLabel)
            .replace('Assistente ', '')
            .replace('Meus ', '')
            .replace('Painel ', '');
          const iconName = ICONS[route.name] || 'ellipse';
          const showBadge = route.name === 'Mensagens' && badgeCount > 0;

          return (
            <Pressable
              key={route.key}
              accessibilityRole="button"
              accessibilityState={{ selected: focused }}
              onPress={() => handleRoutePress(route, focused)}
              style={[
                styles.mobileTabItem,
                isCompactPhone && styles.mobileTabItemCompact,
                focused && { backgroundColor: colors.highlight },
              ]}
            >
              <View>
                <Ionicons
                  name={focused ? iconName : `${iconName}-outline`}
                  size={isCompactPhone ? 18 : 19}
                  color={focused ? colors.highlightForeground : colors.inkMuted}
                />
                {showBadge ? (
                  <View
                    style={[
                      styles.mobileBadge,
                      { backgroundColor: colors.primary },
                    ]}
                  >
                    <Text style={styles.mobileBadgeText}>
                      {badgeCount > 9 ? '9+' : badgeCount}
                    </Text>
                  </View>
                ) : null}
              </View>
              <Text
                style={[
                  styles.mobileTabLabel,
                  isCompactPhone && styles.mobileTabLabelCompact,
                  {
                    color: focused
                      ? colors.highlightForeground
                      : colors.inkMuted,
                  },
                ]}
                numberOfLines={1}
              >
                {shortLabel}
              </Text>
            </Pressable>
          );
        })}
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  sidebar: {
    flexDirection: 'column',
    borderRightWidth: 1,
    paddingHorizontal: 16,
    paddingTop: 20,
    paddingBottom: 16,
  },
  logoWrap: {
    paddingHorizontal: 8,
    paddingVertical: 8,
  },
  sidebarScroll: {
    flex: 1,
    marginTop: 18,
  },
  sidebarScrollContent: {
    paddingBottom: 16,
  },
  sectionHeading: {
    fontFamily: fonts.sans,
    fontSize: 11,
    fontWeight: '700',
    letterSpacing: 1.4,
    paddingHorizontal: 12,
    marginBottom: 8,
  },
  navGroup: {
    gap: 4,
  },
  sidebarItem: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 12,
    borderRadius: radii.lg,
    paddingHorizontal: 12,
    paddingVertical: 10,
  },
  sidebarItemLabel: {
    flex: 1,
    fontFamily: fonts.sans,
    fontSize: 14,
    fontWeight: '600',
  },
  sidebarBadge: {
    borderRadius: 999,
    paddingHorizontal: 8,
    paddingVertical: 2,
  },
  sidebarBadgeText: {
    fontFamily: fonts.sans,
    fontSize: 10,
    fontWeight: '800',
  },
  userCard: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 10,
    borderRadius: radii.xl,
    padding: 12,
    marginTop: 12,
  },
  userAvatar: {
    width: 36,
    height: 36,
    borderRadius: 18,
    alignItems: 'center',
    justifyContent: 'center',
  },
  userAvatarText: {
    fontFamily: fonts.sans,
    fontSize: 13,
    fontWeight: '700',
  },
  userMeta: {
    flex: 1,
    minWidth: 0,
  },
  userName: {
    fontFamily: fonts.sans,
    fontSize: 13,
    fontWeight: '700',
  },
  userPlan: {
    fontFamily: fonts.sans,
    fontSize: 11,
    marginTop: 1,
  },
  logoutBtn: {
    padding: 4,
  },
  mobileFloatWrap: {
    paddingHorizontal: 14,
    paddingBottom: 12,
    paddingTop: 6,
  },
  mobilePillBar: {
    flexDirection: 'row',
    borderRadius: radii.xl,
    padding: 6,
    gap: 4,
    ...shadows.float,
  },
  mobilePillBarCompact: {
    padding: 5,
    gap: 2,
  },
  mobileTabItem: {
    flex: 1,
    minWidth: 0,
    alignItems: 'center',
    justifyContent: 'center',
    borderRadius: radii.lg,
    paddingVertical: 8,
    paddingHorizontal: 2,
    gap: 2,
  },
  mobileTabItemCompact: {
    paddingVertical: 6,
  },
  mobileTabLabel: {
    fontFamily: fonts.sans,
    fontSize: 10,
    fontWeight: '700',
  },
  mobileTabLabelCompact: {
    fontSize: 9,
  },
  mobileBadge: {
    position: 'absolute',
    top: -4,
    right: -10,
    minWidth: 16,
    height: 16,
    borderRadius: 8,
    alignItems: 'center',
    justifyContent: 'center',
    paddingHorizontal: 3,
  },
  mobileBadgeText: {
    color: '#fff',
    fontSize: 9,
    fontWeight: '800',
  },
});
