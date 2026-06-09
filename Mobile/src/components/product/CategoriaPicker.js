import { View, Text, Pressable, StyleSheet, ScrollView } from 'react-native';
import { CATEGORIAS_PRODUTO } from '../../utils/categoriasProduto';
import { colors } from '../../theme';

export default function CategoriaPicker({ label = 'Categoria', value, onChange }) {
  return (
    <View style={styles.container}>
      <Text style={styles.label}>{label}</Text>
      <ScrollView horizontal showsHorizontalScrollIndicator={false} contentContainerStyle={styles.row}>
        {CATEGORIAS_PRODUTO.map((cat) => {
          const ativo = Number(value) === cat.valor;
          return (
            <Pressable
              key={cat.valor}
              style={[styles.chip, ativo && styles.chipAtivo]}
              onPress={() => onChange(cat.valor)}
            >
              <Text style={[styles.chipText, ativo && styles.chipTextAtivo]}>{cat.label}</Text>
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
    color: '#475569',
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
    borderColor: colors.primary,
  },
  chipAtivo: {
    backgroundColor: colors.primary,
  },
  chipText: {
    color: colors.primary,
    fontSize: 13,
    fontWeight: '600',
  },
  chipTextAtivo: {
    color: '#fff',
  },
});
