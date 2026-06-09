import { Alert, Text } from 'react-native';
import { useState } from 'react';
import { registrarCliente } from '../../services/clienteService';
import { login as authLogin } from '../../services/authService';
import { formatApiError } from '../../utils/apiErrorUtils';
import { obterLocalizacaoAtual } from '../../services/locationService';
import { isEmailValido } from '../../utils/validacaoUtils';
import { useAuth } from '../../context/AuthContext';
import {
  FormScreen,
  FormField,
  PrimaryButton,
  formStyles,
} from '../../components/form';

export default function RegisterScreen({ navigation }) {
  const [nomeUsuario, setNomeUsuario] = useState('');
  const [email, setEmail] = useState('');
  const [telefone, setTelefone] = useState('');
  const [senha, setSenha] = useState('');
  const [confirmarSenha, setConfirmarSenha] = useState('');
  const [loading, setLoading] = useState(false);
  const { salvarSessao } = useAuth();

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

    if (senha !== confirmarSenha) {
      Alert.alert('Erro', 'As senhas não coincidem');
      return;
    }

    setLoading(true);
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

      await registrarCliente({
        nomeUsuario: nomeUsuario.trim(),
        email: email.trim(),
        senha,
        telefone: telefone.trim() || null,
        latitudeAtual,
        longitudeAtual,
      });

      const { tipo, perfil } = await authLogin(email.trim(), senha);
      await salvarSessao({ tipo: tipo || 'cliente', perfil }, 'user');
      navigation.replace('Home');
      Alert.alert(
        'Sucesso',
        'Conta criada! Enviamos um e-mail de confirmação — verifique sua caixa de entrada. ' +
          'Você pode abrir uma loja a qualquer momento informando um CNPJ válido.'
      );
    } catch (error) {
      Alert.alert('Erro', formatApiError(error));
    } finally {
      setLoading(false);
    }
  }

  return (
    <FormScreen
      title="Cadastro"
      subtitle="Crie sua conta no Preço Certo"
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
