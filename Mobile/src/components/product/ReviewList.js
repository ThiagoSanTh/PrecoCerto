import { useMemo } from 'react';
import { View, Text, StyleSheet } from 'react-native';
import StarRating from './StarRating';
import { useLayoutProfile } from '../../hooks/useLayoutProfile';

function formatarData(data) {
  if (!data) return '';
  const d = new Date(data);
  if (Number.isNaN(d.getTime())) return '';
  return d.toLocaleDateString('pt-BR');
}

const baseStyles = StyleSheet.create({
  container: {
    marginTop: 8,
  },
  titulo: {
    fontSize: 18,
    fontWeight: '700',
    color: '#0F172A',
    marginBottom: 8,
  },
  resumo: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 8,
    marginBottom: 12,
  },
  quantidade: {
    fontSize: 13,
    color: '#64748B',
  },
  vazio: {
    fontSize: 14,
    color: '#64748B',
    marginBottom: 8,
  },
  card: {
    backgroundColor: '#F8FAFC',
    borderRadius: 8,
    padding: 12,
    marginBottom: 8,
    borderWidth: 1,
    borderColor: '#E2E8F0',
  },
  cardHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: 4,
  },
  data: {
    fontSize: 12,
    color: '#94A3B8',
  },
  comentario: {
    fontSize: 14,
    color: '#475569',
    lineHeight: 20,
  },
});

const webStyles = StyleSheet.create({
  titulo: {
    fontSize: 20,
  },
  quantidade: {
    fontSize: 15,
  },
  vazio: {
    fontSize: 16,
  },
  data: {
    fontSize: 14,
  },
  comentario: {
    fontSize: 16,
    lineHeight: 24,
  },
});

export default function ReviewList({ media, avaliacoes = [] }) {
  const { isWeb } = useLayoutProfile();
  const lista = avaliacoes.slice(0, 5);
  const starSizeResumo = isWeb ? 20 : 18;
  const starSizeCard = isWeb ? 16 : 14;

  const styles = useMemo(
    () => ({
      container: baseStyles.container,
      titulo: [baseStyles.titulo, isWeb && webStyles.titulo],
      resumo: baseStyles.resumo,
      quantidade: [baseStyles.quantidade, isWeb && webStyles.quantidade],
      vazio: [baseStyles.vazio, isWeb && webStyles.vazio],
      card: baseStyles.card,
      cardHeader: baseStyles.cardHeader,
      data: [baseStyles.data, isWeb && webStyles.data],
      comentario: [baseStyles.comentario, isWeb && webStyles.comentario],
    }),
    [isWeb]
  );

  return (
    <View style={styles.container}>
      <Text style={styles.titulo}>Avaliações da loja</Text>

      {media?.quantidade > 0 ? (
        <View style={styles.resumo}>
          <StarRating nota={media.media} size={starSizeResumo} showValue />
          <Text style={styles.quantidade}>
            ({media.quantidade} {media.quantidade === 1 ? 'avaliação' : 'avaliações'})
          </Text>
        </View>
      ) : (
        <Text style={styles.vazio}>Esta loja ainda não possui avaliações.</Text>
      )}

      {lista.map((av) => (
        <View key={av.id} style={styles.card}>
          <View style={styles.cardHeader}>
            <StarRating nota={av.nota} size={starSizeCard} />
            <Text style={styles.data}>{formatarData(av.dataAvaliacao)}</Text>
          </View>
          {av.comentario ? (
            <Text style={styles.comentario}>{av.comentario}</Text>
          ) : null}
        </View>
      ))}
    </View>
  );
}
