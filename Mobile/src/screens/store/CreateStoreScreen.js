import { View, Text, Pressable, Alert, ActivityIndicator } from 'react-native';
import { useState } from 'react';
import { criarLoja } from '../../services/lojaService';
import { useAuth } from '../../context/AuthContext';
import { buscarEnderecoPorCep, geocodificarEndereco } from '../../services/enderecoService';
import { obterLocalizacaoAtual } from '../../services/locationService';
import StoreLocationMapView from '../../components/StoreLocationMapView';
import {
  FormScreen,
  FormField,
  PrimaryButton,
  SecondaryButton,
  formStyles,
} from '../../components/form';
import { isCnpjValido } from '../../utils/validacaoUtils';
import { colors } from '../../theme';

const STEPS = [
  { key: 'loja', label: 'Loja' },
  { key: 'endereco', label: 'Endereço' },
  { key: 'local', label: 'Localização' },
];

export default function CreateStoreScreen({ navigation }) {
  const { session, logout } = useAuth();
  const [step, setStep] = useState(0);

  const [nomeFantasia, setNomeFantasia] = useState('');
  const [cnpj, setCnpj] = useState('');
  const [telefone, setTelefone] = useState('');
  const [emailLoja, setEmailLoja] = useState('');

  const [cep, setCep] = useState('');
  const [logradouro, setLogradouro] = useState('');
  const [numero, setNumero] = useState('');
  const [bairro, setBairro] = useState('');
  const [cidade, setCidade] = useState('');
  const [estado, setEstado] = useState('');

  const [latitude, setLatitude] = useState(null);
  const [longitude, setLongitude] = useState(null);

  const [loading, setLoading] = useState(false);
  const [loadingCep, setLoadingCep] = useState(false);
  const [loadingCoords, setLoadingCoords] = useState(false);

  function validarPassoLoja() {
    if (!nomeFantasia.trim()) {
      Alert.alert('Loja', 'Informe o nome fantasia.');
      return false;
    }
    // Abrir loja exige CNPJ válido — é o que torna o usuário um lojista.
    if (!cnpj.trim()) {
      Alert.alert('Loja', 'Informe o CNPJ da loja.');
      return false;
    }
    if (!isCnpjValido(cnpj)) {
      Alert.alert('Loja', 'CNPJ inválido. Verifique os números informados.');
      return false;
    }
    return true;
  }

  function validarPassoEndereco() {
    if (!cep.trim() || !logradouro.trim() || !numero.trim() || !cidade.trim() || !estado.trim()) {
      Alert.alert('Endereço', 'Preencha CEP, logradouro, número, cidade e estado.');
      return false;
    }
    return true;
  }

  function avancar() {
    if (step === 0 && !validarPassoLoja()) return;
    if (step === 1 && !validarPassoEndereco()) return;
    if (step < STEPS.length - 1) setStep((s) => s + 1);
  }

  function voltar() {
    if (step > 0) setStep((s) => s - 1);
    else navigation.goBack();
  }

  async function handleBuscarCep() {
    if (cep.replace(/\D/g, '').length < 8) {
      Alert.alert('CEP', 'Informe um CEP válido com 8 dígitos.');
      return;
    }

    setLoadingCep(true);
    try {
      const endereco = await buscarEnderecoPorCep(cep);
      setCep(endereco.cep);
      setLogradouro(endereco.logradouro);
      setBairro(endereco.bairro);
      setCidade(endereco.cidade);
      setEstado(endereco.estado);
      setLatitude(null);
      setLongitude(null);
    } catch (error) {
      Alert.alert('CEP', error.message);
    } finally {
      setLoadingCep(false);
    }
  }

  async function handleGeocodificar() {
    if (!validarPassoEndereco()) return;

    setLoadingCoords(true);
    try {
      const coords = await geocodificarEndereco({
        cep,
        logradouro,
        numero,
        bairro,
        cidade,
        estado,
      });
      setLatitude(coords.latitude);
      setLongitude(coords.longitude);
    } catch (error) {
      Alert.alert('Coordenadas', error.message);
    } finally {
      setLoadingCoords(false);
    }
  }

  async function handleUsarGpsAtual() {
    setLoadingCoords(true);
    try {
      const coords = await obterLocalizacaoAtual();
      setLatitude(coords.latitude);
      setLongitude(coords.longitude);
    } catch (error) {
      Alert.alert('GPS', error.message);
    } finally {
      setLoadingCoords(false);
    }
  }

  async function resolverCoordenadas() {
    if (latitude !== null && longitude !== null) {
      return { latitude, longitude };
    }

    const coords = await geocodificarEndereco({
      cep,
      logradouro,
      numero,
      bairro,
      cidade,
      estado,
    });
    setLatitude(coords.latitude);
    setLongitude(coords.longitude);
    return coords;
  }

  async function handleCreateStore() {
    if (!validarPassoLoja() || !validarPassoEndereco()) {
      setStep(!nomeFantasia.trim() ? 0 : 1);
      return;
    }

    if (!session?.perfil?.id) {
      Alert.alert('Erro', 'Faça login para abrir uma loja.');
      return;
    }

    setLoading(true);
    try {
      let lat = latitude;
      let lng = longitude;

      if (lat === null || lng === null) {
        try {
          const coords = await resolverCoordenadas();
          lat = coords.latitude;
          lng = coords.longitude;
        } catch {
          lat = null;
          lng = null;
        }
      }

      // O backend define o usuário autenticado como dono e o promove a Lojista.
      await criarLoja({
        nomeFantasia: nomeFantasia.trim(),
        cnpj: cnpj.trim(),
        telefone: telefone.trim() || null,
        email: emailLoja.trim() || session.perfil.email,
        endereco: {
          cep,
          logradouro,
          numero,
          bairro,
          cidade,
          estado,
          latitude: lat,
          longitude: lng,
        },
      });

      // Como o papel mudou para Lojista, é preciso reautenticar para obter um
      // token com as permissões de loja.
      Alert.alert(
        'Loja criada!',
        'Sua conta agora é de lojista. Entre novamente para acessar o painel da loja.',
        [{ text: 'OK', onPress: () => logout() }]
      );
    } catch (error) {
      const msg = error.response?.data || error.message;
      Alert.alert('Erro', String(msg));
    } finally {
      setLoading(false);
    }
  }

  const coordsOk = latitude !== null && longitude !== null;

  return (
    <FormScreen
      title="Criar loja"
      onBack={voltar}
      backLabel={step === 0 ? 'Voltar' : 'Anterior'}
      steps={STEPS}
      currentStep={step}
      footer={
        step < STEPS.length - 1 ? (
          <PrimaryButton label="Continuar" onPress={avancar} />
        ) : (
          <PrimaryButton label="Criar loja" onPress={handleCreateStore} loading={loading} />
        )
      }
    >
      {step === 0 && (
        <View style={formStyles.section}>
          <Text style={formStyles.sectionHint}>Dados principais da loja</Text>
          <FormField
            label="Nome fantasia *"
            value={nomeFantasia}
            onChangeText={setNomeFantasia}
            autoFocus
          />
          <FormField label="CNPJ" value={cnpj} onChangeText={setCnpj} />
          <FormField
            label="Telefone"
            value={telefone}
            onChangeText={setTelefone}
            keyboardType="phone-pad"
          />
          <FormField
            label="E-mail da loja"
            value={emailLoja}
            onChangeText={setEmailLoja}
            autoCapitalize="none"
            keyboardType="email-address"
            placeholder={session?.perfil?.email || 'opcional'}
          />
        </View>
      )}

      {step === 1 && (
        <View style={formStyles.section}>
          <Text style={formStyles.sectionHint}>CEP preenche o endereço automaticamente</Text>
          <View style={formStyles.cepRow}>
            <View style={formStyles.cepInputWrap}>
              <FormField
                label="CEP *"
                value={cep}
                onChangeText={setCep}
                keyboardType="number-pad"
                compact
              />
            </View>
            <Pressable
              style={[formStyles.cepButton, loadingCep && formStyles.buttonDisabled]}
              onPress={handleBuscarCep}
              disabled={loadingCep}
            >
              {loadingCep ? (
                <ActivityIndicator color="#fff" size="small" />
              ) : (
                <Text style={formStyles.cepButtonText}>Buscar</Text>
              )}
            </Pressable>
          </View>
          <FormField label="Logradouro *" value={logradouro} onChangeText={setLogradouro} />
          <FormField label="Número *" value={numero} onChangeText={setNumero} />
          <FormField label="Bairro" value={bairro} onChangeText={setBairro} />
          <View style={formStyles.row}>
            <View style={formStyles.rowItemLarge}>
              <FormField label="Cidade *" value={cidade} onChangeText={setCidade} compact />
            </View>
            <View style={formStyles.rowItemSmall}>
              <FormField
                label="UF *"
                value={estado}
                onChangeText={setEstado}
                maxLength={2}
                autoCapitalize="characters"
                compact
              />
            </View>
          </View>
        </View>
      )}

      {step === 2 && (
        <View style={formStyles.section}>
          <Text style={formStyles.sectionHint}>
            Toque no mapa ou arraste o pin para marcar onde a loja fica. Também pode usar o endereço
            ou o GPS.
          </Text>
          <View style={formStyles.summaryCard}>
            <Text style={formStyles.summaryTitle}>{nomeFantasia || '—'}</Text>
            <Text style={formStyles.summaryText}>
              {logradouro}, {numero}
              {bairro ? ` · ${bairro}` : ''}
            </Text>
            <Text style={formStyles.summaryText}>
              {cidade}/{estado} · CEP {cep}
            </Text>
          </View>

          {loadingCoords ? (
            <ActivityIndicator
              color={colors.primary}
              style={{ marginVertical: 12 }}
            />
          ) : null}

          <StoreLocationMapView
            latitude={latitude}
            longitude={longitude}
            titulo={nomeFantasia || 'Sua loja'}
            onCoordsChange={({ latitude: lat, longitude: lng }) => {
              setLatitude(lat);
              setLongitude(lng);
            }}
            style={{ marginBottom: 12 }}
          />

          <Text style={[formStyles.sectionHint, { color: colors.primaryDark, marginBottom: 12 }]}>
            {coordsOk
              ? `Coordenadas: ${latitude.toFixed(5)}, ${longitude.toFixed(5)}`
              : 'Toque no mapa para definir a localização'}
          </Text>
          <SecondaryButton
            label="Centralizar pelo endereço"
            onPress={handleGeocodificar}
            disabled={loadingCoords}
          />
          <SecondaryButton
            label="Usar minha localização (GPS)"
            onPress={handleUsarGpsAtual}
            disabled={loadingCoords}
          />
        </View>
      )}
    </FormScreen>
  );
}
