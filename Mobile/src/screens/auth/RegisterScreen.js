import { Alert, Text } from 'react-native';
import { useState } from 'react';
import { registrarCliente } from '../../services/clienteService';
import { login as authLogin } from '../../services/authService';
import { saveToken } from '../../services/tokenStorage';
import { formatApiError } from '../../utils/apiErrorUtils';
import { obterLocalizacaoAtual } from '../../services/locationService';
import { isEmailValido, isTelefoneValido } from '../../utils/validacaoUtils';
import { useAuth } from '../../context/AuthContext';
import {
  FormScreen,
  FormField,
  PrimaryButton,
  formStyles,
} from '../../components/form';

const LOGIN_RETRY_DELAYS_MS = [300, 600, 1200];

function sleep(ms) {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

async function loginComRetry(email, senha) {
  let lastError;
  for (let i = 0; i <= LOGIN_RETRY_DELAYS_MS.length; i++) {
    try {
      return await authLogin(email, senha);
    } catch (error) {
      lastError = error;
      if (i < LOGIN_RETRY_DELAYS_MS.length) {
        await sleep(LOGIN_RETRY_DELAYS_MS[i]);
      }
    }
  }
  throw lastError;
}

export default function RegisterScreen({ navigation }) {
  const [nomeUsuario, setNomeUsuario] = useState('');
  const [email, setEmail] = useState('');
  const [telefone, setTelefone] = useState('');
  const [senha, setSenha] = useState('');
  const [confirmarSenha, setConfirmarSenha] = useState('');
  const [loading, setLoading] = useState(false);
  const { salvarSessao, sincronizarGpsCliente } = useAuth();

  async function concluirSessao(tipo, perfil) {
    await salvarSessao({ tipo: tipo || 'cliente', perfil }, 'user');
    navigation.replace('Home');

    if (perfil?.id) {
      sincronizarGpsCliente(perfil.id).catch(() => {});
    }
  }

  async function handleRegister() {
    if (!nomeUsuario || !email || !senha || !confirmarSenha) {
      Alert.alert('Erro', 'Preencha todos os campos obrigatórios');
      return;
    }

    if (!isEmailValido(email)) {
      Alert.alert('Erro', 'Informe um e-mail válido.');
      return;
    }

    if (senha.length < 6) {
      Alert.alert('Erro', 'A senha deve ter pelo menos 6 caracteres.');
      return;
    }

    if (telefone.trim() && !isTelefoneValido(telefone)) {
      Alert.alert(
        'Erro',
        'Telefone inválido. Informe 8 dígitos (fixo) ou 9 dígitos (celular), com DDD opcional.'
      );
      return;
    }

    if (senha !== confirmarSenha) {
      Alert.alert('Erro', 'As senhas não coincidem');
      return;
    }

    setLoading(true);
    const emailNormalizado = email.trim().toLowerCase();
    let registroOk = false;

    try {
      let latitudeAtual = null;
      let longitudeAtual = null;

      try {
        const coords = await obterLocalizacaoAtual();
        latitudeAtual = coords.latitude;
        longitudeAtual = coords.longitude;
      } catch {
        // GPS opcional no cadastro
      }

      const resposta = await registrarCliente({
        nomeUsuario: nomeUsuario.trim(),
        email: emailNormalizado,
        senha,
        telefone: telefone.trim() || null,
        latitudeAtual,
        longitudeAtual,
      });

      registroOk = true;

      let token = resposta?.token;
      let tipo = resposta?.tipo;
      let perfil = resposta?.perfil;

      if (!token) {
        const loginData = await loginComRetry(emailNormalizado, senha);
        token = loginData.token;
        tipo = loginData.tipo;
        perfil = loginData.perfil;
      } else {
        await saveToken(token);
      }

      await concluirSessao(tipo, perfil);
    } catch (error) {
      if (error.response?.status === 409) {
        Alert.alert(
          'E-mail já cadastrado',
          'Este e-mail já tem conta. Faça login ou use outro e-mail.',
          [
            { text: 'Ir para login', onPress: () => navigation.navigate('Login') },
            { text: 'OK', style: 'cancel' },
          ]
        );
        return;
      }

      if (registroOk) {
        Alert.alert(
          'Conta criada',
          'Sua conta foi criada. Entre com seu e-mail e senha.',
          [{ text: 'Ir para login', onPress: () => navigation.navigate('Login') }]
        );
        return;
      }

      Alert.alert('Erro', formatApiError(error));
    } finally {
      setLoading(false);
    }
  }

  return (
    <FormScreen
      title="Cadastro"
      subtitle="Crie sua conta no Preço Certo"
      webVariant="auth"
      onBack={() => navigation.goBack()}
      footer={
        <PrimaryButton label="Cadastrar" onPress={handleRegister} loading={loading} />
      }
    >
      <Text style={formStyles.sectionHint}>
        Crie sua conta de usuário. Para se tornar lojista, abra uma loja com um CNPJ válido depois.
      </Text>

      <FormField label="Nome de usuário *" value={nomeUsuario} onChangeText={setNomeUsuario} />
      <FormField
        label="E-mail *"
        value={email}
        onChangeText={setEmail}
        autoCapitalize="none"
        keyboardType="email-address"
      />
      <FormField
        label="Telefone"
        value={telefone}
        onChangeText={setTelefone}
        keyboardType="phone-pad"
        placeholder="opcional"
      />

      <FormField label="Senha *" value={senha} onChangeText={setSenha} secureTextEntry />
      <FormField
        label="Confirmar senha *"
        value={confirmarSenha}
        onChangeText={setConfirmarSenha}
        secureTextEntry
      />
    </FormScreen>
  );
}
