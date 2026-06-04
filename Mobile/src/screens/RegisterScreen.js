import { View, Text, TextInput, Pressable, Alert } from 'react-native';
import { useState } from 'react';
import { styles } from '../style';
import { registrarCliente, loginCliente } from '../services/clienteService';
import { registrarLojista, loginLojista } from '../services/lojistaService';
import { useAuth } from '../context/AuthContext';

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
        await registrarCliente({
          nomeUsuario: nomeUsuario.trim(),
          email: email.trim(),
          senha,
          telefone: telefone.trim() || null,
        });

        const { token, perfil } = await loginCliente(email.trim(), senha);
        await salvarSessao({ tipo: 'cliente', perfil }, 'user', token);
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

      const { token, perfil } = await loginLojista(email.trim(), senha);
      await salvarSessao({ tipo: 'lojista', perfil }, 'store', token);
      Alert.alert('Sucesso', 'Conta de lojista criada! Agora cadastre sua loja.');
    } catch (error) {
      const msg =
        error.response?.data ||
        error.message ||
        'Não foi possível criar a conta.';
      Alert.alert('Erro', String(msg));
    } finally {
      setLoading(false);
    }
  }

  return (
    <View style={styles.container}>
      <Pressable onPress={() => navigation.goBack()}>
        <Text style={{ color: '#22C55E', marginBottom: 20 }}>← Voltar</Text>
      </Pressable>

      <Text style={styles.formTitle}>Cadastro</Text>

      <View style={{ flexDirection: 'row', marginBottom: 16, gap: 8 }}>
        <Pressable
          onPress={() => setTipoCadastro('cliente')}
          style={[
            styles.formButton,
            { flex: 1, backgroundColor: tipoCadastro === 'cliente' ? '#22C55E' : '#374151' },
          ]}
        >
          <Text style={styles.textButton}>Cliente</Text>
        </Pressable>
        <Pressable
          onPress={() => setTipoCadastro('lojista')}
          style={[
            styles.formButton,
            { flex: 1, backgroundColor: tipoCadastro === 'lojista' ? '#22C55E' : '#374151' },
          ]}
        >
          <Text style={styles.textButton}>Lojista</Text>
        </Pressable>
      </View>

      <TextInput
        style={styles.formInput}
        placeholder="Nome"
        value={nomeUsuario}
        onChangeText={setNomeUsuario}
      />

      <TextInput
        style={styles.formInput}
        placeholder="E-mail"
        autoCapitalize="none"
        keyboardType="email-address"
        value={email}
        onChangeText={setEmail}
      />

      <TextInput
        style={styles.formInput}
        placeholder="Telefone (opcional)"
        value={telefone}
        onChangeText={setTelefone}
      />

      {tipoCadastro === 'lojista' ? (
        <TextInput
          style={styles.formInput}
          placeholder="Cargo"
          value={cargo}
          onChangeText={setCargo}
        />
      ) : null}

      <TextInput
        style={styles.formInput}
        placeholder="Senha"
        secureTextEntry
        value={senha}
        onChangeText={setSenha}
      />

      <TextInput
        style={styles.formInput}
        placeholder="Confirmar senha"
        secureTextEntry
        value={confirmarSenha}
        onChangeText={setConfirmarSenha}
      />

      <Pressable
        style={[styles.formButton, loading && { opacity: 0.6 }]}
        onPress={handleRegister}
        disabled={loading}
      >
        <Text style={styles.textButton}>{loading ? 'Cadastrando...' : 'Cadastrar'}</Text>
      </Pressable>
    </View>
  );
}
