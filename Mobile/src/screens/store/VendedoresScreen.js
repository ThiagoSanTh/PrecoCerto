import { View, Text, FlatList, Alert, ActivityIndicator, StyleSheet } from 'react-native';
import { useCallback, useState } from 'react';
import { useFocusEffect } from '@react-navigation/native';
import {
  listarVendedores,
  promoverVendedor,
  removerVendedor,
} from '../../services/lojistaService';
import { isEmailValido } from '../../utils/validacaoUtils';
import { formatApiError } from '../../utils/apiErrorUtils';
import {
  FormScreen,
  FormField,
  PrimaryButton,
  ListCardText,
} from '../../components/form';
import { colors } from '../../theme';

export default function VendedoresScreen({ navigation, route }) {
  const lojaId = route?.params?.lojaId;
  const [vendedores, setVendedores] = useState([]);
  const [loading, setLoading] = useState(true);
  const [email, setEmail] = useState('');
  const [cargo, setCargo] = useState('Vendedor');
  const [salvando, setSalvando] = useState(false);

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
      const data = await listarVendedores(lojaId);
      setVendedores(data);
    } catch {
      Alert.alert('Erro', 'Não foi possível carregar os vendedores.');
    } finally {
      setLoading(false);
    }
  }

  async function handlePromover() {
    if (!isEmailValido(email)) {
      Alert.alert('Atenção', 'Informe um e-mail válido do cliente a promover.');
      return;
    }
    setSalvando(true);
    try {
      await promoverVendedor(lojaId, { email: email.trim(), cargo: cargo.trim() || 'Vendedor' });
      setEmail('');
      Alert.alert('Sucesso', 'Cliente promovido a vendedor.');
      await carregar();
    } catch (error) {
      Alert.alert('Erro', formatApiError(error));
    } finally {
      setSalvando(false);
    }
  }

  function confirmarRemover(item) {
    Alert.alert('Remover vendedor', `Remover ${item.nomeUsuario} da equipe?`, [
      { text: 'Cancelar', style: 'cancel' },
      {
        text: 'Remover',
        style: 'destructive',
        onPress: async () => {
          try {
            await removerVendedor(lojaId, item.id);
            await carregar();
          } catch (error) {
            Alert.alert('Erro', formatApiError(error));
          }
        },
      },
    ]);
  }

  return (
    <FormScreen
      title="Vendedores"
      subtitle="Equipe de controle de estoque"
      onBack={() => navigation.goBack()}
      scrollable={false}
      footer={
        <>
          <FormField
            label="E-mail do cliente"
            value={email}
            onChangeText={setEmail}
            autoCapitalize="none"
            keyboardType="email-address"
            placeholder="cliente@exemplo.com"
          />
          <FormField label="Cargo" value={cargo} onChangeText={setCargo} />
          <PrimaryButton label="Promover a vendedor" onPress={handlePromover} loading={salvando} />
        </>
      }
    >
      {loading ? (
        <ActivityIndicator size="large" color={colors.primary} style={{ marginTop: 24 }} />
      ) : (
        <FlatList
          data={vendedores}
          keyExtractor={(item) => item.id}
          contentContainerStyle={{ paddingVertical: 8 }}
          renderItem={({ item }) => (
            <View style={styles.card}>
              <View style={{ flex: 1 }}>
                <Text style={styles.nome}>{item.nomeUsuario}</Text>
                <Text style={styles.email}>{item.email}</Text>
                {item.cargo ? <Text style={styles.cargo}>{item.cargo}</Text> : null}
              </View>
              <Text style={styles.remover} onPress={() => confirmarRemover(item)}>
                Remover
              </Text>
            </View>
          )}
          ListEmptyComponent={
            <ListCardText style={{ textAlign: 'center', marginTop: 24 }}>
              Nenhum vendedor cadastrado. Promova um cliente pelo e-mail.
            </ListCardText>
          }
        />
      )}
    </FormScreen>
  );
}

const styles = StyleSheet.create({
  card: {
    flexDirection: 'row',
    alignItems: 'center',
    backgroundColor: '#F8FAFC',
    borderWidth: 1,
    borderColor: '#E2E8F0',
    borderRadius: 10,
    padding: 12,
    marginBottom: 10,
  },
  nome: { fontSize: 15, fontWeight: '700', color: '#0F172A' },
  email: { fontSize: 13, color: '#64748B', marginTop: 2 },
  cargo: { fontSize: 12, color: colors.primaryDark, marginTop: 2 },
  remover: { color: '#EF4444', fontWeight: '600', fontSize: 13, paddingLeft: 12 },
});
