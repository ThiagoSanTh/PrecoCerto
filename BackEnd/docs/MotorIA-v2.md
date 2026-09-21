# MotorIA v2 — Documentação técnica

## Objetivo

Motor de inteligência **proprietário e determinístico** do Preço Certo.
Não depende de LLM. Usa interpretação por vocabulário, regras, pontuação,
dados reais (PostgreSQL via serviços) e RAG opcional como conhecimento auxiliar.

## Arquitetura

```
POST /api/IA/analisar
        ↓
    IMotorIA (orquestrador)
        ↓
IInterpretadorIA → ContextoIA
        ↓
IMotorRegrasIA (IRegraIA*)
        ↓
IProdutoServico / IOfertaServico / ILojaServico / IAvaliacaoServico / IClimaServico
        ↓
IRagConhecimentoIA → IRagServico (opcional)
        ↓
IMotorRecomendacaoIA → IMotorPontuacaoIA
        ↓
IGeradorRespostaIA → ResultadoAnaliseIA
```

O protótipo `POST /api/IA/chat` permanece independente.

## ContextoIA

Estado estruturado da requisição: mensagem, intenção, objetivo, entidades,
preferências, localização, clima, pesos, candidatos, decisão, confiança e fallbacks.

## Intenções (`IntencaoIA`)

BuscarProduto, BuscarOferta, BuscarLoja, CompararPrecos, BuscarPromocao,
BuscarProdutoMaisBarato, BuscarLojaMaisProxima, RecomendarProduto, RecomendarLoja,
BuscarProdutosRelacionados, ConsultarDisponibilidade, ConsultarEntrega, MontarCesta,
ForaDoDominio, NaoEntendida.

## Objetivos (`ObjetivoIA`)

Economizar, Rapidez, Proximidade, Qualidade, Promocao, Conveniencia, Disponibilidade.

## Vocabulário

Listas estáticas em `VocabularioIA` (PT-BR): preço baixo, distância, urgência,
entrega, promoção, qualidade, fora de domínio, sinônimos de categoria.

Para adicionar sinônimos: editar `VocabularioIA` e cobrir com teste.

## Regras

Implementações de `IRegraIA` registradas no DI:

| Nome | Efeito principal |
|------|------------------|
| USUARIO_QUER_ECONOMIZAR | Preço 40, Promoção 20, Distância 15… |
| URGENCIA | ↑ disponibilidade e distância |
| CHUVA | ↑ entrega/distância (requer clima) |
| NECESSITA_ENTREGA | ↑ entrega (catálogo ainda sem delivery) |
| PROXIMIDADE | ↑ distância |
| BUSCA_PROMOCAO | ↑ promoção |
| QUALIDADE | ↑ avaliação |

Sem lat/lng: peso distância = 0 + fallback `sem_localizacao`.

## Fórmula de pontuação

Critérios normalizados em **[0, 1]** (1 = melhor):

- **Preço**: `1 - (p - min)/(max - min)` no conjunto
- **Distância**: idem invertido; sem coords → 0.5 e peso 0
- **Promoção**: 1 se `EmPromocao`
- **Disponibilidade**: 1 se disponível
- **Avaliação**: `(media - 1) / 4`
- **Entrega**: proxy por proximidade se usuário pediu entrega
- **Conveniência**: média disponibilidade + distância

```
Score = Σ (criterio_i × peso_i) / Σ pesos_ativos
```

## RAG

`IRagConhecimentoIA` envolve `IRagServico`. Usado quando:
- intenção relacionada;
- nenhum candidato após busca estruturada;
- recomendação de produto.

Se RAG não configurado ou falhar → fallback `sem_rag`; motor continua.

**RAG nunca é fonte de verdade** para preço, estoque, distância ou disponibilidade.

## Confiança

Métrica determinística 0–1: intenção clara, entidade, candidato, localização, clima, hit RAG.

## Fallbacks

- sem_localizacao
- sem_clima
- sem_rag
- sem resultados → mensagem honesta

## API

```http
POST /api/IA/analisar
```

```json
{
  "mensagem": "Quero arroz barato perto de mim",
  "latitude": -22.9,
  "longitude": -42.5
}
```

## Como estender

1. **Nova intenção**: valor em `IntencaoIA` + regra no `ClassificadorIntencaoIA` + ramo no gerador/carregamento se necessário.
2. **Nova regra**: classe `IRegraIA` + `AddScoped<IRegraIA, NovaRegra>()`.
3. **Novo sinônimo**: entrada em `VocabularioIA`.
4. **Novo critério**: campo em `PesosIA`/`CriteriosNormalizadosIA` + acumulação em `MotorPontuacaoIA`.

## Evolução LLM (futuro)

Substituir apenas `IInterpretadorIA` / classificador por camada LLM opcional.
Manter regras, pontuação, recomendação, RAG e acesso a dados.
