import { FlatList, Alert, ActivityIndicator } from 'react-native';
import { useCallback, useState } from 'react';
import { useFocusEffect } from '@react-navigation/native';
import { listarHistoricoCliente, limparHistoricoCliente } from '../services/historicoService';
import { useAuth } from '../context/AuthContext';
import {
  FormScreen,
  SecondaryButton,
  ListCard,
  ListCardText,
  formStyles,
} from '../components/form';
import { colors } from '../style';

export default function HistoricoScreen() {
  const [historico, setHistorico] = useState([]);
  const [loading, setLoading] = useState(true);
  const { session } = useAuth();
  const clienteId = session?.perfil?.id;

  useFocusEffect(
    useCallback(() => {
      carregar();
    }, [clienteId])
  );

  async function carregar() {
    if (!clienteId) return;
    setLoading(true);
    try {
      const data = await listarHistoricoCliente(clienteId);
      setHistorico(data);
    } catch {
      Alert.alert('Erro', 'Não foi possível carregar histórico');
    } finally {
      setLoading(false);
    }
  }

  async function handleLimpar() {
    Alert.alert('Limpar histórico', 'Deseja apagar todas as buscas?', [
      { text: 'Cancelar', style: 'cancel' },
      {
        text: 'Limpar',
        style: 'destructive',
        onPress: async () => {
          try {
            await limparHistoricoCliente(clienteId);
            setHistorico([]);
          } catch {
            Alert.alert('Erro', 'Não foi possível limpar');
          }
        },
      },
    ]);
  }

  if (!clienteId) {
    return (
      <FormScreen title="Histórico" subtitle="Faça login como cliente">
        <ListCardText>Nenhuma sessão de cliente ativa.</ListCardText>
      </FormScreen>
    );
  }

  return (
    <FormScreen
      title="Histórico de buscas"
      scrollable={false}
      footer={
        historico.length > 0 ? (
          <SecondaryButton label="Limpar histórico" onPress={handleLimpar} />
        ) : null
      }
    >
      {loading ? (
        <ActivityIndicator color={colors.primary} style={{ marginTop: 24 }} />
      ) : (
        <FlatList
          style={formStyles.listFlex}
          data={historico}
          keyExtractor={(item) => item.id}
          showsVerticalScrollIndicator={false}
          renderItem={({ item }) => (
            <ListCard title={item.termoPesquisa}>
              <ListCardText>
                {new Date(item.dataPesquisa).toLocaleString('pt-BR')}
              </ListCardText>
            </ListCard>
          )}
          ListEmptyComponent={<ListCardText>Nenhuma busca registrada.</ListCardText>}
        />
      )}
    </FormScreen>
  );
}
