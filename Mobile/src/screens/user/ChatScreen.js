import {
  View,
  Text,
  FlatList,
  TextInput,
  Pressable,
  Alert,
  KeyboardAvoidingView,
  Platform,
  StyleSheet,
} from 'react-native';
import { useCallback, useEffect, useRef, useState } from 'react';
import { Ionicons } from '@expo/vector-icons';
import {
  listarMensagens,
  enviarMensagem,
  entrarConversaHub,
  sairConversaHub,
  hubEstaConectado,
  CHAT_POLL_INTERVAL_MS,
} from '../../services/chatService';
import { useAuth } from '../../context/AuthContext';
import { useChatBadge } from '../../context/ChatBadgeContext';
import { useTheme } from '../../context/ThemeContext';
import { FormScreen } from '../../components/form';
import { fontSizeInputWeb, touchTarget } from '../../components/form/formStyles';
import { fonts, radii, shadows } from '../../theme';
import {
  enterEnviaMensagem,
  instanteMensagem,
  itensComSeparadorDeDia,
  ordenarMensagens,
  valorAtualDoInput,
} from '../../utils/chatThread';

const PAPEL_POR_TIPO = { cliente: 1, lojista: 2, vendedor: 3 };

function isErroSilencioso(error) {
  if (error?.lojaClaim) return true;
  const status = error?.response?.status;
  return !status || status === 429 || status >= 500;
}

function isMinhaMensagem(item, session, emModoLoja) {
  const meuCodigo = session?.perfil?.codigoPublico;
  if (meuCodigo && item.codigoRemetente === meuCodigo) return true;

  const meuId = session?.perfil?.id;
  if (meuId && String(item.remetenteId) === String(meuId)) return true;

  const meuPapel = emModoLoja
    ? PAPEL_POR_TIPO[session?.tipo]
    : PAPEL_POR_TIPO.cliente;
  return meuPapel != null && item.remetentePapel === meuPapel;
}

function formatarHoraMsg(msg) {
  const ms = instanteMensagem(msg);
  if (!ms) return '';
  return new Date(ms).toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' });
}

export function ChatThreadPanel({
  conversaCodigo,
  titulo,
  onBack,
  onNovaMensagemLocal,
}) {
  const { session, emModoLoja } = useAuth();
  const { setChatAtivo, syncBadge, hubConectado } = useChatBadge();
  const { colors } = useTheme();
  const [mensagens, setMensagens] = useState([]);
  const [texto, setTexto] = useState('');
  const [enviando, setEnviando] = useState(false);
  const textoRef = useRef('');
  const enviandoRef = useRef(false);
  const ultimaDataRef = useRef(null);
  const pollRef = useRef(null);
  const listRef = useRef(null);

  const aplicarMensagem = useCallback(
    (msg) => {
      if (!msg?.codigoPublico) return;
      setMensagens((prev) => {
        if (prev.some((m) => m.codigoPublico === msg.codigoPublico)) return prev;
        return ordenarMensagens([...prev, msg]);
      });
      if (msg.enviadaEm) ultimaDataRef.current = msg.enviadaEm;
      onNovaMensagemLocal?.(conversaCodigo, msg);
    },
    [conversaCodigo, onNovaMensagemLocal]
  );

  function pararPoll() {
    if (pollRef.current) {
      clearInterval(pollRef.current);
      pollRef.current = null;
    }
  }

  function iniciarPoll() {
    if (pollRef.current) return;
    pollRef.current = setInterval(() => {
      carregarMensagens().catch(() => {});
    }, CHAT_POLL_INTERVAL_MS);
  }

  async function carregarMensagens(reset = false) {
    if (!conversaCodigo) return;
    const apos = reset ? null : ultimaDataRef.current;
    const novas = await listarMensagens(conversaCodigo, apos ? { apos } : {});
    if (!novas?.length) {
      if (reset) setMensagens([]);
      return;
    }
    setMensagens((prev) => {
      const base = reset ? [] : prev;
      const ids = new Set(base.map((m) => m.codigoPublico));
      const merged = [...base];
      novas.forEach((m) => {
        if (!ids.has(m.codigoPublico)) merged.push(m);
      });
      return ordenarMensagens(merged);
    });
    const ordenadas = ordenarMensagens(novas);
    const ultima = ordenadas[ordenadas.length - 1];
    ultimaDataRef.current = ultima?.enviadaEm ?? ultima?.EnviadaEm ?? null;
  }

  useEffect(() => {
    if (!conversaCodigo) return undefined;
    setChatAtivo(true, conversaCodigo);
    return () => {
      setChatAtivo(false);
      syncBadge();
    };
  }, [conversaCodigo, setChatAtivo, syncBadge]);

  useEffect(() => {
    let ativo = true;
    ultimaDataRef.current = null;
    setMensagens([]);

    async function carregarInicial() {
      if (!conversaCodigo) return;
      try {
        await carregarMensagens(true);
      } catch (error) {
        if (ativo && !isErroSilencioso(error)) {
          Alert.alert('Erro', 'Não foi possível carregar as mensagens.');
        }
      }
    }

    carregarInicial();
    return () => {
      ativo = false;
    };
  }, [conversaCodigo]);

  useEffect(() => {
    if (!conversaCodigo) return undefined;
    let ativo = true;

    async function syncHub() {
      const hubOk = await entrarConversaHub(conversaCodigo, aplicarMensagem);
      if (!ativo) return;

      if (hubOk && hubEstaConectado()) {
        pararPoll();
      } else {
        iniciarPoll();
      }
    }

    syncHub();
    return () => {
      ativo = false;
      pararPoll();
      sairConversaHub();
    };
  }, [conversaCodigo, hubConectado, aplicarMensagem]);

  function definirTexto(valor) {
    textoRef.current = valor;
    setTexto(valor);
  }

  async function enviarTexto(bruto) {
    const valor = String(bruto ?? '').trim();
    if (!valor || !conversaCodigo || enviandoRef.current) return;
    enviandoRef.current = true;
    definirTexto('');
    setEnviando(true);
    try {
      const msg = await enviarMensagem(conversaCodigo, valor);
      aplicarMensagem(msg);
      requestAnimationFrame(() =>
        listRef.current?.scrollToEnd?.({ animated: true })
      );
    } catch (error) {
      definirTexto(valor);
      if (!isErroSilencioso(error)) {
        Alert.alert('Erro', 'Não foi possível enviar a mensagem.');
      }
    } finally {
      enviandoRef.current = false;
      setEnviando(false);
    }
  }

  function handleEnviar() {
    enviarTexto(textoRef.current);
  }

  const handleKeyPress = (e) => {
    if (Platform.OS !== 'web' || !enterEnviaMensagem(e.nativeEvent)) return;
    e.preventDefault?.();
    const domValue = e?.target?.value ?? e.nativeEvent?.target?.value;
    enviarTexto(valorAtualDoInput(textoRef.current, domValue));
  };

  return (
    <View style={[styles.threadWrap, { backgroundColor: colors.background }]}>
      {/* Thread Header */}
      <View
        style={[
          styles.threadHeader,
          { backgroundColor: colors.card, borderBottomColor: colors.border },
        ]}
      >
        {onBack ? (
          <Pressable
            onPress={onBack}
            style={[
              styles.backBtn,
              touchTarget,
              { borderColor: colors.border, backgroundColor: colors.surface },
            ]}
            accessibilityRole="button"
            accessibilityLabel="Voltar para conversas"
          >
            <Ionicons name="arrow-back" size={18} color={colors.text} />
          </Pressable>
        ) : null}

        <View
          style={[styles.contactAvatar, { backgroundColor: colors.ink }]}
        >
          <Ionicons
            name={emModoLoja ? 'person-outline' : 'storefront-outline'}
            size={17}
            color={colors.inkForeground}
          />
        </View>

        <View style={{ flex: 1, minWidth: 0 }}>
          <Text
            style={[styles.contactTitle, { color: colors.text }]}
            numberOfLines={1}
          >
            {titulo || 'Conversa'}
          </Text>
          <Text style={[styles.contactStatus, { color: colors.success }]}>
            Atendimento direto no Preço Certo
          </Text>
        </View>
      </View>

      {/* Messages List */}
      <FlatList
        ref={listRef}
        data={itensComSeparadorDeDia(mensagens)}
        keyExtractor={(item) => item.id}
        style={styles.messagesList}
        contentContainerStyle={styles.messagesContent}
        showsVerticalScrollIndicator={false}
        onContentSizeChange={() =>
          listRef.current?.scrollToEnd?.({ animated: true })
        }
        ListEmptyComponent={
          <Text
            style={[styles.emptyThreadText, { color: colors.textMuted }]}
          >
            Envie uma mensagem para iniciar a conversa.
          </Text>
        }
        renderItem={({ item }) => {
          if (item.tipo === 'dia') {
            return (
              <View style={styles.diaSeparador} accessibilityRole="header">
                <Text style={[styles.diaSeparadorTexto, { color: colors.textMuted }]}>
                  {item.label}
                </Text>
              </View>
            );
          }
          const msg = item.msg;
          const minha = isMinhaMensagem(msg, session, emModoLoja);
          const hora = formatarHoraMsg(msg);
          return (
            <View
              style={[
                styles.msgRow,
                minha ? styles.msgRowSent : styles.msgRowReceived,
              ]}
            >
              <View
                style={[
                  styles.msgBubble,
                  minha
                    ? [
                        styles.msgBubbleSent,
                        { backgroundColor: colors.ink },
                      ]
                    : [
                        styles.msgBubbleReceived,
                        {
                          backgroundColor: colors.card,
                          borderColor: colors.border,
                        },
                      ],
                ]}
              >
                <Text
                  style={[
                    styles.msgText,
                    {
                      color: minha ? colors.inkForeground : colors.text,
                    },
                  ]}
                >
                  {msg.texto}
                </Text>
                {hora ? (
                  <Text
                    style={[
                      styles.msgTime,
                      {
                        color: minha ? colors.inkMuted : colors.textMuted,
                      },
                    ]}
                  >
                    {hora}
                  </Text>
                ) : null}
              </View>
            </View>
          );
        }}
      />

      {/* Input Footer */}
      <KeyboardAvoidingView
        behavior={Platform.OS === 'ios' ? 'padding' : undefined}
        style={[
          styles.inputFooterWrap,
          { backgroundColor: colors.card, borderTopColor: colors.border },
        ]}
      >
        <View
          style={[
            styles.inputBox,
            {
              borderColor: colors.border,
              backgroundColor: colors.surface,
            },
          ]}
        >
          <TextInput
            value={texto}
            onChangeText={definirTexto}
            placeholder="Escreva uma mensagem…"
            placeholderTextColor={colors.placeholder}
            returnKeyType="send"
            onSubmitEditing={handleEnviar}
            onKeyPress={handleKeyPress}
            blurOnSubmit={false}
            accessibilityLabel="Digite sua mensagem"
            style={[styles.inputField, { color: colors.text, fontSize: fontSizeInputWeb(14) }]}
          />
          <Pressable
            onPress={handleEnviar}
            disabled={enviando || !texto.trim()}
            accessibilityRole="button"
            accessibilityLabel="Enviar mensagem"
            accessibilityState={{ disabled: enviando || !texto.trim() }}
            style={[
              styles.sendBtn,
              touchTarget,
              {
                backgroundColor: colors.primary,
                opacity: enviando || !texto.trim() ? 0.5 : 1,
              },
            ]}
          >
            <Ionicons
              name="send"
              size={16}
              color={colors.primaryForeground}
            />
          </Pressable>
        </View>
      </KeyboardAvoidingView>
    </View>
  );
}

export default function ChatScreen({ route, navigation }) {
  const { conversaCodigo, titulo } = route.params || {};

  return (
    <FormScreen
      title={titulo || 'Chat'}
      hideHeader
      fullBleed
      noBodyPadding
      scrollable={false}
    >
      <ChatThreadPanel
        conversaCodigo={conversaCodigo}
        titulo={titulo}
        onBack={() => navigation.goBack()}
      />
    </FormScreen>
  );
}

const styles = StyleSheet.create({
  threadWrap: {
    flex: 1,
  },
  threadHeader: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 12,
    paddingHorizontal: 20,
    paddingVertical: 14,
    borderBottomWidth: 1,
  },
  diaSeparador: {
    alignItems: 'center',
    marginVertical: 8,
  },
  diaSeparadorTexto: {
    fontFamily: fonts.sansSemiBold,
    fontSize: 12,
    fontWeight: '600',
  },
  backBtn: {
    minWidth: 44,
    minHeight: 44,
    borderRadius: radii.md,
    borderWidth: 1,
    alignItems: 'center',
    justifyContent: 'center',
  },
  contactAvatar: {
    width: 40,
    height: 40,
    borderRadius: 20,
    alignItems: 'center',
    justifyContent: 'center',
  },
  contactTitle: {
    fontFamily: fonts.sansSemiBold,
    fontSize: 15.5,
    fontWeight: '600',
  },
  contactStatus: {
    fontFamily: fonts.sansMedium,
    fontSize: 12,
    marginTop: 1,
  },
  messagesList: {
    flex: 1,
  },
  messagesContent: {
    padding: 20,
    gap: 12,
  },
  emptyThreadText: {
    fontFamily: fonts.sans,
    fontSize: 13.5,
    textAlign: 'center',
    marginTop: 32,
  },
  msgRow: {
    flexDirection: 'row',
    marginBottom: 4,
  },
  msgRowSent: {
    justifyContent: 'flex-end',
  },
  msgRowReceived: {
    justifyContent: 'flex-start',
  },
  msgBubble: {
    maxWidth: '78%',
    paddingHorizontal: 16,
    paddingVertical: 10,
  },
  msgBubbleSent: {
    borderRadius: 22,
    borderBottomRightRadius: 6,
  },
  msgBubbleReceived: {
    borderRadius: 22,
    borderBottomLeftRadius: 6,
    borderWidth: 1,
    ...shadows.soft,
  },
  msgText: {
    fontFamily: fonts.sans,
    fontSize: 14,
    lineHeight: 20,
  },
  msgTime: {
    fontFamily: fonts.sans,
    fontSize: 10.5,
    marginTop: 4,
    alignSelf: 'flex-end',
  },
  inputFooterWrap: {
    paddingHorizontal: 16,
    paddingVertical: 12,
    borderTopWidth: 1,
  },
  inputBox: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 10,
    borderRadius: radii.xl,
    borderWidth: 1,
    paddingLeft: 14,
    paddingRight: 6,
    paddingVertical: 6,
  },
  inputField: {
    flex: 1,
    fontFamily: fonts.sans,
    fontSize: 14,
    paddingVertical: 6,
    minWidth: 0,
  },
  sendBtn: {
    minWidth: 44,
    minHeight: 44,
    borderRadius: radii.md,
    alignItems: 'center',
    justifyContent: 'center',
  },
});
