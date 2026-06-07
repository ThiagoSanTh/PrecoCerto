import { FlatList, Alert, ActivityIndicator } from 'react-native';
import { useCallback, useState } from 'react';
import { useFocusEffect } from '@react-navigation/native';
import { listarOfertas } from '../services/ofertaService';
import { obterLoja } from '../services/lojaService';
import { useAuth } from '../context/AuthContext';
import {
  FormScreen,
  PrimaryButton,
  ListCard,
  ListCardText,
  formStyles,
} from '../components/form';
import { colors } from '../style';

export default function StoreScreen({ navigation }) {
  const { session } = useAuth();
  const lojaId = session?.perfil?.lojaId;
  const [loja, setLoja] = useState(null);
  const [ofertas, setOfertas] = useState([]);
  const [loading, setLoading] = useState(true);

  useFocusEffect(
    useCallback(() => {
      carregar();
    }, [lojaId])
  );

  async function carregar() {
    if (!lojaId) {
      setLoading(false);
      return;
    }

    setLoading(true);
    try {
      const [lojaData, todasOfertas] = await Promise.all([
        obterLoja(lojaId),
        listarOfertas(),
      ]);
      setLoja(lojaData);
      setOfertas(todasOfertas.filter((o) => o.lojaId === lojaId));
    } catch {
      Alert.alert('Erro', 'Não foi possível carregar dados da loja');
    } finally {
      setLoading(false);
    }
  }

  if (loading) {
    return (
      <FormScreen title="Minha loja" scrollable={false}>
        <ActivityIndicator color={colors.primary} style={{ marginTop: 24 }} />
      </FormScreen>
    );
  }

  if (!lojaId) {
    return (
      <FormScreen
        title="Minha loja"
        subtitle="Vincule uma loja ao seu perfil"
        scrollable={false}
        footer={
          <PrimaryButton label="Criar loja" onPress={() => navigation.navigate('CreateStore')} />
        }
      >
        <ListCard title="Nenhuma loja vinculada">
          <ListCardText>Cadastre sua loja para começar a publicar ofertas.</ListCardText>
        </ListCard>
      </FormScreen>
    );
  }

  return (
    <FormScreen title="Minha loja" subtitle={loja?.nomeFantasia} scrollable={false}>
      {loja ? (
        <ListCard title={loja.nomeFantasia}>
          <ListCardText>{loja.email}</ListCardText>
          <ListCardText>{loja.telefone}</ListCardText>
        </ListCard>
      ) : null}

      <PrimaryButton
        label="+ Nova oferta"
        onPress={() => navigation.navigate('CreateOferta')}
        style={{ marginBottom: 12 }}
      />

      <FlatList
        style={formStyles.listFlex}
        data={ofertas}
        keyExtractor={(item) => item.id}
        showsVerticalScrollIndicator={false}
        ListHeaderComponent={
          <ListCardText style={{ marginBottom: 8, fontWeight: '600' }}>
            Ofertas ({ofertas.length})
          </ListCardText>
        }
        renderItem={({ item }) => (
          <ListCard title={item.nomeProduto}>
            <ListCardText>R$ {Number(item.preco).toFixed(2)}</ListCardText>
            <ListCardText>{item.disponivel ? 'Disponível' : 'Indisponível'}</ListCardText>
          </ListCard>
        )}
        ListEmptyComponent={
          <ListCardText>Cadastre produtos e crie ofertas.</ListCardText>
        }
      />
    </FormScreen>
  );
}
