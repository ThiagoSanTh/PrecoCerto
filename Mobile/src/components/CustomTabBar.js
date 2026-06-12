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

function TabItem({
  route,
  focused,
  options,
  navigation,
  colors,
  badgeCount = 0,
  variant = 'bottom',
}) {
  const label = options.tabBarLabel ?? options.title ?? route.name;
  const base = ICONS[route.name] || 'ellipse';
  const iconName = focused ? base : `${base}-outline`;
  const showBadge = route.name === 'Mensagens' && badgeCount > 0;
  const isSidebar = variant === 'sidebar';
  const iconSize = isSidebar ? 22 : 26;
  const labelSize = isSidebar ? 13 : 13;

  return (
    <Pressable
      accessibilityRole="button"
      onPress={() => {
        const event = navigation.emit({ type: 'tabPress', target: route.key, canPreventDefault: true });
        if (!focused && !event.defaultPrevented) navigation.navigate(route.name);
      }}
      style={[
        styles.item,
        isSidebar && styles.itemSidebar,
        focused && isSidebar && { backgroundColor: 'rgba(45, 212, 191, 0.12)' },
      ]}
    >
      <View>
        <Ionicons
          name={iconName}
          size={iconSize}
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
          fontSize: labelSize,
          marginTop: isSidebar ? 4 : 2,
          color: focused ? colors.primary : colors.textMuted,
          fontWeight: focused ? '700' : '500',
          textAlign: isSidebar ? 'center' : undefined,
        }}
      >
        {label}
      </Text>
    </Pressable>
  );
}

export default function CustomTabBar({ state, descriptors, navigation, badgeCount = 0 }) {
  const { colors } = useTheme();
  const { useSidebarNav, sidebarWidth } = useLayoutProfile();

  if (useSidebarNav) {
    return (
      <View
        style={[
          styles.sidebar,
          {
            width: sidebarWidth,
            backgroundColor: colors.surface,
            borderRightColor: colors.border,
          },
        ]}
      >
        {state.routes.map((route, index) => {
          const { options } = descriptors[route.key];
          const focused = state.index === index;
          return (
            <TabItem
              key={route.key}
              route={route}
              focused={focused}
              options={options}
              navigation={navigation}
              colors={colors}
              badgeCount={badgeCount}
              variant="sidebar"
            />
          );
        })}
      </View>
    );
  }

  return (
    <View style={[styles.bar, { backgroundColor: colors.surface, borderTopColor: colors.border }]}>
      {state.routes.map((route, index) => {
        const { options } = descriptors[route.key];
        const focused = state.index === index;
        return (
          <TabItem
            key={route.key}
            route={route}
            focused={focused}
            options={options}
            navigation={navigation}
            colors={colors}
            badgeCount={badgeCount}
            variant="bottom"
          />
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
    paddingTop: 56,
    paddingHorizontal: 8,
    paddingBottom: 16,
    gap: 12,
  },
  item: {
    flex: 1,
    alignItems: 'center',
    justifyContent: 'center',
  },
  itemSidebar: {
    flex: 0,
    paddingVertical: 14,
    paddingHorizontal: 8,
    borderRadius: 10,
    width: '100%',
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
