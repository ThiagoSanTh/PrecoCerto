import { Alert, Text } from 'react-native';
import { useState } from 'react';
import { useAuth } from '../../context/AuthContext';
import { alterarSenha } from '../../services/clienteService';
import { alterarSenhaLojista } from '../../services/lojistaService';
import { formatApiError } from '../../utils/apiErrorUtils';
import {
  FormScreen,
  FormField,
  PrimaryButton,
  formStyles,
} from '../../components/form';

export default function ChangePasswordScreen({ navigation }) {
  const { session, isLojista } = useAuth();
  const [senhaAtual, setSenhaAtual] = useState('');
  const [novaSenha, setNovaSenha] = useState('');
  const [confirmarSenha, setConfirmarSenha] = useState('');
  const [loading, setLoading] = useState(false);

  async function handleAlterarSenha() {
    if (!senhaAtual || !novaSenha || !confirmarSenha) {
      Alert.alert('Erro', 'Preencha todos os campos.');
      return;
    }
    if (novaSenha.length < 6) {
      Alert.alert('Erro', 'A nova senha deve ter pelo menos 6 caracteres.');
      return;
    }
    if (novaSenha !== confirmarSenha) {
      Alert.alert('Erro', 'A confirmação não coincide com a nova senha.');
      return;
    }
    if (!session?.perfil?.id) {
      Alert.alert('Erro', 'Sessão inválida. Faça login novamente.');
      return;
    }

    setLoading(true);
    try {
      if (isLojista) {
        await alterarSenhaLojista(session.perfil.id, senhaAtual, novaSenha);
      } else {
        await alterarSenha(session.perfil.id, senhaAtual, novaSenha);
      }
      Alert.alert('Sucesso', 'Senha alterada com sucesso.');
      navigation.goBack();
    } catch (error) {
      Alert.alert('Erro', formatApiError(error));
    } finally {
      setLoading(false);
    }
  }

  return (
    <FormScreen
      title="Alterar senha"
      subtitle="Atualize sua senha de acesso"
      onBack={() => navigation.goBack()}
      footer={
        <PrimaryButton label="Salvar nova senha" onPress={handleAlterarSenha} loading={loading} />
      }
    >
      <Text style={formStyles.sectionHint}>
        Use uma senha forte com pelo menos 6 caracteres.
      </Text>
      <FormField
        label="Senha atual"
        value={senhaAtual}
        onChangeText={setSenhaAtual}
        secureTextEntry
      />
      <FormField
        label="Nova senha"
        value={novaSenha}
        onChangeText={setNovaSenha}
        secureTextEntry
      />
      <FormField
        label="Confirmar nova senha"
        value={confirmarSenha}
        onChangeText={setConfirmarSenha}
        secureTextEntry
      />
    </FormScreen>
  );
}
