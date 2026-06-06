import { StatusBar } from 'expo-status-bar';
import { View, Text, Pressable, Alert } from 'react-native';
import { useState } from 'react';
import { loginCliente } from '../services/clienteService';
import { loginLojista } from '../services/lojistaService';
import { useAuth } from '../context/AuthContext';
import {
  FormScreen,
  FormField,
  FormTabs,
  PrimaryButton,
  formStyles,
} from '../components/form';

export default function LoginScreen({ navigation }) {
  const [email, setEmail] = useState('');
  const [senha, setSenha] = useState('');
  const [tipo, setTipo] = useState('cliente');
  const [loading, setLoading] = useState(false);
  const { salvarSessao, sincronizarGpsCliente } = useAuth();

  async function handleLogin() {
    if (!email || !senha) {
      Alert.alert('Erro', 'Preencha todos os campos');
      return;
    }

    setLoading(true);
    try {
      if (tipo === 'cliente') {
        const perfil = await loginCliente(email.trim(), senha);
        await salvarSessao({ tipo: 'cliente', perfil }, 'user');
        await sincronizarGpsCliente();
        navigation.replace('Home');
      } else {
        const perfil = await loginLojista(email.trim(), senha);
        await salvarSessao({ tipo: 'lojista', perfil }, 'store');
        navigation.replace('Home');
      }
    } catch (error) {
      const msg =
        error.response?.data ||
        error.message ||
        'Não foi possível entrar. Verifique email e senha.';
      Alert.alert('Erro', String(msg));
    } finally {
      setLoading(false);
    }
  }

  return (
    <FormScreen
      title="Preço Certo"
      subtitle="Entre com sua conta"
      footer={<PrimaryButton label="Entrar" onPress={handleLogin} loading={loading} />}
    >
      <FormTabs
        options={[
          { value: 'cliente', label: 'Cliente' },
          { value: 'lojista', label: 'Lojista' },
        ]}
        value={tipo}
        onChange={setTipo}
      />

      {tipo === 'cliente' ? (
        <Text style={formStyles.sectionHint}>
          Após o login, sua localização é obtida automaticamente pelo GPS.
        </Text>
      ) : null}

      <FormField
        label="E-mail"
        value={email}
        onChangeText={setEmail}
        autoCapitalize="none"
        keyboardType="email-address"
      />
      <FormField
        label="Senha"
        value={senha}
        onChangeText={setSenha}
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
          onPress={() => navigation.navigate('Cadastro', { tipoInicial: tipo })}
          style={{ flex: 1, paddingVertical: 8, alignItems: 'flex-end' }}
        >
          <Text style={formStyles.linkText}>Novo usuário</Text>
        </Pressable>
      </View>

      <StatusBar style="auto" />
    </FormScreen>
  );
}
