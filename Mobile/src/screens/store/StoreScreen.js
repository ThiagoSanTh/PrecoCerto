import { FlatList, Alert, ActivityIndicator } from 'react-native';
import { useCallback, useState } from 'react';
import { useFocusEffect } from '@react-navigation/native';
import { listarOfertas } from '../../services/ofertaService';
import { obterLoja } from '../../services/lojaService';
import { useAuth } from '../../context/AuthContext';
import { podeCriarLoja } from '../../utils/modoUsuario';
import {
  FormScreen,
  PrimaryButton,
  ListCard,
  ListCardText,
  formStyles,
} from '../../components/form';
import { colors } from '../../theme';

export default function StoreScreen({ navigation }) {
  const { session, isLojista } = useAuth();
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
      const [lojaData, ofertasRes] = await Promise.all([
        obterLoja(lojaId),
        listarOfertas(1, 100),
      ]);
      setLoja(lojaData);
      setOfertas(ofertasRes.items.filter((o) => String(o.lojaId) === String(lojaId)));
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
    const podeCriar = podeCriarLoja(session);
    return (
      <FormScreen
        title="Minha loja"
        subtitle={podeCriar ? 'Vincule uma loja ao seu perfil' : 'Loja vinculada ao perfil'}
        scrollable={false}
        footer={
          podeCriar ? (
            <PrimaryButton label="Criar loja" onPress={() => navigation.navigate('CreateStore')} />
          ) : undefined
        }
      >
        <ListCard title={podeCriar ? 'Nenhuma loja vinculada' : 'Loja em carregamento'}>
          <ListCardText>
            {podeCriar
              ? 'Cadastre sua loja para começar a publicar ofertas.'
              : 'Sua conta já está vinculada a uma loja. Atualize a tela ou entre novamente se os dados não aparecerem.'}
          </ListCardText>
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

      {isLojista ? (
        <PrimaryButton
          label="Gerenciar vendedores"
          onPress={() => navigation.navigate('Vendedores', { lojaId })}
          style={{ marginBottom: 12 }}
        />
      ) : null}

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
