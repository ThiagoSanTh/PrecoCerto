import { View, Text, StyleSheet } from 'react-native';

export default function StarRating({ nota, size = 16, showValue = false }) {
  const valor = Math.max(0, Math.min(5, Number(nota) || 0));
  const cheias = Math.floor(valor);
  const meia = valor - cheias >= 0.5;
  const vazias = 5 - cheias - (meia ? 1 : 0);

  return (
    <View style={styles.row}>
      <Text style={[styles.star, { fontSize: size }]}>
        {'★'.repeat(cheias)}
        {meia ? '⯨' : ''}
        {'☆'.repeat(vazias)}
      </Text>
      {showValue ? (
        <Text style={[styles.value, { fontSize: size - 2 }]}>
          {valor.toFixed(1)}
        </Text>
      ) : null}
    </View>
  );
}

const styles = StyleSheet.create({
  row: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 4,
  },
  star: {
    color: '#F59E0B',
    letterSpacing: 1,
  },
  value: {
    color: '#64748B',
    fontWeight: '600',
  },
});
