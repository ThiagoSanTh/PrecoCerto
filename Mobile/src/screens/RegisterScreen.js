import { Alert, Text } from 'react-native';
import { useState } from 'react';
import { registrarCliente } from '../services/clienteService';
import { registrarLojista } from '../services/lojistaService';
import { login as authLogin } from '../services/authService';
import { formatApiError } from '../utils/apiErrorUtils';
import { obterLocalizacaoAtual } from '../services/locationService';
import { useAuth } from '../context/AuthContext';
import {
  FormScreen,
  FormField,
  FormTabs,
  PrimaryButton,
  formStyles,
} from '../components/form';

export default function RegisterScreen({ navigation, route }) {
  const [tipoCadastro, setTipoCadastro] = useState(route?.params?.tipoInicial || 'cliente');
  const [nomeUsuario, setNomeUsuario] = useState('');
  const [email, setEmail] = useState('');
  const [telefone, setTelefone] = useState('');
  const [cargo, setCargo] = useState('Gerente');
  const [senha, setSenha] = useState('');
  const [confirmarSenha, setConfirmarSenha] = useState('');
  const [loading, setLoading] = useState(false);
  const { salvarSessao } = useAuth();

  async function handleRegister() {
    if (!nomeUsuario || !email || !senha || !confirmarSenha) {
      Alert.alert('Erro', 'Preencha todos os campos obrigatórios');
      return;
    }

    if (senha !== confirmarSenha) {
      Alert.alert('Erro', 'As senhas não coincidem');
      return;
    }

    setLoading(true);
    try {
      if (tipoCadastro === 'cliente') {
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

        const { perfil } = await authLogin(email.trim(), senha, 'cliente');
        await salvarSessao({ tipo: 'cliente', perfil }, 'user');
        navigation.replace('Home');
        Alert.alert('Sucesso', 'Conta de cliente criada!');
        return;
      }

      await registrarLojista({
        nomeUsuario: nomeUsuario.trim(),
        email: email.trim(),
        senha,
        telefone: telefone.trim() || null,
        cargo: cargo.trim() || 'Gerente',
      });

      const { perfil } = await authLogin(email.trim(), senha, 'lojista');
      await salvarSessao({ tipo: 'lojista', perfil }, 'store');
      navigation.replace('Home');
      Alert.alert('Sucesso', 'Conta de lojista criada! Agora cadastre sua loja.');
    } catch (error) {
      Alert.alert('Erro', formatApiError(error));
    } finally {
      setLoading(false);
    }
  }

  const labelCadastro = tipoCadastro === 'cliente' ? 'cliente' : 'lojista';

  return (
    <FormScreen
      title="Cadastro"
      subtitle="Crie sua conta no Preço Certo"
      onBack={() => navigation.goBack()}
      footer={
        <PrimaryButton
          label={`Cadastrar ${labelCadastro}`}
          onPress={handleRegister}
          loading={loading}
        />
      }
    >
      <FormTabs
        options={[
          { value: 'cliente', label: 'Cliente' },
          { value: 'lojista', label: 'Lojista' },
        ]}
        value={tipoCadastro}
        onChange={setTipoCadastro}
      />

      <Text style={formStyles.sectionHint}>
        {tipoCadastro === 'cliente'
          ? 'Latitude e longitude são capturadas automaticamente pelo GPS.'
          : 'Cadastre a loja com CEP e endereço depois de criar a conta.'}
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

      {tipoCadastro === 'lojista' ? (
        <FormField label="Cargo na loja" value={cargo} onChangeText={setCargo} />
      ) : null}

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
