import { View, Text, Image, Pressable, StyleSheet } from 'react-native';
import { useMemo } from 'react';
import { useTheme } from '../../context/ThemeContext';
import { formatarPrecoBrl } from '../../utils/mapaUtils';
import { nomeProduto } from '../../utils/produtoUtils';

export default function FavoritoListCard({ produto, oferta, onPress }) {
  const { colors } = useTheme();

  const styles = useMemo(
    () =>
      StyleSheet.create({
        card: {
          flexDirection: 'row',
          alignItems: 'center',
          backgroundColor: colors.surface,
          borderRadius: 8,
          padding: 10,
          marginBottom: 8,
          gap: 10,
          shadowColor: '#000',
          shadowOffset: { width: 0, height: 1 },
          shadowOpacity: 0.08,
          shadowRadius: 3,
          elevation: 2,
        },
        thumbWrap: {
          width: 72,
          height: 72,
          borderRadius: 8,
          overflow: 'hidden',
          backgroundColor: colors.card,
        },
        thumb: {
          width: '100%',
          height: '100%',
        },
        thumbPlaceholder: {
          flex: 1,
          alignItems: 'center',
          justifyContent: 'center',
        },
        placeholderLetter: {
          fontSize: 24,
          fontWeight: '700',
          color: colors.textMuted,
        },
        info: {
          flex: 1,
          minWidth: 0,
        },
        title: {
          fontSize: 14,
          fontWeight: '600',
          color: colors.text,
          lineHeight: 18,
        },
        loja: {
          fontSize: 12,
          color: colors.textMuted,
          marginTop: 4,
        },
        right: {
          alignItems: 'flex-end',
          maxWidth: 100,
        },
        promoBadge: {
          backgroundColor: '#DCFCE7',
          paddingHorizontal: 8,
          paddingVertical: 3,
          borderRadius: 4,
          marginBottom: 6,
        },
        promoText: {
          color: '#15803D',
          fontSize: 10,
          fontWeight: '700',
        },
        semPromo: {
          fontSize: 10,
          color: colors.textMuted,
          marginBottom: 6,
          textAlign: 'right',
        },
        preco: {
          fontSize: 15,
          fontWeight: '700',
          color: colors.text,
          textAlign: 'right',
        },
      }),
    [colors]
  );

  if (!produto) return null;

  const precoAtual = oferta?.preco ?? produto.preco;
  const emPromocao =
    Boolean(oferta?.emPromocao) ||
    (oferta?.precoAnterior && Number(oferta.precoAnterior) > Number(precoAtual));
  const titulo = nomeProduto(produto);
  const loja = produto.lojaNomeFantasia || oferta?.nomeLoja;

  return (
    <Pressable style={styles.card} onPress={onPress}>
      <View style={styles.thumbWrap}>
        {produto.imagemUrl ? (
          <Image source={{ uri: produto.imagemUrl }} style={styles.thumb} resizeMode="cover" />
        ) : (
          <View style={styles.thumbPlaceholder}>
            <Text style={styles.placeholderLetter}>{titulo.charAt(0).toUpperCase()}</Text>
          </View>
        )}
      </View>

      <View style={styles.info}>
        <Text style={styles.title} numberOfLines={2}>
          {titulo}
        </Text>
        {loja ? (
          <Text style={styles.loja} numberOfLines={1}>
            {loja}
          </Text>
        ) : null}
      </View>

      <View style={styles.right}>
        {emPromocao ? (
          <View style={styles.promoBadge}>
            <Text style={styles.promoText}>Promoção</Text>
          </View>
        ) : (
          <Text style={styles.semPromo}>Sem promoção</Text>
        )}
        <Text style={styles.preco}>{formatarPrecoBrl(precoAtual)}</Text>
      </View>
    </Pressable>
  );
}
