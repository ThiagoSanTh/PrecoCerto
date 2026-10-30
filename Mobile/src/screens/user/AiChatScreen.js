import {
  View,
  Text,
  FlatList,
  TextInput,
  Pressable,
  ActivityIndicator,
  KeyboardAvoidingView,
  Platform,
  StyleSheet,
} from 'react-native';
import { useRef, useState } from 'react';
import { Ionicons } from '@expo/vector-icons';
import { FormScreen } from '../../components/form';
import { PillBadge } from '../../components/ui/DesignPrimitives';
import { useAuth } from '../../context/AuthContext';
import { useTheme } from '../../context/ThemeContext';
import { enviarPerguntaIA, isAdminSession } from '../../services/iaService';
import { fonts, radii, shadows } from '../../theme';

const CHIPS_CLIENTE = [
  'Onde tem café mais barato perto de mim?',
  'Monte uma cesta básica mais barata',
  'Quais são as promoções de hoje?',
  'Lista de churrasco com menor preço',
];

const CHIPS_LOJISTA = [
  'Meus preços vs concorrência',
  'Promoções do mercado hoje',
  'Lojas concorrentes próximas',
];

function idMsg() {
  return `${Date.now()}-${Math.random().toString(36).slice(2, 8)}`;
}

function formatPreco(valor) {
  if (valor == null || Number.isNaN(Number(valor))) return null;
  return `R$ ${Number(valor).toFixed(2).replace('.', ',')}`;
}

function ResultadoCardsTable({ resultados, colors, navigation }) {
  if (!Array.isArray(resultados) || resultados.length === 0) return null;

  const visiveis = resultados.slice(0, 5);
  const comPreco = visiveis.filter(
    (r) => r.preco != null && !Number.isNaN(Number(r.preco)) && Number(r.preco) > 0
  );
  const somaPrecos = comPreco.reduce((acc, r) => acc + Number(r.preco), 0);
  const lojasUnicas = new Set(visiveis.map((r) => r.loja).filter(Boolean)).size;
  const primeiro = visiveis[0];

  function abrirItem(r) {
    if (r?.produtoId) {
      navigation.navigate('ProductDetail', { productId: String(r.produtoId) });
    } else if (r?.lojaId) {
      navigation.navigate('StoreCatalog', {
        lojaId: String(r.lojaId),
        lojaNome: r.loja || r.titulo || 'Loja',
      });
    }
  }

  return (
    <View
      style={[
        styles.resultTableCard,
        { backgroundColor: colors.card, borderColor: colors.border },
      ]}
    >
      {visiveis.map((r, idx) => {
        const precoFmt = formatPreco(r.preco);
        const distFmt =
          r.distanciaKm != null && !Number.isNaN(Number(r.distanciaKm))
            ? `${Number(r.distanciaKm).toFixed(1).replace('.', ',')} km`
            : null;
        const subtitulo = [r.loja, distFmt].filter(Boolean).join(' · ');

        return (
          <View
            key={`${r.titulo || 'item'}-${idx}`}
            style={[
              styles.resultTableRow,
              idx < visiveis.length - 1 && {
                borderBottomWidth: 1,
                borderBottomColor: colors.border,
              },
            ]}
          >
            <View style={{ flex: 1, minWidth: 0 }}>
              <View style={styles.resultRowTitleWrap}>
                <Text
                  style={[styles.resultItemTitle, { color: colors.text }]}
                  numberOfLines={1}
                >
                  {r.titulo || r.produto || r.loja || 'Opção'}
                </Text>
                {r.emPromocao ? (
                  <PillBadge label="PROMO" variant="highlight" />
                ) : null}
              </View>
              {subtitulo ? (
                <Text
                  style={[styles.resultItemSub, { color: colors.textMuted }]}
                  numberOfLines={1}
                >
                  {subtitulo}
                </Text>
              ) : null}
              {Array.isArray(r.motivos) && r.motivos.length > 0 ? (
                <Text
                  style={[styles.resultItemReason, { color: colors.textMuted }]}
                  numberOfLines={1}
                >
                  {r.motivos.slice(0, 2).join(' · ')}
                </Text>
              ) : null}

              {r.produtoId || r.lojaId ? (
                <View style={styles.resultActionLinks}>
                  {r.produtoId ? (
                    <Pressable
                      onPress={() =>
                        navigation.navigate('ProductDetail', {
                          productId: String(r.produtoId),
                        })
                      }
                      style={[
                        styles.miniActionPill,
                        { backgroundColor: colors.primary },
                      ]}
                    >
                      <Text
                        style={[
                          styles.miniActionPillText,
                          { color: colors.primaryForeground },
                        ]}
                      >
                        Ver Produto
                      </Text>
                    </Pressable>
                  ) : null}
                  {r.lojaId ? (
                    <Pressable
                      onPress={() =>
                        navigation.navigate('StoreCatalog', {
                          lojaId: String(r.lojaId),
                          lojaNome: r.loja || r.titulo || 'Loja',
                        })
                      }
                      style={[
                        styles.miniActionOutline,
                        { borderColor: colors.primary },
                      ]}
                    >
                      <Text
                        style={[
                          styles.miniActionOutlineText,
                          { color: colors.primary },
                        ]}
                      >
                        Ver Loja
                      </Text>
                    </Pressable>
                  ) : null}
                </View>
              ) : null}
            </View>

            {precoFmt ? (
              <Text style={[styles.resultItemPrice, { color: colors.text }]}>
                {precoFmt}
              </Text>
            ) : null}
          </View>
        );
      })}

      <View
        style={[
          styles.resultTableFooter,
          { backgroundColor: colors.primarySoft },
        ]}
      >
        <Text
          style={[
            styles.resultFooterSummary,
            { color: colors.accentForeground },
          ]}
        >
          {comPreco.length > 1
            ? `${comPreco.length} itens · Total ${formatPreco(somaPrecos)}${
                lojasUnicas > 0 ? ` · ${lojasUnicas} loja(s)` : ''
              }`
            : `${visiveis.length} opção(ões) encontrada(s)`}
        </Text>
        {primeiro && (primeiro.produtoId || primeiro.lojaId) ? (
          <Pressable
            onPress={() => abrirItem(primeiro)}
            style={styles.footerCtaBtn}
          >
            <Text style={[styles.footerCtaText, { color: colors.primary }]}>
              Abrir melhor opção
            </Text>
            <Ionicons name="chevron-forward" size={14} color={colors.primary} />
          </Pressable>
        ) : null}
      </View>
    </View>
  );
}

function AdminDebugPanel({ item, colors }) {
  const resultados = Array.isArray(item.resultados) ? item.resultados : [];
  return (
    <View
      style={[
        styles.adminDebugCard,
        {
          borderTopColor: colors.border,
          backgroundColor: colors.surfaceSubtle,
        },
      ]}
    >
      <Text style={[styles.adminDebugTitle, { color: colors.textMuted }]}>
        MotorIA (admin) · Provedor: {item.provedorLlm || 'Deterministico'}
      </Text>
      <Text style={[styles.adminDebugLine, { color: colors.textMuted }]}>
        Intenção: {item.intencao || '—'} · Objetivo: {item.objetivo || '—'} ·
        Confiança:{' '}
        {item.confianca != null ? Number(item.confianca).toFixed(2) : '—'}
      </Text>
      {item.motivos?.length ? (
        <Text style={[styles.adminDebugLine, { color: colors.textMuted }]}>
          Motivos: {item.motivos.join(' · ')}
        </Text>
      ) : null}
      {item.fallbacks?.length ? (
        <Text style={[styles.adminDebugLine, { color: colors.textMuted }]}>
          Fallbacks: {item.fallbacks.join(', ')}
        </Text>
      ) : null}
      {resultados.slice(0, 5).map((r, idx) => (
        <Text
          key={`${r.titulo}-${idx}`}
          style={[styles.adminDebugLine, { color: colors.textMuted }]}
        >
          #{idx + 1} score {Number(r.score ?? 0).toFixed(3)}
          {r.preco != null ? ` · ${formatPreco(r.preco)}` : ''}
          {r.distanciaKm != null ? ` · ${Number(r.distanciaKm).toFixed(1)} km` : ''}
          {r.loja ? ` · ${r.loja}` : ''}
        </Text>
      ))}
    </View>
  );
}

export default function AiChatScreen({ navigation, route }) {
  const { colors } = useTheme();
  const { session, emModoLoja } = useAuth();
  const isAdmin = isAdminSession(session);
  const lojaId = session?.perfil?.lojaId || session?.perfil?.lojaVinculadaId || null;
  const modoLojistaAtivo = Boolean(emModoLoja && lojaId);
  const chipsSugestao = modoLojistaAtivo ? CHIPS_LOJISTA : CHIPS_CLIENTE;
  const isTabRoot = route?.name === 'IA';

  const [mensagens, setMensagens] = useState([
    {
      id: 'welcome',
      papel: 'ia',
      texto: modoLojistaAtivo
        ? 'Olá! Sou o assistente do Preço Certo (Modo Lojista). Posso comparar seus preços com a concorrência e analisar ofertas do mercado em tempo real.'
        : 'Olá! Sou o assistente do Preço Certo. Pergunte sobre produtos, cesta básica, itens para churrasco ou lojas mais baratas perto de você.',
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
    setMensagens((prev) => [
      ...prev,
      { id: idMsg(), papel: 'user', texto: pergunta },
    ]);
    setEnviando(true);

    try {
      const data = await enviarPerguntaIA(pergunta, {
        incluirLocalizacao: true,
        usuarioId: session?.perfil?.id || null,
        lojaId: modoLojistaAtivo ? lojaId : null,
        modoOperacional: modoLojistaAtivo ? 'loja' : 'cliente',
        historico: historicoAtual,
      });
      const resposta =
        data?.resposta || 'Não foi possível obter uma resposta.';
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
      if (status === 429) {
        msg = 'Muitas perguntas em pouco tempo. Aguarde um momento.';
      } else if (error?.response?.data?.message) {
        msg = error.response.data.message;
      }
      setErro(msg);
      setMensagens((prev) => [
        ...prev,
        { id: idMsg(), papel: 'ia', texto: msg },
      ]);
    } finally {
      setEnviando(false);
      requestAnimationFrame(() =>
        listRef.current?.scrollToEnd?.({ animated: true })
      );
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
    if (
      Platform.OS === 'web' &&
      e.nativeEvent?.key === 'Enter' &&
      !e.nativeEvent?.shiftKey
    ) {
      e.preventDefault?.();
      handleEnviar();
    }
  };

  return (
    <FormScreen
      title="Assistente Preço Certo"
      hideHeader
      scrollable={false}
      maxContentWidth={820}
    >
      {/* Header */}
      <View style={styles.header}>
        {!isTabRoot && navigation.canGoBack() ? (
          <Pressable
            onPress={() => navigation.goBack()}
            style={[
              styles.backBtn,
              { borderColor: colors.border, backgroundColor: colors.card },
            ]}
            accessibilityRole="button"
            accessibilityLabel="Voltar"
          >
            <Ionicons name="arrow-back" size={18} color={colors.text} />
          </Pressable>
        ) : null}
        <View style={[styles.aiHeaderBadge, { backgroundColor: colors.ink }]}>
          <Ionicons name="sparkles" size={20} color={colors.highlight} />
        </View>
        <View style={{ flex: 1 }}>
          <Text style={[styles.headerTitle, { color: colors.text }]}>
            {isAdmin
              ? 'Assistente Preço Certo (Admin)'
              : modoLojistaAtivo
                ? 'Assistente Preço Certo (Lojista)'
                : 'Assistente Preço Certo'}
          </Text>
          <Text style={[styles.headerSub, { color: colors.textMuted }]}>
            Pesquisa preços em tempo real nas lojas da sua região
          </Text>
        </View>
      </View>

      {/* Messages Feed */}
      <FlatList
        ref={listRef}
        data={mensagens}
        keyExtractor={(item) => item.id}
        style={styles.chatList}
        contentContainerStyle={styles.chatContent}
        showsVerticalScrollIndicator={false}
        onContentSizeChange={() =>
          listRef.current?.scrollToEnd?.({ animated: true })
        }
        renderItem={({ item }) => {
          const minha = item.papel === 'user';
          if (minha) {
            return (
              <View style={styles.userRow}>
                <View
                  style={[
                    styles.userBubble,
                    { backgroundColor: colors.ink },
                  ]}
                >
                  <Text
                    style={[
                      styles.userBubbleText,
                      { color: colors.inkForeground },
                    ]}
                  >
                    {item.texto}
                  </Text>
                </View>
              </View>
            );
          }

          return (
            <View style={styles.aiRow}>
              <View
                style={[
                  styles.aiAvatar,
                  { backgroundColor: colors.highlight },
                ]}
              >
                <Ionicons
                  name="sparkles"
                  size={15}
                  color={colors.highlightForeground}
                />
              </View>
              <View style={styles.aiContentCol}>
                <View
                  style={[
                    styles.aiTextCard,
                    {
                      backgroundColor: colors.card,
                      borderColor: colors.border,
                    },
                  ]}
                >
                  <Text style={[styles.aiMessageText, { color: colors.text }]}>
                    {item.texto}
                  </Text>
                </View>

                {Array.isArray(item.resultados) &&
                item.resultados.length > 0 ? (
                  <ResultadoCardsTable
                    resultados={item.resultados}
                    colors={colors}
                    navigation={navigation}
                  />
                ) : null}

                {isAdmin && item.intencao ? (
                  <AdminDebugPanel item={item} colors={colors} />
                ) : null}
              </View>
            </View>
          );
        }}
        ListFooterComponent={
          enviando ? (
            <View style={styles.loadingRow}>
              <View
                style={[
                  styles.aiAvatar,
                  { backgroundColor: colors.highlight },
                ]}
              >
                <Ionicons
                  name="sparkles"
                  size={15}
                  color={colors.highlightForeground}
                />
              </View>
              <ActivityIndicator size="small" color={colors.primary} />
            </View>
          ) : null
        }
      />

      {erro ? (
        <Text style={[styles.errorBanner, { color: colors.danger }]}>
          {erro}
        </Text>
      ) : null}

      {/* Bottom Sticky Suggestions + Input Bar */}
      <KeyboardAvoidingView
        behavior={Platform.OS === 'ios' ? 'padding' : undefined}
        style={styles.bottomArea}
      >
        <View style={styles.suggestionsWrap}>
          {chipsSugestao.map((chip) => (
            <Pressable
              key={chip}
              disabled={enviando}
              onPress={() => handleChipPress(chip)}
              style={[
                styles.suggestionChip,
                {
                  backgroundColor: colors.card,
                  borderColor: colors.border,
                  opacity: enviando ? 0.6 : 1,
                },
              ]}
            >
              <Text
                style={[styles.suggestionChipText, { color: colors.textMuted }]}
              >
                {chip}
              </Text>
            </Pressable>
          ))}
        </View>

        <View
          style={[
            styles.inputBar,
            { backgroundColor: colors.card, borderColor: colors.border },
          ]}
        >
          <TextInput
            value={texto}
            onChangeText={setTexto}
            placeholder="Pergunte sobre produtos, preços ou lojas…"
            placeholderTextColor={colors.placeholder}
            editable={!enviando}
            maxLength={500}
            returnKeyType="send"
            onSubmitEditing={handleEnviar}
            onKeyPress={handleKeyPress}
            blurOnSubmit={false}
            accessibilityLabel="Digite sua pergunta para o assistente"
            style={[styles.inputField, { color: colors.text }]}
          />
          <Pressable
            onPress={handleEnviar}
            disabled={enviando || !texto.trim()}
            accessibilityRole="button"
            accessibilityLabel="Enviar pergunta"
            accessibilityState={{ disabled: enviando || !texto.trim() }}
            style={[
              styles.sendBtn,
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
    </FormScreen>
  );
}

const styles = StyleSheet.create({
  header: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 12,
    paddingTop: 8,
    marginBottom: 16,
  },
  backBtn: {
    width: 38,
    height: 38,
    borderRadius: radii.md,
    borderWidth: 1,
    alignItems: 'center',
    justifyContent: 'center',
  },
  aiHeaderBadge: {
    width: 44,
    height: 44,
    borderRadius: radii.xl,
    alignItems: 'center',
    justifyContent: 'center',
  },
  headerTitle: {
    fontFamily: fonts.display,
    fontSize: 20,
    fontWeight: '700',
    letterSpacing: -0.3,
  },
  headerSub: {
    fontFamily: fonts.sans,
    fontSize: 12.5,
    marginTop: 2,
  },
  chatList: {
    flex: 1,
  },
  chatContent: {
    paddingVertical: 12,
    gap: 18,
  },
  userRow: {
    flexDirection: 'row',
    justifyContent: 'flex-end',
  },
  userBubble: {
    maxWidth: '82%',
    borderRadius: 24,
    borderBottomRightRadius: 6,
    paddingHorizontal: 18,
    paddingVertical: 12,
  },
  userBubbleText: {
    fontFamily: fonts.sans,
    fontSize: 14,
    lineHeight: 20,
  },
  aiRow: {
    flexDirection: 'row',
    alignItems: 'flex-start',
    gap: 12,
  },
  aiAvatar: {
    width: 32,
    height: 32,
    borderRadius: 16,
    alignItems: 'center',
    justifyContent: 'center',
    marginTop: 2,
  },
  aiContentCol: {
    flex: 1,
    gap: 10,
  },
  aiTextCard: {
    borderRadius: radii.xl,
    borderWidth: 1,
    paddingHorizontal: 16,
    paddingVertical: 12,
    ...shadows.soft,
  },
  aiMessageText: {
    fontFamily: fonts.sans,
    fontSize: 14,
    lineHeight: 21,
  },
  resultTableCard: {
    borderRadius: radii.xl,
    borderWidth: 1,
    overflow: 'hidden',
    ...shadows.soft,
  },
  resultTableRow: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    gap: 12,
    paddingHorizontal: 16,
    paddingVertical: 12,
  },
  resultRowTitleWrap: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 8,
    flexWrap: 'wrap',
  },
  resultItemTitle: {
    fontFamily: fonts.sansSemiBold,
    fontSize: 14,
    fontWeight: '600',
  },
  resultItemSub: {
    fontFamily: fonts.sans,
    fontSize: 12,
    marginTop: 2,
  },
  resultItemReason: {
    fontFamily: fonts.sans,
    fontSize: 11,
    marginTop: 2,
  },
  resultItemPrice: {
    fontFamily: fonts.display,
    fontSize: 15,
    fontWeight: '700',
  },
  resultActionLinks: {
    flexDirection: 'row',
    gap: 8,
    marginTop: 8,
  },
  miniActionPill: {
    paddingHorizontal: 10,
    paddingVertical: 5,
    borderRadius: radii.sm,
  },
  miniActionPillText: {
    fontFamily: fonts.sansBold,
    fontSize: 11,
    fontWeight: '700',
  },
  miniActionOutline: {
    paddingHorizontal: 10,
    paddingVertical: 5,
    borderRadius: radii.sm,
    borderWidth: 1,
  },
  miniActionOutlineText: {
    fontFamily: fonts.sansBold,
    fontSize: 11,
    fontWeight: '700',
  },
  resultTableFooter: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    paddingHorizontal: 16,
    paddingVertical: 11,
    gap: 8,
  },
  resultFooterSummary: {
    fontFamily: fonts.sansSemiBold,
    fontSize: 12.5,
    fontWeight: '600',
    flex: 1,
  },
  footerCtaBtn: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 3,
  },
  footerCtaText: {
    fontFamily: fonts.sansBold,
    fontSize: 12.5,
    fontWeight: '700',
  },
  adminDebugCard: {
    padding: 10,
    borderRadius: radii.md,
    borderTopWidth: 1,
    gap: 3,
  },
  adminDebugTitle: {
    fontFamily: fonts.sansBold,
    fontSize: 11,
    fontWeight: '700',
  },
  adminDebugLine: {
    fontFamily: fonts.sans,
    fontSize: 11,
  },
  loadingRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 10,
    paddingVertical: 8,
  },
  errorBanner: {
    fontFamily: fonts.sansMedium,
    fontSize: 12,
    marginBottom: 6,
  },
  bottomArea: {
    gap: 10,
    paddingTop: 8,
    paddingBottom: 6,
  },
  suggestionsWrap: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: 8,
  },
  suggestionChip: {
    borderRadius: radii.full,
    borderWidth: 1,
    paddingHorizontal: 13,
    paddingVertical: 6,
  },
  suggestionChipText: {
    fontFamily: fonts.sansMedium,
    fontSize: 12,
    fontWeight: '500',
  },
  inputBar: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 10,
    borderRadius: radii.xl,
    borderWidth: 1,
    paddingLeft: 16,
    paddingRight: 8,
    paddingVertical: 7,
    ...shadows.float,
  },
  inputField: {
    flex: 1,
    fontFamily: fonts.sans,
    fontSize: 14,
    paddingVertical: 6,
    minWidth: 0,
  },
  sendBtn: {
    width: 40,
    height: 40,
    borderRadius: radii.md,
    alignItems: 'center',
    justifyContent: 'center',
  },
});
