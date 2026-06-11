import { View, Pressable, StyleSheet, ActivityIndicator } from 'react-native';
import { useMemo } from 'react';
import { Ionicons } from '@expo/vector-icons';
import { useTheme } from '../../context/ThemeContext';

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
          backgroundColor: colors.surface,
          borderWidth: 1,
          borderColor: colors.border,
          alignItems: 'center',
          justifyContent: 'center',
          shadowColor: '#000',
          shadowOffset: { width: 0, height: 1 },
          shadowOpacity: 0.06,
          shadowRadius: 4,
          elevation: 2,
        },
        btnFavorito: {
          backgroundColor: '#FEF2F2',
          borderColor: '#FECACA',
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
          accessibilityLabel={ehFavorito ? 'Remover dos favoritos' : 'Adicionar aos favoritos'}
        >
          {favoritoLoading ? (
            <ActivityIndicator size="small" color="#EF4444" />
          ) : (
            <Ionicons
              name={ehFavorito ? 'heart' : 'heart-outline'}
              size={22}
              color={ehFavorito ? '#EF4444' : colors.textMuted}
            />
          )}
        </Pressable>
      ) : null}

      <Pressable
        style={({ pressed }) => [styles.btn, pressed && styles.btnPressed]}
        onPress={onChat}
        accessibilityLabel="Falar com a loja"
      >
        <Ionicons name="chatbubble-outline" size={22} color={colors.text} />
      </Pressable>

      <Pressable
        style={({ pressed }) => [styles.btn, pressed && styles.btnPressed]}
        onPress={onCompartilhar}
        accessibilityLabel="Compartilhar produto"
      >
        <Ionicons name="share-outline" size={22} color={colors.text} />
      </Pressable>
    </View>
  );
}
