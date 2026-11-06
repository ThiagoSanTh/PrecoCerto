import { View, Text, Pressable, StyleSheet, ScrollView } from 'react-native';
import { CATEGORIAS_PRODUTO } from '../../utils/categoriasProduto';
import { useTheme } from '../../context/ThemeContext';

export default function CategoriaPicker({ label = 'Categoria', value, onChange }) {
  const { colors } = useTheme();
  return (
    <View style={styles.container}>
      <Text style={[styles.label, { color: colors.label }]}>{label}</Text>
      <ScrollView horizontal showsHorizontalScrollIndicator={false} contentContainerStyle={styles.row}>
        {CATEGORIAS_PRODUTO.map((cat) => {
          const ativo = Number(value) === cat.valor;
          return (
            <Pressable
              key={cat.valor}
              accessibilityRole="button"
              accessibilityState={{ selected: ativo }}
              style={[
                styles.chip,
                { borderColor: colors.primary },
                ativo && { backgroundColor: colors.primary },
              ]}
              onPress={() => onChange(cat.valor)}
            >
              <Text
                style={[
                  styles.chipText,
                  { color: ativo ? colors.primaryForeground : colors.primary },
                ]}
              >
                {cat.label}
              </Text>
            </Pressable>
          );
        })}
      </ScrollView>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    marginBottom: 12,
  },
  label: {
    fontSize: 12,
    fontWeight: '600',
    marginBottom: 6,
  },
  row: {
    gap: 8,
    paddingRight: 8,
  },
  chip: {
    paddingHorizontal: 14,
    paddingVertical: 8,
    borderRadius: 20,
    borderWidth: 1,
  },
  chipText: {
    fontSize: 13,
    fontWeight: '600',
  },
});
