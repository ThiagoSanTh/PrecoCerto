import { View, Text, Pressable, StyleSheet, Platform } from 'react-native';
import { FormField } from '../form';
import { useTheme } from '../../context/ThemeContext';

const MODO_MAPA = 'mapa';
const MODO_LISTA = 'lista';

function ViewModeToggle({ value, onChange, colors }) {
  const options = [
    { value: MODO_MAPA, label: 'Mapa' },
    { value: MODO_LISTA, label: 'Lista' },
  ];

  return (
    <View style={[styles.toggle, { backgroundColor: colors.background, borderColor: colors.border }]}>
      {options.map((opt) => {
        const active = value === opt.value;
        return (
          <Pressable
            key={opt.value}
            style={[styles.toggleButton, active && { backgroundColor: colors.primary }]}
            onPress={() => onChange(opt.value)}
          >
            <Text
              style={[
                styles.toggleText,
                { color: colors.textMuted },
                active && { color: '#fff', fontWeight: '700' },
              ]}
            >
              {opt.label}
            </Text>
          </Pressable>
        );
      })}
    </View>
  );
}

export default function MapSearchOverlay({
  termoBusca,
  onChangeText,
  onSubmit,
  modoVisualizacao,
  onModoChange,
  buscando = false,
}) {
  const { colors } = useTheme();

  return (
    <View style={styles.overlay} pointerEvents="box-none">
      <View
        style={[
          styles.bar,
          {
            backgroundColor: colors.surface,
            borderColor: colors.border,
          },
        ]}
        pointerEvents="auto"
      >
        <View style={styles.searchWrap}>
          <FormField
            label=""
            value={termoBusca}
            onChangeText={onChangeText}
            placeholder="Buscar produtos..."
            onSubmitEditing={onSubmit}
            returnKeyType="search"
            compact
          />
        </View>
        <ViewModeToggle value={modoVisualizacao} onChange={onModoChange} colors={colors} />
      </View>
      {buscando ? (
        <Text style={[styles.buscandoHint, { color: colors.textMuted }]}>Buscando...</Text>
      ) : null}
    </View>
  );
}

export { MODO_MAPA, MODO_LISTA };

export const MAP_SEARCH_OVERLAY_HEIGHT = 72;

const styles = StyleSheet.create({
  overlay: {
    position: 'absolute',
    top: 0,
    left: 0,
    right: 0,
    zIndex: 10,
    paddingHorizontal: 16,
    paddingTop: 12,
  },
  bar: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 10,
    borderRadius: 12,
    borderWidth: 1,
    paddingHorizontal: 12,
    paddingVertical: 8,
    ...(Platform.OS === 'web'
      ? {
          boxShadow: '0 4px 16px rgba(15, 23, 42, 0.12)',
        }
      : {
          elevation: 4,
          shadowColor: '#000',
          shadowOpacity: 0.1,
          shadowRadius: 8,
          shadowOffset: { width: 0, height: 2 },
        }),
  },
  searchWrap: {
    flex: 1,
  },
  toggle: {
    flexDirection: 'row',
    borderRadius: 8,
    borderWidth: 1,
    overflow: 'hidden',
  },
  toggleButton: {
    paddingHorizontal: 12,
    paddingVertical: 8,
  },
  toggleText: {
    fontSize: 13,
    fontWeight: '600',
  },
  buscandoHint: {
    fontSize: 12,
    marginTop: 4,
    marginLeft: 4,
  },
});
