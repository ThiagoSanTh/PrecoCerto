import { View, Text, FlatList, TextInput, Pressable, ActivityIndicator, KeyboardAvoidingView, Platform } from 'react-native';
import { useRef, useState } from 'react';
import { FormScreen } from '../../components/form';
import { useAuth } from '../../context/AuthContext';
import { useTheme } from '../../context/ThemeContext';
import { enviarPerguntaIA, isAdminSession, perguntaPrecisaLocalizacao } from '../../services/iaService';

function idMsg() {
  return `${Date.now()}-${Math.random().toString(36).slice(2, 8)}`;
}

function formatPreco(valor) {
  if (valor == null || Number.isNaN(Number(valor))) return null;
  return `R$ ${Number(valor).toFixed(2).replace('.', ',')}`;
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
      <Text style={{ fontSize: 11, fontWeight: '700', color: colors.textMuted }}>MotorIA (admin)</Text>
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
  const { session } = useAuth();
  const isAdmin = isAdminSession(session);
  const [mensagens, setMensagens] = useState([
    {
      id: 'welcome',
      papel: 'ia',
      texto:
        'Olá! Sou o assistente do Preço Certo. Pergunte sobre produtos, lojas, preços e ofertas.',
    },
  ]);
  const [texto, setTexto] = useState('');
  const [enviando, setEnviando] = useState(false);
  const [erro, setErro] = useState(null);
  const listRef = useRef(null);

  async function handleEnviar() {
    const pergunta = texto.trim();
    if (!pergunta || enviando) return;

    setErro(null);
    setTexto('');
    setMensagens((prev) => [...prev, { id: idMsg(), papel: 'user', texto: pergunta }]);
    setEnviando(true);

    try {
      const precisaLoc = perguntaPrecisaLocalizacao(pergunta);
      const data = await enviarPerguntaIA(pergunta, {
        incluirLocalizacao: precisaLoc || isAdmin,
        usuarioId: session?.perfil?.id || null,
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

  return (
    <FormScreen
      title={isAdmin ? 'Assistente Preço Certo (admin)' : 'Assistente Preço Certo'}
      onBack={() => navigation.goBack()}
      scrollable={false}
    >
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
                maxWidth: '85%',
              }}
            >
              <Text style={{ fontSize: 11, color: minha ? 'rgba(255,255,255,0.8)' : colors.textMuted, marginBottom: 2 }}>
                {minha ? 'Você' : 'IA'}
              </Text>
              <Text style={{ color: minha ? '#fff' : colors.text }}>{item.texto}</Text>
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
