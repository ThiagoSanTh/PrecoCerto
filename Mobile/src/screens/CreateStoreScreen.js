import { View, Text, TextInput, Pressable, Alert } from 'react-native';
import { styles } from '../style';
import { useState } from 'react';
import AsyncStorage from '@react-native-async-storage/async-storage';
import { useAuth } from '../context/AuthContext';

export default function CreateStoreScreen({ navigation }) {
  const { session } = useAuth();
  const [nomeFantasia, setNomeFantasia] = useState('');
  const [cnpj, setCnpj] = useState('');
  const [telefone, setTelefone] = useState('');
  const [email, setEmail] = useState('');
  const [descricao, setDescricao] = useState('');

  async function handleCreateStore() {
    if (!nomeFantasia) {
      Alert.alert('Erro', 'Nome Fantasia é obrigatório');
      return;
    }
  
    const userEmail = session?.perfil?.email;

    if (!userEmail) {
      Alert.alert('Erro', 'Usuário não autenticado');
      return;
    }

    const newStore = {
      id: Date.now().toString(),
      nomeFantasia,
      cnpj,
      telefone,
      email,
      descricao,
      userEmail,
    };
  
    // 💾 salva normalmente
    const data = await AsyncStorage.getItem('@lojas');
    let lojas = data ? JSON.parse(data) : [];
  
    lojas.push(newStore);
  
    await AsyncStorage.setItem('@lojas', JSON.stringify(lojas));
  
    await AsyncStorage.setItem('@userMode', 'store');
  
    Alert.alert('Sucesso', 'Loja criada com sucesso!');
  
    navigation.replace('Home');
  }

  return (
    <View style={styles.container}>
      <Text style={styles.formTitle}>Criar Loja 🏪</Text>

      <TextInput
        style={styles.formInput}
        placeholder="Nome Fantasia *"
        value={nomeFantasia}
        onChangeText={setNomeFantasia}
      />

      <TextInput
        style={styles.formInput}
        placeholder="CNPJ"
        value={cnpj}
        onChangeText={setCnpj}
      />

      <TextInput
        style={styles.formInput}
        placeholder="Telefone"
        value={telefone}
        onChangeText={setTelefone}
      />

      <TextInput
        style={styles.formInput}
        placeholder="Email da loja"
        value={email}
        onChangeText={setEmail}
      />

      <TextInput
        style={styles.formInput}
        placeholder="Descrição"
        value={descricao}
        onChangeText={setDescricao}
      />

      <Pressable style={styles.formButton} onPress={handleCreateStore}>
        <Text style={styles.textButton}>Criar Loja</Text>
      </Pressable>

      <Pressable
        style={styles.formButton}
        onPress={() => navigation.goBack()}
      >
        <Text style={styles.textButton}>Voltar</Text>
      </Pressable>
    </View>
  );
}