import { View, Pressable, StyleSheet, ActivityIndicator } from 'react-native';
import { useMemo } from 'react';
import { Ionicons } from '@expo/vector-icons';
import { useTheme } from '../../context/ThemeContext';
import { shadows } from '../../theme';

export default function ProductActionBar({
  ehFavorito,
  onFavorito,
  favoritoLoading,
  mostrarFavorito,
  onChat,
  onCompartilhar,
}) {
  const { colors } = useTheme();

  const styles = useMemo(
    () =>
      StyleSheet.create({
        bar: {
          flexDirection: 'row',
          alignItems: 'center',
          justifyContent: 'flex-end',
          gap: 10,
        },
        btn: {
          width: 44,
          height: 44,
          borderRadius: 22,
          backgroundColor: colors.card,
          borderWidth: 1,
          borderColor: colors.border,
          alignItems: 'center',
          justifyContent: 'center',
          ...shadows.soft,
        },
        btnFavorito: {
          backgroundColor: colors.destructiveSoft,
          borderColor: colors.destructive,
        },
        btnPressed: {
          opacity: 0.75,
          transform: [{ scale: 0.96 }],
        },
      }),
    [colors]
  );

  return (
    <View style={styles.bar}>
      {mostrarFavorito ? (
        <Pressable
          style={({ pressed }) => [
            styles.btn,
            ehFavorito && styles.btnFavorito,
            pressed && styles.btnPressed,
          ]}
          onPress={onFavorito}
          disabled={favoritoLoading}
          accessibilityRole="button"
          accessibilityLabel={ehFavorito ? 'Remover dos favoritos' : 'Adicionar aos favoritos'}
        >
          {favoritoLoading ? (
            <ActivityIndicator size="small" color={colors.destructive} />
          ) : (
            <Ionicons
              name={ehFavorito ? 'heart' : 'heart-outline'}
              size={22}
              color={ehFavorito ? colors.destructive : colors.textMuted}
            />
          )}
        </Pressable>
      ) : null}

      <Pressable
        style={({ pressed }) => [styles.btn, pressed && styles.btnPressed]}
        onPress={onChat}
        accessibilityRole="button"
        accessibilityLabel="Falar com a loja"
      >
        <Ionicons name="chatbubble-outline" size={22} color={colors.text} />
      </Pressable>

      <Pressable
        style={({ pressed }) => [styles.btn, pressed && styles.btnPressed]}
        onPress={onCompartilhar}
        accessibilityRole="button"
        accessibilityLabel="Compartilhar produto"
      >
        <Ionicons name="share-outline" size={22} color={colors.text} />
      </Pressable>
    </View>
  );
}
