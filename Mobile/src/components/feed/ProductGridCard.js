import { View, Text, Image, Pressable, StyleSheet } from 'react-native';
import { useMemo } from 'react';
import { Ionicons } from '@expo/vector-icons';
import { useTheme } from '../../context/ThemeContext';
import { formatarPrecoMl } from '../../utils/precoUtils';
import { formatarPrecoBrl } from '../../utils/mapaUtils';
import { nomeProduto } from '../../utils/produtoUtils';

export default function ProductGridCard({
  produto,
  oferta,
  onPress,
  fillCell = true,
  ehFavorito = false,
  mostrarFavorito = false,
  onFavorito,
  favoritoLoading = false,
}) {
  const { colors } = useTheme();

  const styles = useMemo(
    () =>
      StyleSheet.create({
        card: {
          ...(fillCell ? { flex: 1 } : { width: '100%' }),
          backgroundColor: colors.surface,
          borderRadius: 8,
          overflow: 'hidden',
          marginBottom: 8,
          elevation: 2,
          shadowColor: '#000',
          shadowOffset: { width: 0, height: 1 },
          shadowOpacity: 0.08,
          shadowRadius: 3,
        },
        imageWrap: {
          aspectRatio: 1,
          backgroundColor: colors.surface,
          position: 'relative',
        },
        image: {
          width: '100%',
          height: '100%',
        },
        imagePlaceholder: {
          flex: 1,
          backgroundColor: colors.card,
          alignItems: 'center',
          justifyContent: 'center',
        },
        placeholderLetter: {
          fontSize: 32,
          fontWeight: '700',
          color: colors.textMuted,
        },
        promoBadge: {
          position: 'absolute',
          top: 8,
          left: 8,
          backgroundColor: '#15803D',
          paddingHorizontal: 6,
          paddingVertical: 2,
          borderRadius: 4,
        },
        promoText: {
          color: '#fff',
          fontSize: 10,
          fontWeight: '700',
        },
        heartBtn: {
          position: 'absolute',
          top: 8,
          right: 8,
          width: 34,
          height: 34,
          borderRadius: 17,
          backgroundColor: 'rgba(255,255,255,0.92)',
          alignItems: 'center',
          justifyContent: 'center',
          elevation: 3,
          shadowColor: '#000',
          shadowOffset: { width: 0, height: 1 },
          shadowOpacity: 0.12,
          shadowRadius: 3,
        },
        heartBtnAtivo: {
          backgroundColor: '#FEF2F2',
        },
        body: {
          padding: 10,
        },
        title: {
          fontSize: 13,
          color: colors.text,
          lineHeight: 17,
          marginBottom: 6,
        },
        precoAnterior: {
          fontSize: 11,
          color: colors.textMuted,
          textDecorationLine: 'line-through',
          marginBottom: 2,
        },
        precoRow: {
          flexDirection: 'row',
          alignItems: 'flex-start',
        },
        moeda: {
          fontSize: 12,
          color: colors.text,
          marginTop: 4,
        },
        inteiro: {
          fontSize: 22,
          fontWeight: '300',
          color: colors.text,
          lineHeight: 26,
        },
        centavos: {
          fontSize: 11,
          color: colors.text,
          marginTop: 2,
        },
        loja: {
          fontSize: 11,
          color: colors.textMuted,
          marginTop: 4,
        },
      }),
    [colors, fillCell]
  );

  const precoAtual = oferta?.preco ?? produto.preco;
  const precoAnterior = oferta?.precoAnterior;
  const emPromocao =
    oferta?.emPromocao ||
    (precoAnterior && Number(precoAnterior) > Number(precoAtual));
  const { inteiro, centavos } = formatarPrecoMl(precoAtual);
  const titulo = nomeProduto(produto);
  const loja = produto.lojaNomeFantasia || oferta?.nomeLoja;

  return (
    <Pressable style={styles.card} onPress={onPress}>
      <View style={styles.imageWrap}>
        {produto.imagemUrl ? (
          <Image
            source={{ uri: produto.imagemUrl }}
            style={styles.image}
            resizeMode="cover"
          />
        ) : (
          <View style={styles.imagePlaceholder}>
            <Text style={styles.placeholderLetter}>
              {titulo.charAt(0).toUpperCase()}
            </Text>
          </View>
        )}
        {emPromocao ? (
          <View style={styles.promoBadge}>
            <Text style={styles.promoText}>Oferta</Text>
          </View>
        ) : null}
        {mostrarFavorito ? (
          <Pressable
            style={[styles.heartBtn, ehFavorito && styles.heartBtnAtivo]}
            onPress={() => {
              onFavorito?.();
            }}
            disabled={favoritoLoading || !onFavorito}
            hitSlop={8}
            accessibilityLabel={ehFavorito ? 'Remover dos favoritos' : 'Adicionar aos favoritos'}
          >
            <Ionicons
              name={ehFavorito ? 'heart' : 'heart-outline'}
              size={18}
              color={ehFavorito ? '#EF4444' : colors.textMuted}
            />
          </Pressable>
        ) : null}
      </View>

      <View style={styles.body}>
        <Text style={styles.title} numberOfLines={2}>
          {titulo}
        </Text>

        {emPromocao && precoAnterior ? (
          <Text style={styles.precoAnterior}>{formatarPrecoBrl(precoAnterior)}</Text>
        ) : null}

        <View style={styles.precoRow}>
          <Text style={styles.moeda}>R$ </Text>
          <Text style={styles.inteiro}>{inteiro}</Text>
          <Text style={styles.centavos}>{centavos}</Text>
        </View>

        {loja ? (
          <Text style={styles.loja} numberOfLines={1}>
            {loja}
          </Text>
        ) : null}
      </View>
    </Pressable>
  );
}
