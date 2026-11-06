import { View, Text, StyleSheet } from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { useTheme } from '../../context/ThemeContext';

export default function StarRating({ nota, size = 16, showValue = false }) {
  const { colors } = useTheme();
  const valor = Math.max(0, Math.min(5, Number(nota) || 0));
  const cheias = Math.floor(valor);
  const meia = valor - cheias >= 0.5;
  const vazias = 5 - cheias - (meia ? 1 : 0);

  return (
    <View
      style={styles.row}
      accessible
      accessibilityRole="text"
      accessibilityLabel={`Avaliação ${valor.toFixed(1)} de 5 estrelas`}
    >
      <View style={styles.starsWrap}>
        {Array.from({ length: cheias }).map((_, i) => (
          <Ionicons key={`f-${i}`} name="star" size={size} color="#F59E0B" />
        ))}
        {meia ? <Ionicons name="star-half" size={size} color="#F59E0B" /> : null}
        {Array.from({ length: vazias }).map((_, i) => (
          <Ionicons key={`e-${i}`} name="star-outline" size={size} color="#F59E0B" />
        ))}
      </View>
      {showValue ? (
        <Text style={[styles.value, { fontSize: size - 2, color: colors.textMuted }]}>
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
    gap: 5,
  },
  starsWrap: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 1,
  },
  value: {
    fontWeight: '600',
    fontVariant: ['tabular-nums'],
  },
});
