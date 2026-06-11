import { View, Text, Pressable, StyleSheet } from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { useTheme } from '../context/ThemeContext';
import { useLayoutProfile } from '../hooks/useLayoutProfile';

const ICONS = {
  Buscar: 'search',
  Favoritos: 'heart',
  Histórico: 'time',
  Mensagens: 'chatbubbles',
  Perfil: 'person',
  Produtos: 'pricetags',
  Loja: 'storefront',
  Conta: 'person',
};

export default function CustomTabBar({ state, descriptors, navigation, badgeCount = 0 }) {
  const { colors } = useTheme();
  const { isDesktopWeb, sidebarWidth } = useLayoutProfile();
  const variant = isDesktopWeb ? 'sidebar' : 'bottom';

  return (
    <View
      style={[
        variant === 'sidebar' ? styles.sidebar : styles.bar,
        variant === 'sidebar'
          ? {
              width: sidebarWidth,
              backgroundColor: colors.surface,
              borderRightColor: colors.border,
            }
          : {
              backgroundColor: colors.surface,
              borderTopColor: colors.border,
            },
      ]}
    >
      {state.routes.map((route, index) => {
        const { options } = descriptors[route.key];
        const label = options.tabBarLabel ?? options.title ?? route.name;
        const focused = state.index === index;
        const base = ICONS[route.name] || 'ellipse';
        const iconName = focused ? base : `${base}-outline`;
        const showBadge = route.name === 'Mensagens' && badgeCount > 0;

        return (
          <Pressable
            key={route.key}
            accessibilityRole="button"
            onPress={() => {
              const event = navigation.emit({ type: 'tabPress', target: route.key, canPreventDefault: true });
              if (!focused && !event.defaultPrevented) navigation.navigate(route.name);
            }}
            style={[
              variant === 'sidebar' ? styles.sidebarItem : styles.item,
              variant === 'sidebar' && focused && { backgroundColor: `${colors.primary}18` },
            ]}
          >
            <View>
              <Ionicons
                name={iconName}
                size={22}
                color={focused ? colors.primary : colors.textMuted}
              />
              {showBadge ? (
                <View style={[styles.badge, { backgroundColor: colors.primary }]}>
                  <Text style={styles.badgeText}>{badgeCount > 9 ? '9+' : badgeCount}</Text>
                </View>
              ) : null}
            </View>
            <Text
              style={{
                fontSize: variant === 'sidebar' ? 13 : 11,
                marginTop: variant === 'sidebar' ? 0 : 2,
                marginLeft: variant === 'sidebar' ? 10 : 0,
                color: focused ? colors.primary : colors.textMuted,
                fontWeight: focused ? '700' : '500',
              }}
            >
              {label}
            </Text>
          </Pressable>
        );
      })}
    </View>
  );
}

const styles = StyleSheet.create({
  bar: {
    flexDirection: 'row',
    borderTopWidth: 1,
    paddingBottom: 6,
    paddingTop: 8,
    elevation: 8,
    shadowColor: '#000',
    shadowOpacity: 0.08,
    shadowRadius: 8,
    shadowOffset: { width: 0, height: -2 },
  },
  sidebar: {
    flexDirection: 'column',
    borderRightWidth: 1,
    paddingTop: 16,
    paddingBottom: 16,
    paddingHorizontal: 8,
    height: '100%',
  },
  item: {
    flex: 1,
    alignItems: 'center',
    justifyContent: 'center',
  },
  sidebarItem: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingVertical: 12,
    paddingHorizontal: 12,
    borderRadius: 10,
    marginBottom: 4,
  },
  badge: {
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
  badgeText: {
    color: '#fff',
    fontSize: 10,
    fontWeight: '700',
  },
});
