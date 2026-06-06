import { View, Text, StyleSheet } from 'react-native';
import StarRating from './StarRating';

function formatarData(data) {
  if (!data) return '';
  const d = new Date(data);
  if (Number.isNaN(d.getTime())) return '';
  return d.toLocaleDateString('pt-BR');
}

export default function ReviewList({ media, avaliacoes = [] }) {
  const lista = avaliacoes.slice(0, 5);

  return (
    <View style={styles.container}>
      <Text style={styles.titulo}>Avaliações da loja</Text>

      {media?.quantidade > 0 ? (
        <View style={styles.resumo}>
          <StarRating nota={media.media} size={18} showValue />
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
            <StarRating nota={av.nota} size={14} />
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

const styles = StyleSheet.create({
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
