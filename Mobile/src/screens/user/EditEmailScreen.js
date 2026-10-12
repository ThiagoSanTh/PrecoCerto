import { Alert } from 'react-native';
import { useState, useRef } from 'react';
import { useAuth } from '../../context/AuthContext';
import { alterarEmailCliente } from '../../services/clienteService';
import { isEmailValido } from '../../utils/validacaoUtils';
import { formatApiError } from '../../utils/apiErrorUtils';
import { FormScreen, FormField, PrimaryButton } from '../../components/form';

export default function EditEmailScreen({ navigation }) {
  const { session, atualizarPerfilSessao } = useAuth();
  const [senhaAtual, setSenhaAtual] = useState('');
  const [novoEmail, setNovoEmail] = useState('');
  const [loading, setLoading] = useState(false);
  const novoEmailRef = useRef(null);

  async function handleSalvar() {
    if (loading) return;
    if (!senhaAtual || !novoEmail) {
      Alert.alert('Erro', 'Preencha todos os campos.');
      return;
    }
    if (!isEmailValido(novoEmail)) {
      Alert.alert('Erro', 'Informe um e-mail válido.');
      return;
    }

    setLoading(true);
    try {
      const atualizado = await alterarEmailCliente(
        session.perfil.id,
        senhaAtual,
        novoEmail.trim().toLowerCase()
      );
      await atualizarPerfilSessao(atualizado);
      Alert.alert('Sucesso', 'E-mail alterado com sucesso.', [
        { text: 'OK', onPress: () => navigation.goBack() },
      ]);
    } catch (error) {
      Alert.alert('Erro', formatApiError(error));
    } finally {
      setLoading(false);
    }
  }

  return (
    <FormScreen
      title="Alterar e-mail"
      subtitle="Confirme sua senha atual"
      onBack={() => navigation.goBack()}
      footer={<PrimaryButton label="Salvar novo e-mail" onPress={handleSalvar} loading={loading} />}
    >
      <FormField
        label="Senha atual"
        value={senhaAtual}
        onChangeText={setSenhaAtual}
        secureTextEntry
        returnKeyType="next"
        onSubmitEditing={() => novoEmailRef.current?.focus()}
      />
      <FormField
        ref={novoEmailRef}
        label="Novo e-mail"
        value={novoEmail}
        onChangeText={setNovoEmail}
        autoCapitalize="none"
        keyboardType="email-address"
        returnKeyType="done"
        onSubmitEditing={handleSalvar}
      />
    </FormScreen>
  );
}
