import { View, Text, Image, Pressable, StyleSheet } from 'react-native';
import { useMemo } from 'react';
import { Ionicons } from '@expo/vector-icons';
import { useTheme } from '../../context/ThemeContext';
import { PillBadge } from '../ui/DesignPrimitives';
import { formatarPrecoMl } from '../../utils/precoUtils';
import { formatarPrecoBrl } from '../../utils/mapaUtils';
import { nomeProduto } from '../../utils/produtoUtils';
import { fonts, radii, shadows, tabularNums } from '../../theme';

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
          backgroundColor: colors.card,
          borderRadius: radii.xl,
          borderWidth: 1,
          borderColor: colors.border,
          overflow: 'hidden',
          marginBottom: 10,
          ...shadows.soft,
        },
        imageWrap: {
          aspectRatio: 1,
          backgroundColor: colors.surfaceSubtle,
          position: 'relative',
        },
        image: {
          width: '100%',
          height: '100%',
        },
        imagePlaceholder: {
          flex: 1,
          backgroundColor: colors.surfaceSubtle,
          alignItems: 'center',
          justifyContent: 'center',
        },
        placeholderLetter: {
          fontFamily: fonts.display,
          fontSize: 32,
          fontWeight: '700',
          color: colors.textMuted,
        },
        promoBadge: {
          position: 'absolute',
          top: 8,
          left: 8,
        },
        heartBtn: {
          position: 'absolute',
          top: 8,
          right: 8,
          width: 34,
          height: 34,
          borderRadius: 17,
          backgroundColor: colors.card,
          borderWidth: 1,
          borderColor: colors.border,
          alignItems: 'center',
          justifyContent: 'center',
          ...shadows.soft,
        },
        heartBtnAtivo: {
          backgroundColor: colors.destructiveSoft,
          borderColor: colors.destructive,
        },
        body: {
          padding: 12,
        },
        title: {
          fontFamily: fonts.sansSemiBold,
          fontSize: 13.5,
          fontWeight: '600',
          color: colors.text,
          lineHeight: 18,
          marginBottom: 6,
        },
        precoAnterior: {
          fontFamily: fonts.sans,
          fontSize: 11,
          color: colors.textMuted,
          textDecorationLine: 'line-through',
          marginBottom: 2,
          ...tabularNums,
        },
        precoRow: {
          flexDirection: 'row',
          alignItems: 'flex-start',
        },
        moeda: {
          fontFamily: fonts.sansSemiBold,
          fontSize: 12,
          color: colors.text,
          marginTop: 4,
          ...tabularNums,
        },
        inteiro: {
          fontFamily: fonts.display,
          fontSize: 21,
          fontWeight: '700',
          color: colors.text,
          lineHeight: 26,
          ...tabularNums,
        },
        centavos: {
          fontFamily: fonts.display,
          fontSize: 11,
          fontWeight: '600',
          color: colors.text,
          marginTop: 3,
          ...tabularNums,
        },
        loja: {
          fontFamily: fonts.sans,
          fontSize: 11.5,
          color: colors.textMuted,
          marginTop: 5,
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
            <PillBadge label="OFERTA" variant="highlight" />
          </View>
        ) : null}
        {mostrarFavorito ? (
          <Pressable
            style={[styles.heartBtn, ehFavorito && styles.heartBtnAtivo]}
            onPress={(event) => {
              event?.stopPropagation?.();
              onFavorito?.();
            }}
            disabled={favoritoLoading || !onFavorito}
            hitSlop={8}
            accessibilityLabel={ehFavorito ? 'Remover dos favoritos' : 'Adicionar aos favoritos'}
          >
            <Ionicons
              name={ehFavorito ? 'heart' : 'heart-outline'}
              size={18}
              color={ehFavorito ? colors.destructive : colors.textMuted}
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
