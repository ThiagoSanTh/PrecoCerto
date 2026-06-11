import { Text } from 'react-native';
import { useState } from 'react';
import { useAuth } from '../../context/AuthContext';
import { alterarSenha } from '../../services/clienteService';
import { alterarSenhaLojista } from '../../services/lojistaService';
import { mapApiError } from '../../utils/apiErrorUtils';
import { useFormStyles } from '../../hooks/useFormStyles';
import {
  FormScreen,
  FormField,
  PrimaryButton,
} from '../../components/form';

export default function ChangePasswordScreen({ navigation }) {
  const { session, isLojista } = useAuth();
  const [senhaAtual, setSenhaAtual] = useState('');
  const [novaSenha, setNovaSenha] = useState('');
  const [confirmarSenha, setConfirmarSenha] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);
  const formStyles = useFormStyles();

  async function handleAlterarSenha() {
    if (!senhaAtual || !novaSenha || !confirmarSenha) {
      setError({ title: 'Campos obrigatórios', message: 'Preencha todos os campos.' });
      return;
    }
    if (novaSenha.length < 6) {
      setError({ title: 'Senha curta', message: 'A nova senha deve ter pelo menos 6 caracteres.' });
      return;
    }
    if (novaSenha !== confirmarSenha) {
      setError({ title: 'Confirmação', message: 'A confirmação não coincide com a nova senha.' });
      return;
    }
    if (!session?.perfil?.id) {
      setError({ title: 'Sessão', message: 'Sessão inválida. Faça login novamente.' });
      return;
    }

    setError(null);
    setLoading(true);
    try {
      if (isLojista) {
        await alterarSenhaLojista(session.perfil.id, senhaAtual, novaSenha);
      } else {
        await alterarSenha(session.perfil.id, senhaAtual, novaSenha);
      }
      navigation.goBack();
    } catch (err) {
      setError(mapApiError(err));
    } finally {
      setLoading(false);
    }
  }

  return (
    <FormScreen
      title="Alterar senha"
      subtitle="Atualize sua senha de acesso"
      onBack={() => navigation.goBack()}
      error={error}
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
