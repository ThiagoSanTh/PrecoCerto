import {
  View,
  Text,
  TextInput,
  Alert,
  FlatList,
  Pressable,
  ActivityIndicator,
  RefreshControl,
  StyleSheet,
} from 'react-native';
import { useCallback, useEffect, useMemo, useState } from 'react';
import { useFocusEffect } from '@react-navigation/native';
import { Ionicons } from '@expo/vector-icons';
import {
  listarConversas,
  listarConversasComStale,
} from '../../services/chatService';
import { FormScreen } from '../../components/form';
import { useAuth } from '../../context/AuthContext';
import { useTheme } from '../../context/ThemeContext';
import { useChatBadge } from '../../context/ChatBadgeContext';
import { useLayoutProfile } from '../../hooks/useLayoutProfile';
import { ChatThreadPanel } from './ChatScreen';
import { fonts, radii } from '../../theme';

function formatarHorarioConversa(iso) {
  if (!iso) return '';
  const data = new Date(iso);
  if (Number.isNaN(data.getTime())) return '';
  const agora = new Date();
  const mesmoDia =
    data.getFullYear() === agora.getFullYear() &&
    data.getMonth() === agora.getMonth() &&
    data.getDate() === agora.getDate();

  if (mesmoDia) {
    return data.toLocaleTimeString('pt-BR', {
      hour: '2-digit',
      minute: '2-digit',
    });
  }
  return data.toLocaleDateString('pt-BR', {
    day: '2-digit',
    month: '2-digit',
  });
}

export default function ConversasScreen({ navigation }) {
  const [conversas, setConversas] = useState([]);
  const [busca, setBusca] = useState('');
  const [ativaCodigo, setAtivaCodigo] = useState(null);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const { emModoLoja } = useAuth();
  const { colors } = useTheme();
  const { atualizarFromConversas } = useChatBadge();
  const { useSidebarNav } = useLayoutProfile();

  const carregar = useCallback(
    async (force = false) => {
      try {
        const result = force
          ? { data: await listarConversas({ force: true }) }
          : await listarConversasComStale();
        const lista = Array.isArray(result.data) ? result.data : [];
        setConversas(lista);
        atualizarFromConversas(lista);
      } catch (error) {
        const status = error?.response?.status;
        if (status !== 429) {
          Alert.alert('Erro', 'Não foi possível carregar as conversas.');
        }
      }
    },
    [atualizarFromConversas]
  );

  useFocusEffect(
    useCallback(() => {
      let ativo = true;
      async function init() {
        setLoading(true);
        await carregar(false);
        if (ativo) setLoading(false);
      }
      init();
      return () => {
        ativo = false;
      };
    }, [carregar])
  );

  useEffect(() => {
    if (conversas.length === 0) {
      setAtivaCodigo(null);
      return;
    }
    const existe = conversas.some((c) => c.codigoPublico === ativaCodigo);
    if (!existe) {
      setAtivaCodigo(conversas[0].codigoPublico);
    }
  }, [conversas, ativaCodigo]);

  const conversasFiltradas = useMemo(() => {
    const q = busca.trim().toLowerCase();
    if (!q) return conversas;
    return conversas.filter((c) => {
      const nome = String(c.nomeContato || '').toLowerCase();
      const msg = String(c.ultimaMensagemTexto || '').toLowerCase();
      return nome.includes(q) || msg.includes(q);
    });
  }, [conversas, busca]);

  const conversaAtiva = useMemo(
    () =>
      conversas.find((c) => c.codigoPublico === ativaCodigo) ||
      conversasFiltradas[0] ||
      null,
    [conversas, conversasFiltradas, ativaCodigo]
  );

  const handleNovaMensagemLocal = useCallback((codigo, msg) => {
    setConversas((prev) =>
      prev.map((c) =>
        c.codigoPublico === codigo
          ? {
              ...c,
              ultimaMensagemTexto: msg?.texto || c.ultimaMensagemTexto,
              ultimaMensagemEm: msg?.enviadaEm || c.ultimaMensagemEm,
              naoLidas: 0,
            }
          : c
      )
    );
  }, []);

  async function onRefresh() {
    setRefreshing(true);
    await carregar(true);
    setRefreshing(false);
  }

  function handleSelectConversa(item) {
    setAtivaCodigo(item.codigoPublico);
    setConversas((prev) =>
      prev.map((c) =>
        c.codigoPublico === item.codigoPublico ? { ...c, naoLidas: 0 } : c
      )
    );

    if (!useSidebarNav) {
      navigation.navigate('Chat', {
        conversaCodigo: item.codigoPublico,
        titulo: item.nomeContato || 'Conversa',
      });
    }
  }

  function renderListaColuna() {
    return (
      <View
        style={[
          styles.leftCol,
          { backgroundColor: colors.card },
          useSidebarNav && [
            styles.leftColSplit,
            { borderRightColor: colors.border },
          ],
        ]}
      >
        <View style={styles.listHeader}>
          <Text style={[styles.listTitle, { color: colors.text }]}>
            Mensagens
          </Text>
          <View
            style={[
              styles.searchWrap,
              { backgroundColor: colors.surfaceSubtle },
            ]}
          >
            <Ionicons
              name="search-outline"
              size={16}
              color={colors.textMuted}
            />
            <TextInput
              value={busca}
              onChangeText={setBusca}
              placeholder="Buscar conversa"
              placeholderTextColor={colors.placeholder}
              style={[styles.searchInput, { color: colors.text }]}
            />
            {busca.length > 0 ? (
              <Pressable onPress={() => setBusca('')} hitSlop={6}>
                <Ionicons
                  name="close-circle"
                  size={16}
                  color={colors.textMuted}
                />
              </Pressable>
            ) : null}
          </View>
        </View>

        {loading ? (
          <View style={styles.loaderCenter}>
            <ActivityIndicator color={colors.primary} />
          </View>
        ) : (
          <FlatList
            data={conversasFiltradas}
            keyExtractor={(item) => item.codigoPublico}
            refreshControl={
              <RefreshControl refreshing={refreshing} onRefresh={onRefresh} />
            }
            contentContainerStyle={styles.listContent}
            ListEmptyComponent={
              <Text style={[styles.emptyText, { color: colors.textMuted }]}>
                {busca.trim()
                  ? 'Nenhuma conversa corresponde à busca.'
                  : 'Nenhuma conversa ainda. Abra o chat em uma loja ou produto.'}
              </Text>
            }
            renderItem={({ item }) => {
              const selecionada =
                useSidebarNav &&
                conversaAtiva?.codigoPublico === item.codigoPublico;
              const horario = formatarHorarioConversa(item.ultimaMensagemEm);
              const previa =
                item.ultimaMensagemTexto ||
                (item.ultimaMensagemEm
                  ? `Atualizada em ${new Date(
                      item.ultimaMensagemEm
                    ).toLocaleDateString('pt-BR')}`
                  : 'Sem mensagens ainda');
              const naoLidas = Number(item.naoLidas || 0);

              return (
                <Pressable
                  onPress={() => handleSelectConversa(item)}
                  style={[
                    styles.conversaItem,
                    selecionada && {
                      backgroundColor: colors.primarySoft,
                    },
                  ]}
                >
                  <View
                    style={[
                      styles.avatarCircle,
                      { backgroundColor: colors.ink },
                    ]}
                  >
                    <Ionicons
                      name={
                        emModoLoja ? 'person-outline' : 'storefront-outline'
                      }
                      size={17}
                      color={colors.inkForeground}
                    />
                  </View>

                  <View style={styles.conversaBody}>
                    <View style={styles.conversaTopRow}>
                      <Text
                        style={[styles.conversaName, { color: colors.text }]}
                        numberOfLines={1}
                      >
                        {item.nomeContato || 'Contato'}
                      </Text>
                      {horario ? (
                        <Text
                          style={[
                            styles.conversaTime,
                            { color: colors.textMuted },
                          ]}
                        >
                          {horario}
                        </Text>
                      ) : null}
                    </View>

                    <View style={styles.conversaBottomRow}>
                      <Text
                        style={[
                          styles.conversaPreview,
                          { color: colors.textMuted },
                        ]}
                        numberOfLines={1}
                      >
                        {previa}
                      </Text>
                      {naoLidas > 0 ? (
                        <View
                          style={[
                            styles.unreadBadge,
                            { backgroundColor: colors.primary },
                          ]}
                        >
                          <Text
                            style={[
                              styles.unreadText,
                              { color: colors.primaryForeground },
                            ]}
                          >
                            {naoLidas > 99 ? '99+' : String(naoLidas)}
                          </Text>
                        </View>
                      ) : null}
                    </View>
                  </View>
                </Pressable>
              );
            }}
          />
        )}
      </View>
    );
  }

  return (
    <FormScreen
      title="Mensagens"
      hideHeader
      fullBleed
      noBodyPadding
      scrollable={false}
    >
      {useSidebarNav ? (
        <View style={styles.splitRow}>
          {renderListaColuna()}
          <View style={styles.rightCol}>
            {conversaAtiva ? (
              <ChatThreadPanel
                conversaCodigo={conversaAtiva.codigoPublico}
                titulo={conversaAtiva.nomeContato}
                onNovaMensagemLocal={handleNovaMensagemLocal}
              />
            ) : (
              <View style={styles.emptyDetailPane}>
                <Ionicons
                  name="chatbubbles-outline"
                  size={36}
                  color={colors.textMuted}
                />
                <Text
                  style={[styles.emptyDetailText, { color: colors.textMuted }]}
                >
                  Selecione uma conversa ao lado para visualizar as mensagens.
                </Text>
              </View>
            )}
          </View>
        </View>
      ) : (
        renderListaColuna()
      )}
    </FormScreen>
  );
}

const styles = StyleSheet.create({
  splitRow: {
    flex: 1,
    flexDirection: 'row',
  },
  leftCol: {
    flex: 1,
  },
  leftColSplit: {
    width: 340,
    maxWidth: 340,
    flexGrow: 0,
    flexShrink: 0,
    borderRightWidth: 1,
  },
  rightCol: {
    flex: 1,
  },
  listHeader: {
    padding: 20,
    gap: 14,
  },
  listTitle: {
    fontFamily: fonts.display,
    fontSize: 24,
    fontWeight: '700',
    letterSpacing: -0.4,
  },
  searchWrap: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 8,
    borderRadius: radii.md,
    paddingHorizontal: 12,
    paddingVertical: 9,
  },
  searchInput: {
    flex: 1,
    fontFamily: fonts.sans,
    fontSize: 13.5,
    paddingVertical: 2,
    minWidth: 0,
  },
  loaderCenter: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
  },
  listContent: {
    paddingBottom: 90,
  },
  conversaItem: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 12,
    paddingHorizontal: 20,
    paddingVertical: 14,
  },
  avatarCircle: {
    width: 44,
    height: 44,
    borderRadius: 22,
    alignItems: 'center',
    justifyContent: 'center',
  },
  conversaBody: {
    flex: 1,
    minWidth: 0,
    gap: 3,
  },
  conversaTopRow: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    gap: 8,
  },
  conversaName: {
    fontFamily: fonts.sansSemiBold,
    fontSize: 14,
    fontWeight: '600',
    flex: 1,
  },
  conversaTime: {
    fontFamily: fonts.sans,
    fontSize: 11,
  },
  conversaBottomRow: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    gap: 8,
  },
  conversaPreview: {
    fontFamily: fonts.sans,
    fontSize: 12.5,
    flex: 1,
  },
  unreadBadge: {
    minWidth: 20,
    height: 20,
    borderRadius: 10,
    paddingHorizontal: 5,
    alignItems: 'center',
    justifyContent: 'center',
  },
  unreadText: {
    fontFamily: fonts.sansBold,
    fontSize: 10,
    fontWeight: '700',
  },
  emptyText: {
    fontFamily: fonts.sans,
    fontSize: 13.5,
    textAlign: 'center',
    marginTop: 28,
    paddingHorizontal: 20,
  },
  emptyDetailPane: {
    flex: 1,
    alignItems: 'center',
    justifyContent: 'center',
    padding: 32,
    gap: 10,
  },
  emptyDetailText: {
    fontFamily: fonts.sans,
    fontSize: 14,
    textAlign: 'center',
    maxWidth: 320,
  },
});
