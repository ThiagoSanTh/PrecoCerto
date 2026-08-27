import { StatusBar } from 'expo-status-bar';
import { View, Text, Pressable } from 'react-native';
import { useState } from 'react';
import { login as authLogin } from '../../services/authService';
import { mapApiError } from '../../utils/apiErrorUtils';
import { useAuth } from '../../context/AuthContext';
import { useFormStyles } from '../../hooks/useFormStyles';
import {
  FormScreen,
  FormField,
  PrimaryButton,
} from '../../components/form';

export default function LoginScreen({ navigation }) {
  const [email, setEmail] = useState('');
  const [senha, setSenha] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);
  const { salvarSessao, sincronizarGpsCliente } = useAuth();
  const formStyles = useFormStyles();

  async function handleLogin() {
    if (!email || !senha) {
      setError({ title: 'Campos obrigatórios', message: 'Preencha todos os campos.' });
      return;
    }

    setError(null);
    setLoading(true);
    try {
      const { tipo, perfil } = await authLogin(email.trim().toLowerCase(), senha);
      const ehLojaUser = tipo === 'lojista' || tipo === 'vendedor';
      const modo = ehLojaUser ? 'store' : 'user';
      await salvarSessao({ tipo, perfil }, modo);
      navigation.replace('Home');

      if (perfil?.id) {
        sincronizarGpsCliente(perfil.id, { force: true }).catch(() => {});
      }
    } catch (err) {
      setError(mapApiError(err));
    } finally {
      setLoading(false);
    }
  }

  return (
    <FormScreen
      title="Preço Certo"
      subtitle="Entre com sua conta"
      webVariant="auth"
      error={error}
      footer={<PrimaryButton label="Entrar" onPress={handleLogin} loading={loading} />}
    >
      <Text style={formStyles.sectionHint}>
        Após o login, sua localização é obtida automaticamente pelo GPS.
      </Text>

      <FormField
        label="E-mail"
        value={email}
        onChangeText={(v) => {
          setEmail(v);
          if (error) setError(null);
        }}
        autoCapitalize="none"
        keyboardType="email-address"
      />
      <FormField
        label="Senha"
        value={senha}
        onChangeText={(v) => {
          setSenha(v);
          if (error) setError(null);
        }}
        secureTextEntry
      />

      <View style={formStyles.row}>
        <Pressable
          onPress={() => navigation.navigate('EsqueciSenha')}
          style={{ flex: 1, paddingVertical: 8 }}
        >
          <Text style={formStyles.linkText}>Esqueci a senha</Text>
        </Pressable>
        <Pressable
          onPress={() => navigation.navigate('Cadastro')}
          style={{ flex: 1, paddingVertical: 8, alignItems: 'flex-end' }}
        >
          <Text style={formStyles.linkText}>Novo usuário</Text>
        </Pressable>
      </View>

      <StatusBar style="auto" />
    </FormScreen>
  );
}
