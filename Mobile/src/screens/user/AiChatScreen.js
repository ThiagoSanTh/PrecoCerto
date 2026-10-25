import {
  View,
  Text,
  FlatList,
  TextInput,
  Pressable,
  ActivityIndicator,
  KeyboardAvoidingView,
  Platform,
  ScrollView,
} from 'react-native';
import { useRef, useState } from 'react';
import { FormScreen } from '../../components/form';
import { useAuth } from '../../context/AuthContext';
import { useTheme } from '../../context/ThemeContext';
import { enviarPerguntaIA, isAdminSession } from '../../services/iaService';

const CHIPS_CLIENTE = [
  '🔥 Promoções de hoje',
  '🛒 Cesta básica mais barata',
  '📍 Lojas mais próximas',
  '🥩 Itens para churrasco',
];

const CHIPS_LOJISTA = [
  '📊 Meus preços vs concorrência',
  '🔥 Promoções do mercado',
  '📍 Lojas concorrentes próximas',
];

function idMsg() {
  return `${Date.now()}-${Math.random().toString(36).slice(2, 8)}`;
}

function formatPreco(valor) {
  if (valor == null || Number.isNaN(Number(valor))) return null;
  return `R$ ${Number(valor).toFixed(2).replace('.', ',')}`;
}

function ResultadoCardsList({ resultados, colors, navigation }) {
  if (!Array.isArray(resultados) || resultados.length === 0) return null;

  return (
    <View style={{ marginTop: 10, gap: 8 }}>
      {resultados.slice(0, 5).map((r, idx) => {
        const precoFmt = formatPreco(r.preco);
        const distFmt =
          r.distanciaKm != null && !Number.isNaN(Number(r.distanciaKm))
            ? `${Number(r.distanciaKm).toFixed(1)} km`
            : null;
        return (
          <View
            key={`${r.titulo || 'item'}-${idx}`}
            style={{
              backgroundColor: colors.background || '#F8FAFC',
              borderWidth: 1,
              borderColor: colors.border,
              borderRadius: 10,
              padding: 10,
              gap: 4,
            }}
          >
            <View style={{ flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center', gap: 6 }}>
              <Text style={{ fontWeight: '700', fontSize: 13, color: colors.text, flex: 1 }}>
                {r.titulo || r.produto || r.loja || 'Opção'}
              </Text>
              {r.emPromocao ? (
                <View
                  style={{
                    backgroundColor: '#DCFCE7',
                    paddingHorizontal: 6,
                    paddingVertical: 2,
                    borderRadius: 6,
                  }}
                >
                  <Text style={{ fontSize: 10, fontWeight: '700', color: '#15803D' }}>PROMO</Text>
                </View>
              ) : null}
            </View>

            <View style={{ flexDirection: 'row', flexWrap: 'wrap', gap: 8, alignItems: 'center' }}>
              {precoFmt ? (
                <Text style={{ fontSize: 14, fontWeight: '800', color: colors.primary }}>
                  {precoFmt}
                </Text>
              ) : null}
              {r.loja ? (
                <Text style={{ fontSize: 12, color: colors.textMuted }}>🏪 {r.loja}</Text>
              ) : null}
              {distFmt ? (
                <Text style={{ fontSize: 12, color: colors.textMuted }}>📍 {distFmt}</Text>
              ) : null}
            </View>

            {Array.isArray(r.motivos) && r.motivos.length > 0 ? (
              <Text style={{ fontSize: 11, color: colors.textMuted }}>
                {r.motivos.slice(0, 2).join(' · ')}
              </Text>
            ) : null}

            {(r.produtoId || r.lojaId) ? (
              <View style={{ flexDirection: 'row', gap: 8, marginTop: 4 }}>
                {r.produtoId ? (
                  <Pressable
                    onPress={() => navigation.navigate('ProductDetail', { productId: r.produtoId })}
                    style={{
                      backgroundColor: colors.primary,
                      paddingHorizontal: 10,
                      paddingVertical: 6,
                      borderRadius: 8,
                    }}
                  >
                    <Text style={{ color: '#fff', fontSize: 11, fontWeight: '700' }}>Ver Produto</Text>
                  </Pressable>
                ) : null}
                {r.lojaId ? (
                  <Pressable
                    onPress={() =>
                      navigation.navigate('StoreCatalog', {
                        lojaId: r.lojaId,
                        lojaNome: r.loja || r.titulo || 'Loja',
                      })
                    }
                    style={{
                      borderWidth: 1,
                      borderColor: colors.primary,
                      paddingHorizontal: 10,
                      paddingVertical: 6,
                      borderRadius: 8,
                    }}
                  >
                    <Text style={{ color: colors.primary, fontSize: 11, fontWeight: '700' }}>Ver Loja</Text>
                  </Pressable>
                ) : null}
              </View>
            ) : null}
          </View>
        );
      })}
    </View>
  );
}

function AdminDebugPanel({ item, colors }) {
  const resultados = Array.isArray(item.resultados) ? item.resultados : [];
  return (
    <View
      style={{
        marginTop: 8,
        paddingTop: 8,
        borderTopWidth: 1,
        borderTopColor: colors.border,
        gap: 4,
      }}
    >
      <Text style={{ fontSize: 11, fontWeight: '700', color: colors.textMuted }}>
        MotorIA (admin) · Provedor: {item.provedorLlm || 'Deterministico'}
      </Text>
      <Text style={{ fontSize: 11, color: colors.textMuted }}>
        Intenção: {item.intencao || '—'} · Objetivo: {item.objetivo || '—'} · Confiança:{' '}
        {item.confianca != null ? Number(item.confianca).toFixed(2) : '—'}
      </Text>
      {item.motivos?.length ? (
        <Text style={{ fontSize: 11, color: colors.textMuted }}>
          Motivos: {item.motivos.join(' · ')}
        </Text>
      ) : null}
      {item.fallbacks?.length ? (
        <Text style={{ fontSize: 11, color: colors.textMuted }}>
          Fallbacks: {item.fallbacks.join(', ')}
        </Text>
      ) : null}
      {resultados.slice(0, 5).map((r, idx) => (
        <Text key={`${r.titulo}-${idx}`} style={{ fontSize: 11, color: colors.textMuted }}>
          #{idx + 1} score {Number(r.score ?? 0).toFixed(3)}
          {r.preco != null ? ` · ${formatPreco(r.preco)}` : ''}
          {r.distanciaKm != null ? ` · ${Number(r.distanciaKm).toFixed(1)} km` : ''}
          {r.loja ? ` · ${r.loja}` : ''}
        </Text>
      ))}
    </View>
  );
}

export default function AiChatScreen({ navigation }) {
  const { colors } = useTheme();
  const { session, emModoLoja } = useAuth();
  const isAdmin = isAdminSession(session);
  const lojaId = session?.perfil?.lojaId || session?.perfil?.lojaVinculadaId || null;
  const modoLojistaAtivo = Boolean(emModoLoja && lojaId);
  const chipsSugestao = modoLojistaAtivo ? CHIPS_LOJISTA : CHIPS_CLIENTE;

  const [mensagens, setMensagens] = useState([
    {
      id: 'welcome',
      papel: 'ia',
      texto: modoLojistaAtivo
        ? 'Olá! Sou o assistente do Preço Certo (Modo Lojista). Posso comparar seus preços com a concorrência e analisar ofertas do mercado.'
        : 'Olá! Sou o assistente do Preço Certo. Pergunte sobre produtos, cesta básica, lojas próximas e ofertas.',
    },
  ]);
  const [texto, setTexto] = useState('');
  const [enviando, setEnviando] = useState(false);
  const [erro, setErro] = useState(null);
  const listRef = useRef(null);

  async function enviarMensagemTexto(perguntaBruta) {
    const pergunta = String(perguntaBruta || '').trim();
    if (!pergunta || enviando) return;

    setErro(null);
    setTexto('');
    const historicoAtual = [...mensagens];
    setMensagens((prev) => [...prev, { id: idMsg(), papel: 'user', texto: pergunta }]);
    setEnviando(true);

    try {
      const data = await enviarPerguntaIA(pergunta, {
        incluirLocalizacao: true,
        usuarioId: session?.perfil?.id || null,
        lojaId: modoLojistaAtivo ? lojaId : null,
        modoOperacional: modoLojistaAtivo ? 'loja' : 'cliente',
        historico: historicoAtual,
      });
      const resposta = data?.resposta || 'Não foi possível obter uma resposta.';
      setMensagens((prev) => [
        ...prev,
        {
          id: idMsg(),
          papel: 'ia',
          texto: resposta,
          intencao: data?.intencao,
          objetivo: data?.objetivo,
          confianca: data?.confianca,
          provedorLlm: data?.provedorLlm,
          resultados: data?.resultados,
          motivos: data?.motivos,
          fallbacks: data?.fallbacks,
        },
      ]);
    } catch (error) {
      const status = error?.response?.status;
      let msg = 'Não foi possível falar com o assistente. Tente novamente.';
      if (status === 429) msg = 'Muitas perguntas em pouco tempo. Aguarde um momento.';
      else if (error?.response?.data?.message) msg = error.response.data.message;
      setErro(msg);
      setMensagens((prev) => [...prev, { id: idMsg(), papel: 'ia', texto: msg }]);
    } finally {
      setEnviando(false);
      requestAnimationFrame(() => listRef.current?.scrollToEnd?.({ animated: true }));
    }
  }

  function handleEnviar() {
    enviarMensagemTexto(texto);
  }

  function handleChipPress(chipTexto) {
    const limpo = chipTexto.replace(/^[^\p{L}\p{N}]+/u, '').trim();
    enviarMensagemTexto(limpo);
  }

  const handleKeyPress = (e) => {
    if (Platform.OS === 'web' && e.nativeEvent?.key === 'Enter' && !e.nativeEvent?.shiftKey) {
      e.preventDefault?.();
      handleEnviar();
    }
  };

  return (
    <FormScreen
      title={
        isAdmin
          ? 'Assistente Preço Certo (admin)'
          : modoLojistaAtivo
            ? 'Assistente Preço Certo (Lojista)'
            : 'Assistente Preço Certo'
      }
      onBack={() => navigation.goBack()}
      scrollable={false}
    >
      <View style={{ marginBottom: 8 }}>
        <ScrollView
          horizontal
          showsHorizontalScrollIndicator={false}
          contentContainerStyle={{ gap: 8, paddingVertical: 4 }}
        >
          {chipsSugestao.map((chip) => (
            <Pressable
              key={chip}
              disabled={enviando}
              onPress={() => handleChipPress(chip)}
              style={{
                backgroundColor: colors.surface,
                borderWidth: 1,
                borderColor: colors.border,
                borderRadius: 16,
                paddingHorizontal: 12,
                paddingVertical: 6,
                opacity: enviando ? 0.6 : 1,
              }}
            >
              <Text style={{ fontSize: 12, color: colors.text, fontWeight: '600' }}>{chip}</Text>
            </Pressable>
          ))}
        </ScrollView>
      </View>

      <FlatList
        ref={listRef}
        data={mensagens}
        keyExtractor={(item) => item.id}
        style={{ flex: 1, marginBottom: 8 }}
        onContentSizeChange={() => listRef.current?.scrollToEnd?.({ animated: true })}
        renderItem={({ item }) => {
          const minha = item.papel === 'user';
          return (
            <View
              style={{
                alignSelf: minha ? 'flex-end' : 'flex-start',
                backgroundColor: minha ? colors.primary : colors.surface,
                borderWidth: minha ? 0 : 1,
                borderColor: colors.border,
                padding: 10,
                borderRadius: 12,
                marginBottom: 8,
                maxWidth: '88%',
              }}
            >
              <Text style={{ fontSize: 11, color: minha ? 'rgba(255,255,255,0.8)' : colors.textMuted, marginBottom: 2 }}>
                {minha ? 'Você' : 'IA'}
              </Text>
              <Text style={{ color: minha ? '#fff' : colors.text }}>{item.texto}</Text>
              {!minha && Array.isArray(item.resultados) && item.resultados.length > 0 ? (
                <ResultadoCardsList
                  resultados={item.resultados}
                  colors={colors}
                  navigation={navigation}
                />
              ) : null}
              {!minha && isAdmin && item.intencao ? (
                <AdminDebugPanel item={item} colors={colors} />
              ) : null}
            </View>
          );
        }}
        ListFooterComponent={
          enviando ? (
            <View style={{ paddingVertical: 8, alignItems: 'flex-start' }}>
              <ActivityIndicator color={colors.primary} />
            </View>
          ) : null
        }
      />

      {erro ? (
        <Text style={{ color: colors.danger || '#DC2626', marginBottom: 6, fontSize: 12 }}>{erro}</Text>
      ) : null}

      <KeyboardAvoidingView behavior={Platform.OS === 'ios' ? 'padding' : undefined}>
        <View style={{ flexDirection: 'row', gap: 8, alignItems: 'center' }}>
          <TextInput
            value={texto}
            onChangeText={setTexto}
            placeholder="Digite sua pergunta..."
            placeholderTextColor={colors.textMuted}
            editable={!enviando}
            maxLength={500}
            returnKeyType="send"
            onSubmitEditing={handleEnviar}
            onKeyPress={handleKeyPress}
            blurOnSubmit={false}
            accessibilityLabel="Digite sua pergunta para o assistente"
            style={{
              flex: 1,
              borderWidth: 1,
              borderColor: colors.border,
              borderRadius: 10,
              paddingHorizontal: 12,
              paddingVertical: 10,
              color: colors.text,
              backgroundColor: colors.surface,
            }}
          />
          <Pressable
            onPress={handleEnviar}
            disabled={enviando || !texto.trim()}
            accessibilityRole="button"
            accessibilityLabel="Enviar pergunta"
            accessibilityState={{ disabled: enviando || !texto.trim() }}
            style={{
              backgroundColor: colors.primary,
              paddingHorizontal: 16,
              paddingVertical: 12,
              borderRadius: 10,
              opacity: enviando || !texto.trim() ? 0.6 : 1,
            }}
          >
            <Text style={{ color: '#fff', fontWeight: '700' }}>➤</Text>
          </Pressable>
        </View>
      </KeyboardAvoidingView>
    </FormScreen>
  );
}
