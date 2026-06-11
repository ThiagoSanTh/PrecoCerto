import { Alert } from 'react-native';
import { useState } from 'react';
import { solicitarRecuperacaoSenha, redefinirSenha } from '../../services/authService';
import { formatApiError } from '../../utils/apiErrorUtils';
import { isEmailValido } from '../../utils/validacaoUtils';
import { FormScreen, FormField, PrimaryButton, SecondaryButton } from '../../components/form';

export default function ForgotPasswordScreen({ navigation }) {
  const [email, setEmail] = useState('');
  const [token, setToken] = useState('');
  const [novaSenha, setNovaSenha] = useState('');
  const [modoToken, setModoToken] = useState(false);
  const [loading, setLoading] = useState(false);

  async function handleSolicitar() {
    if (!isEmailValido(email)) {
      Alert.alert('Erro', 'Informe um e-mail válido.');
      return;
    }
    setLoading(true);
    try {
      await solicitarRecuperacaoSenha(email);
      Alert.alert(
        'Verifique seu e-mail',
        'Se o e-mail estiver cadastrado, enviamos um token para redefinir a senha.',
        [{ text: 'OK', onPress: () => setModoToken(true) }]
      );
    } catch (error) {
      Alert.alert('Erro', formatApiError(error));
    } finally {
      setLoading(false);
    }
  }

  async function handleRedefinir() {
    if (!token || novaSenha.length < 6) {
      Alert.alert('Erro', 'Informe o token e uma senha com pelo menos 6 caracteres.');
      return;
    }
    setLoading(true);
    try {
      await redefinirSenha(token.trim(), novaSenha);
      Alert.alert('Sucesso', 'Senha redefinida. Faça login.', [
        { text: 'OK', onPress: () => navigation.replace('Login') },
      ]);
    } catch (error) {
      Alert.alert('Erro', formatApiError(error));
    } finally {
      setLoading(false);
    }
  }

  return (
    <FormScreen
      title="Recuperar senha"
      subtitle={modoToken ? 'Informe o token recebido por e-mail' : 'Informe o e-mail da conta'}
      onBack={() => navigation.goBack()}
      footer={
        modoToken ? (
          <PrimaryButton label="Redefinir senha" onPress={handleRedefinir} loading={loading} />
        ) : (
          <>
            <PrimaryButton label="Solicitar recuperação" onPress={handleSolicitar} loading={loading} />
            <SecondaryButton label="Já tenho o token" onPress={() => setModoToken(true)} />
          </>
        )
      }
    >
      {!modoToken ? (
        <FormField
          label="E-mail"
          value={email}
          onChangeText={setEmail}
          autoCapitalize="none"
          keyboardType="email-address"
        />
      ) : (
        <>
          <FormField label="Token" value={token} onChangeText={setToken} autoCapitalize="none" />
          <FormField label="Nova senha" value={novaSenha} onChangeText={setNovaSenha} secureTextEntry />
        </>
      )}
    </FormScreen>
  );
}
