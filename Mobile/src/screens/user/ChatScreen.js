import { View, Text, FlatList, TextInput, Pressable, Alert, KeyboardAvoidingView, Platform } from 'react-native';
import { useEffect, useRef, useState } from 'react';
import {
  listarMensagens,
  enviarMensagem,
  conectarChatHub,
} from '../../services/chatService';
import { useAuth } from '../../context/AuthContext';
import { useTheme } from '../../context/ThemeContext';
import { FormScreen } from '../../components/form';

const PAPEL_POR_TIPO = { cliente: 1, lojista: 2, vendedor: 3 };

function isMinhaMensagem(item, session) {
  const meuCodigo = session?.perfil?.codigoPublico;
  if (meuCodigo && item.codigoRemetente === meuCodigo) return true;

  const meuPapel = PAPEL_POR_TIPO[session?.tipo];
  return meuPapel != null && item.remetentePapel === meuPapel;
}

export default function ChatScreen({ route, navigation }) {
  const { conversaCodigo, titulo } = route.params;
  const { session } = useAuth();
  const { colors } = useTheme();
  const [mensagens, setMensagens] = useState([]);
  const [texto, setTexto] = useState('');
  const [enviando, setEnviando] = useState(false);
  const ultimaDataRef = useRef(null);
  const hubRef = useRef(null);
  const pollRef = useRef(null);

  async function carregarMensagens() {
    const apos = ultimaDataRef.current;
    const novas = await listarMensagens(conversaCodigo, apos ? { apos } : {});
    if (!novas?.length) return;
    setMensagens((prev) => {
      const ids = new Set(prev.map((m) => m.codigoPublico));
      const merged = [...prev];
      novas.forEach((m) => {
        if (!ids.has(m.codigoPublico)) merged.push(m);
      });
      return merged.sort((a, b) => new Date(a.enviadaEm) - new Date(b.enviadaEm));
    });
    ultimaDataRef.current = novas[novas.length - 1].enviadaEm;
  }

  useEffect(() => {
    let ativo = true;

    async function iniciar() {
      try {
        await carregarMensagens();
      } catch {
        if (ativo) Alert.alert('Erro', 'Não foi possível carregar as mensagens.');
      }

      hubRef.current = await conectarChatHub(conversaCodigo, (msg) => {
        setMensagens((prev) => {
          if (prev.some((m) => m.codigoPublico === msg.codigoPublico)) return prev;
          return [...prev, msg].sort((a, b) => new Date(a.enviadaEm) - new Date(b.enviadaEm));
        });
        ultimaDataRef.current = msg.enviadaEm;
      });

      if (!hubRef.current && ativo) {
        pollRef.current = setInterval(() => {
          carregarMensagens().catch(() => {});
        }, 5000);
      }
    }

    iniciar();
    return () => {
      ativo = false;
      if (pollRef.current) clearInterval(pollRef.current);
      hubRef.current?.stop?.();
    };
  }, [conversaCodigo]);

  async function handleEnviar() {
    if (!texto.trim()) return;
    setEnviando(true);
    try {
      const msg = await enviarMensagem(conversaCodigo, texto.trim());
      setTexto('');
      setMensagens((prev) => {
        if (prev.some((m) => m.codigoPublico === msg.codigoPublico)) return prev;
        return [...prev, msg];
      });
      ultimaDataRef.current = msg.enviadaEm;
    } catch (error) {
      Alert.alert('Erro', 'Não foi possível enviar a mensagem.');
    } finally {
      setEnviando(false);
    }
  }

  return (
    <FormScreen title={titulo || 'Chat'} onBack={() => navigation.goBack()} scrollable={false}>
      <FlatList
        data={mensagens}
        keyExtractor={(item) => item.codigoPublico}
        style={{ flex: 1, marginBottom: 8 }}
        renderItem={({ item }) => {
          const minha = isMinhaMensagem(item, session);
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
                maxWidth: '80%',
              }}
            >
              <Text style={{ color: minha ? '#fff' : colors.text }}>{item.texto}</Text>
            </View>
          );
        }}
      />
      <KeyboardAvoidingView behavior={Platform.OS === 'ios' ? 'padding' : undefined}>
        <View style={{ flexDirection: 'row', gap: 8, alignItems: 'center' }}>
          <TextInput
            value={texto}
            onChangeText={setTexto}
            placeholder="Digite sua mensagem..."
            placeholderTextColor={colors.textMuted}
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
            disabled={enviando}
            style={{
              backgroundColor: colors.primary,
              paddingHorizontal: 16,
              paddingVertical: 12,
              borderRadius: 10,
            }}
          >
            <Text style={{ color: '#fff', fontWeight: '700' }}>Enviar</Text>
          </Pressable>
        </View>
      </KeyboardAvoidingView>
    </FormScreen>
  );
}
