import { View, Text, Alert, FlatList, Pressable, ActivityIndicator, RefreshControl } from 'react-native';
import { useCallback, useState } from 'react';
import { useFocusEffect } from '@react-navigation/native';
import { listarConversas, listarConversasComStale } from '../../services/chatService';
import { FormScreen } from '../../components/form';
import { useTheme } from '../../context/ThemeContext';
import { useChatBadge } from '../../context/ChatBadgeContext';

export default function ConversasScreen({ navigation }) {
  const [conversas, setConversas] = useState([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const { colors } = useTheme();
  const { atualizarFromConversas } = useChatBadge();

  const carregar = useCallback(async (force = false) => {
    try {
      const result = force
        ? { data: await listarConversas({ force: true }) }
        : await listarConversasComStale();
      setConversas(result.data || []);
      atualizarFromConversas(result.data || []);
    } catch (error) {
      const status = error?.response?.status;
      if (status !== 429) {
        Alert.alert('Erro', 'Não foi possível carregar as conversas.');
      }
    }
  }, [atualizarFromConversas]);

  useFocusEffect(
    useCallback(() => {
      let ativo = true;
      async function init() {
        setLoading(true);
        await carregar(false);
        if (ativo) setLoading(false);
      }
      init();
      return () => {
        ativo = false;
      };
    }, [carregar])
  );

  async function onRefresh() {
    setRefreshing(true);
    await carregar(true);
    setRefreshing(false);
  }

  if (loading) {
    return (
      <View style={{ flex: 1, justifyContent: 'center', alignItems: 'center' }}>
        <ActivityIndicator color={colors.primary} />
      </View>
    );
  }

  return (
    <FormScreen title="Mensagens" subtitle="Conversas com lojas" scrollable={false}>
      <FlatList
        data={conversas}
        keyExtractor={(item) => item.codigoPublico}
        refreshControl={<RefreshControl refreshing={refreshing} onRefresh={onRefresh} />}
        windowSize={5}
        maxToRenderPerBatch={10}
        removeClippedSubviews
        ListEmptyComponent={
          <Text style={{ color: colors.textMuted, textAlign: 'center', marginTop: 24 }}>
            Nenhuma conversa ainda. Abra o chat em uma loja ou produto.
          </Text>
        }
        renderItem={({ item }) => (
          <Pressable
            onPress={() =>
              navigation.navigate('Chat', {
                conversaCodigo: item.codigoPublico,
                titulo: item.nomeContato || 'Conversa',
              })
            }
            style={{
              padding: 14,
              borderRadius: 10,
              borderWidth: 1,
              borderColor: colors.border,
              backgroundColor: colors.surface,
              marginBottom: 10,
            }}
          >
            <Text style={{ fontWeight: '700', color: colors.text }}>{item.nomeContato || 'Loja'}</Text>
            <Text style={{ color: colors.textMuted, marginTop: 4, fontSize: 12 }}>
              {item.ultimaMensagemEm
                ? new Date(item.ultimaMensagemEm).toLocaleString('pt-BR')
                : 'Sem mensagens'}
            </Text>
          </Pressable>
        )}
      />
    </FormScreen>
  );
}
