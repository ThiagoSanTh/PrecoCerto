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

function TabItem({ route, index, focused, options, navigation, colors, badgeCount = 0, horizontal }) {
  const label = options.tabBarLabel ?? options.title ?? route.name;
  const base = ICONS[route.name] || 'ellipse';
  const iconName = focused ? base : `${base}-outline`;
  const showBadge = route.name === 'Mensagens' && badgeCount > 0;
  const iconSize = horizontal ? 20 : 26;
  const labelSize = horizontal ? 14 : 13;

  return (
    <Pressable
      accessibilityRole="button"
      onPress={() => {
        const event = navigation.emit({ type: 'tabPress', target: route.key, canPreventDefault: true });
        if (!focused && !event.defaultPrevented) navigation.navigate(route.name);
      }}
      style={[styles.item, horizontal && styles.itemHorizontal, focused && horizontal && styles.itemHorizontalFocused]}
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
          marginTop: horizontal ? 0 : 2,
          marginLeft: horizontal ? 6 : 0,
          color: focused ? colors.primary : colors.textMuted,
          fontWeight: focused ? '700' : '500',
        }}
      >
        {label}
      </Text>
    </Pressable>
  );
}

export default function CustomTabBar({ state, descriptors, navigation, badgeCount = 0 }) {
  const { colors } = useTheme();
  const { useTopNav } = useLayoutProfile();

  if (useTopNav) {
    return (
      <View style={[styles.topBar, { backgroundColor: colors.surface, borderBottomColor: colors.border }]}>
        {state.routes.map((route, index) => {
          const { options } = descriptors[route.key];
          const focused = state.index === index;
          return (
            <TabItem
              key={route.key}
              route={route}
              index={index}
              focused={focused}
              options={options}
              navigation={navigation}
              colors={colors}
              badgeCount={badgeCount}
              horizontal
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
            index={index}
            focused={focused}
            options={options}
            navigation={navigation}
            colors={colors}
            badgeCount={badgeCount}
            horizontal={false}
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
  topBar: {
    flexDirection: 'row',
    borderBottomWidth: 1,
    paddingHorizontal: 12,
    paddingVertical: 10,
    flexWrap: 'wrap',
    gap: 4,
    justifyContent: 'center',
  },
  item: {
    flex: 1,
    alignItems: 'center',
    justifyContent: 'center',
  },
  itemHorizontal: {
    flex: 0,
    flexDirection: 'row',
    alignItems: 'center',
    paddingHorizontal: 14,
    paddingVertical: 8,
    borderRadius: 8,
  },
  itemHorizontalFocused: {
    backgroundColor: 'rgba(45, 212, 191, 0.12)',
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
