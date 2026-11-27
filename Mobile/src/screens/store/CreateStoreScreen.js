import { View, Text, Pressable, ActivityIndicator, Platform } from 'react-native';
import { useRef, useState } from 'react';
import { Ionicons } from '@expo/vector-icons';
import { criarLoja } from '../../services/lojaService';
import { useAuth } from '../../context/AuthContext';
import { clearPendingAuthAction } from '../../navigation/authIntent';
import { buscarEnderecoPorCep, geocodificarEndereco } from '../../services/enderecoService';
import { consultarCnpj } from '../../services/consultaService';
import { obterLocalizacaoAtual } from '../../services/locationService';
import StoreLocationMapView from '../../components/StoreLocationMapView';
import {
  FormScreen,
  FormField,
  PrimaryButton,
  SecondaryButton,
} from '../../components/form';
import { apenasDigitosCnpj, isCnpjValido } from '../../utils/validacaoUtils';
import {
  FRASES,
  mensagemErroConsultaCnpj,
  montarPayloadCriarLoja,
  planejarPreenchimento,
  validarDadosEndereco,
  validarDadosLoja,
} from '../../utils/cnpjLoja';
import { formatApiError, mapApiError } from '../../utils/apiErrorUtils';
import { useTheme } from '../../context/ThemeContext';
import { useFormStyles } from '../../hooks/useFormStyles';

const STEPS = [
  { key: 'loja', label: 'Loja' },
  { key: 'endereco', label: 'Endereço' },
  { key: 'local', label: 'Localização' },
];

export default function CreateStoreScreen({ navigation }) {
  const { session, logout } = useAuth();
  const { colors } = useTheme();
  const formStyles = useFormStyles();
  const [step, setStep] = useState(0);

  const [nomeFantasia, setNomeFantasia] = useState('');
  const [razaoSocial, setRazaoSocial] = useState('');
  const [cnpj, setCnpj] = useState('');
  const [telefone, setTelefone] = useState('');
  const [emailLoja, setEmailLoja] = useState('');
  const [erroCnpj, setErroCnpj] = useState('');
  const [confirmacaoCnpj, setConfirmacaoCnpj] = useState(null);
  // Feedback visual unificado (Web + Mobile) via FormErrorBanner, evitando Alert.alert no-op no react-native-web.
  const [feedback, setFeedback] = useState(null);

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
  const [loadingCnpj, setLoadingCnpj] = useState(false);
  const [loadingCoords, setLoadingCoords] = useState(false);
  const [cnpjVerificado, setCnpjVerificado] = useState(false);
  const consultaRef = useRef({ digitos: '', dados: null, pedido: null });
  const geracaoRef = useRef(0);
  const camposRef = useRef({});
  camposRef.current = {
    nomeFantasia,
    razaoSocial,
    telefone,
    email: emailLoja,
    cep,
    logradouro,
    numero,
    bairro,
    cidade,
    estado,
  };

  function aplicarCampos(patch) {
    if (patch.nomeFantasia) setNomeFantasia(patch.nomeFantasia);
    if (patch.razaoSocial) setRazaoSocial(patch.razaoSocial);
    if (patch.telefone) setTelefone(patch.telefone);
    if (patch.email) setEmailLoja(patch.email);
    if (patch.cep) setCep(patch.cep);
    if (patch.logradouro) setLogradouro(patch.logradouro);
    if (patch.numero) setNumero(patch.numero);
    if (patch.bairro) setBairro(patch.bairro);
    if (patch.cidade) setCidade(patch.cidade);
    if (patch.estado) setEstado(patch.estado);
  }

  function oferecerDadosCnpj(data) {
    const { vazios, conflitos } = planejarPreenchimento(data, camposRef.current);
    aplicarCampos(vazios);
    setConfirmacaoCnpj(Object.keys(conflitos).length ? conflitos : null);
  }

  function limparConsultaCnpj() {
    geracaoRef.current += 1;
    consultaRef.current = { digitos: '', dados: null, pedido: null };
    setCnpjVerificado(false);
    setErroCnpj('');
    setConfirmacaoCnpj(null);
    setLoadingCnpj(false);
  }

  async function buscarDadosCnpj(repetir = false) {
    const digitos = apenasDigitosCnpj(cnpj);
    if (digitos.length !== 14) return false;
    if (!isCnpjValido(digitos)) {
      setErroCnpj(FRASES.cnpj_invalido);
      setCnpjVerificado(false);
      return false;
    }

    const atual = consultaRef.current;
    if (atual.digitos === digitos && atual.dados) {
      if (repetir) oferecerDadosCnpj(atual.dados);
      setCnpjVerificado(true);
      setErroCnpj('');
      return true;
    }
    if (atual.digitos === digitos && atual.pedido) return atual.pedido;

    const geracao = ++geracaoRef.current;
    setLoadingCnpj(true);
    setErroCnpj('');
    setConfirmacaoCnpj(null);

    const pedido = consultarCnpj(digitos)
      .then((data) => {
        if (geracaoRef.current !== geracao) return false;
        consultaRef.current = { digitos, dados: data, pedido: null };
        setCnpjVerificado(true);
        oferecerDadosCnpj(data);
        return true;
      })
      .catch((error) => {
        if (geracaoRef.current !== geracao) return false;
        consultaRef.current = { digitos: '', dados: null, pedido: null };
        setCnpjVerificado(false);
        setErroCnpj(mensagemErroConsultaCnpj(error));
        return false;
      })
      .finally(() => {
        if (geracaoRef.current === geracao) setLoadingCnpj(false);
      });

    consultaRef.current = { digitos, dados: null, pedido };
    return pedido;
  }

  async function garantirCnpjNaReceita() {
    if (cnpjVerificado) return true;
    return buscarDadosCnpj();
  }

  function validarPassoLoja() {
    const erro = validarDadosLoja({
      nomeFantasia,
      cnpj,
      telefone,
      email: emailLoja,
    });
    if (erro) {
      setFeedback(erro);
      return false;
    }
    return true;
  }

  function validarPassoEndereco() {
    const erro = validarDadosEndereco({
      cep,
      logradouro,
      numero,
      cidade,
      estado,
    });
    if (erro) {
      setFeedback(erro);
      return false;
    }
    return true;
  }

  async function avancar() {
    setFeedback(null);
    if (step === 0) {
      if (!validarPassoLoja()) return;
      if (!(await garantirCnpjNaReceita())) return;
    }
    if (step === 1 && !validarPassoEndereco()) return;
    if (step < STEPS.length - 1) setStep((s) => s + 1);
  }

  function voltar() {
    setFeedback(null);
    if (step > 0) setStep((s) => s - 1);
    else navigation.goBack();
  }

  async function handleBuscarCep() {
    setFeedback(null);
    if (cep.replace(/\D/g, '').length < 8) {
      setFeedback({ title: 'CEP', message: 'Informe um CEP válido com 8 dígitos.' });
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
      setFeedback({ title: 'CEP', message: error.message });
    } finally {
      setLoadingCep(false);
    }
  }

  async function handleGeocodificar() {
    setFeedback(null);
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
      setFeedback({ title: 'Coordenadas', message: error.message });
    } finally {
      setLoadingCoords(false);
    }
  }

  async function handleUsarGpsAtual() {
    setFeedback(null);
    setLoadingCoords(true);
    try {
      const coords = await obterLocalizacaoAtual({ allowCached: false });
      setLatitude(coords.latitude);
      setLongitude(coords.longitude);
    } catch (error) {
      setFeedback({ title: 'GPS', message: error.message });
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

  /**
   * Conclui a promoção da conta para lojista em Web e Mobile:
   * navega primeiro para Login com o aviso de confirmação (evitando que o guard sobrescreva params)
   * e em seguida limpa a sessão antiga de cliente para obter novo JWT com papel Lojista.
   */
  async function concluirCriacaoLoja(notice) {
    clearPendingAuthAction();
    navigation.reset({
      index: 0,
      routes: [{ name: 'Login', params: { authNotice: notice } }],
    });
    await logout();
  }

  async function handleCreateStore() {
    if (loading) return;
    setFeedback(null);

    if (!validarPassoLoja()) {
      setStep(0);
      return;
    }
    if (!validarPassoEndereco()) {
      setStep(1);
      return;
    }

    if (!session?.perfil?.id) {
      setFeedback({ title: 'Sessão necessária', message: 'Faça login para abrir uma loja.' });
      return;
    }

    setLoading(true);
    let finalizou = false;
    try {
      if (!(await garantirCnpjNaReceita())) {
        setStep(0);
        return;
      }

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
      await criarLoja(
        montarPayloadCriarLoja({
          nomeFantasia,
          razaoSocial,
          cnpj,
          telefone,
          emailLoja,
          sessionEmail: session.perfil.email,
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
        })
      );

      finalizou = true;
      await concluirCriacaoLoja({
        title: 'Loja criada!',
        message: 'Sua conta agora é de lojista. Entre novamente para acessar o painel da loja.',
        variant: 'success',
      });
    } catch (error) {
      if (error.response?.status === 409) {
        finalizou = true;
        await concluirCriacaoLoja({
          title: 'Loja já cadastrada',
          message: formatApiError(error),
          variant: 'info',
        });
        return;
      }
      setFeedback(mapApiError(error));
    } finally {
      if (!finalizou) setLoading(false);
    }
  }

  const coordsOk = latitude !== null && longitude !== null;
  const cnpjCompleto = apenasDigitosCnpj(cnpj).length === 14;

  return (
    <FormScreen
      title="Criar loja"
      onBack={voltar}
      backLabel={step === 0 ? 'Voltar' : 'Anterior'}
      steps={STEPS}
      currentStep={step}
      error={feedback}
      footer={
        step < STEPS.length - 1 ? (
          <PrimaryButton label="Continuar" onPress={avancar} loading={loadingCnpj} />
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
            returnKeyType="next"
          />
          <FormField
            label="Razão social"
            value={razaoSocial}
            onChangeText={setRazaoSocial}
            returnKeyType="next"
          />
          <FormField
            label="CNPJ"
            value={cnpj}
            onChangeText={(value) => {
              setCnpj(value);
              limparConsultaCnpj();
            }}
            onBlur={() => buscarDadosCnpj(false)}
            keyboardType="number-pad"
            returnKeyType="next"
            rightAccessory={
              <Pressable
                accessibilityRole="button"
                accessibilityLabel={loadingCnpj ? 'Buscando dados do CNPJ…' : 'Buscar dados do CNPJ'}
                accessibilityState={{ disabled: !cnpjCompleto, busy: loadingCnpj }}
                disabled={!cnpjCompleto}
                onPress={() => buscarDadosCnpj(true)}
                style={({ focused }) => [
                  lupaBase,
                  !cnpjCompleto && { opacity: 0.4 },
                  focused && lupaFoco,
                ]}
              >
                {loadingCnpj ? (
                  <ActivityIndicator color={colors.primary} size="small" />
                ) : (
                  <Ionicons
                    name="search-outline"
                    size={20}
                    color={cnpjCompleto ? colors.primary : colors.textMuted}
                    accessible={false}
                  />
                )}
              </Pressable>
            }
          />
          {erroCnpj ? (
            <Text accessibilityRole="alert" accessibilityLiveRegion="polite" style={formStyles.dangerText}>
              {erroCnpj}
            </Text>
          ) : null}
          {confirmacaoCnpj ? (
            <View accessibilityRole="alert">
              <Text style={formStyles.sectionHint}>
                Alguns campos já estão preenchidos. Substituir pelos dados do CNPJ?
              </Text>
              <SecondaryButton
                label="Manter"
                accessibilityLabel="Manter o que já digitei"
                onPress={() => setConfirmacaoCnpj(null)}
              />
              <PrimaryButton
                label="Substituir"
                accessibilityLabel="Substituir pelos dados do CNPJ"
                onPress={() => {
                  aplicarCampos(confirmacaoCnpj);
                  setConfirmacaoCnpj(null);
                }}
              />
            </View>
          ) : null}
          <FormField
            label="Telefone"
            value={telefone}
            onChangeText={setTelefone}
            keyboardType="phone-pad"
            returnKeyType="next"
          />
          <FormField
            label="E-mail da loja"
            value={emailLoja}
            onChangeText={setEmailLoja}
            autoCapitalize="none"
            keyboardType="email-address"
            placeholder={session?.perfil?.email || 'opcional'}
            returnKeyType="next"
            onSubmitEditing={avancar}
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
                returnKeyType="search"
                onSubmitEditing={handleBuscarCep}
              />
            </View>
            <Pressable
              style={[formStyles.cepButton, loadingCep && formStyles.buttonDisabled]}
              onPress={handleBuscarCep}
              disabled={loadingCep}
              accessibilityRole="button"
              accessibilityLabel="Buscar CEP"
            >
              {loadingCep ? (
                <ActivityIndicator color={colors.primaryForeground} size="small" />
              ) : (
                <Text style={formStyles.cepButtonText}>Buscar</Text>
              )}
            </Pressable>
          </View>
          <FormField
            label="Logradouro *"
            value={logradouro}
            onChangeText={setLogradouro}
            returnKeyType="next"
          />
          <FormField
            label="Número *"
            value={numero}
            onChangeText={setNumero}
            returnKeyType="next"
          />
          <FormField
            label="Bairro"
            value={bairro}
            onChangeText={setBairro}
            returnKeyType="next"
          />
          <View style={formStyles.row}>
            <View style={formStyles.rowItemLarge}>
              <FormField
                label="Cidade *"
                value={cidade}
                onChangeText={setCidade}
                compact
                returnKeyType="next"
              />
            </View>
            <View style={formStyles.rowItemSmall}>
              <FormField
                label="UF *"
                value={estado}
                onChangeText={setEstado}
                maxLength={2}
                autoCapitalize="characters"
                compact
                returnKeyType="next"
                onSubmitEditing={avancar}
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

const lupaBase = {
  width: 44,
  height: 44,
  alignItems: 'center',
  justifyContent: 'center',
  flexShrink: 0,
  ...(Platform.OS === 'web' ? { cursor: 'pointer' } : null),
};

const lupaFoco = Platform.OS === 'web' ? { boxShadow: '0 0 0 2px rgba(14, 124, 102, 0.45)' } : null;
