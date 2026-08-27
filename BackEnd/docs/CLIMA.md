# Clima (Weather)

Infraestrutura de clima do Preço Certo. Os dados são estruturados para um futuro Context Builder / LLM — **não há recomendação nem prompt nesta etapa**.

## Provedor

**Open-Meteo Forecast API** (`https://api.open-meteo.com/v1/forecast`)

Motivos:

- cobertura no Brasil;
- temperatura, sensação térmica, umidade, vento, precipitação, probabilidade de chuva, código WMO;
- previsão horária e diária;
- API estável e documentada;
- plano gratuito adequado a MVP, inclusive uso comercial (licença [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/) — atribuir “Weather data by Open-Meteo.com”);
- **não exige API key** no plano público, então nada de segredo no mobile nem no Git.

Cidade/UF: **Nominatim (OpenStreetMap)** (`https://nominatim.openstreetmap.org/reverse`). O Open-Meteo ainda não oferece reverse geocode estável (`/v1/reverse` responde 404). Nominatim também não usa API key; o `User-Agent` identifica o Preço Certo. Falha no geocode **não** derruba o clima (cidade fica vazia).

O restante da API fala só com `IClimaServico`. Trocar o fornecedor = nova implementação de `IClimaProvedor`.

`Clima:ApiKey` existe na configuração só para um provedor futuro. Open-Meteo ignora. **Nunca** coloque chave no React Native.

## Endpoint

```
GET /api/Weather?latitude=-22.93&longitude=-42.51
```

Anônimo (só coordenadas). Rate limit do catálogo (120/min).

Resposta normalizada (camelCase), não o JSON do Open-Meteo:

```json
{
  "localidade": { "latitude": -22.93, "longitude": -42.51, "cidade": "Saquarema", "estado": "RJ", "pais": "BR" },
  "atual": {
    "temperatura": 27.4,
    "sensacaoTermica": 29.1,
    "umidade": 78,
    "velocidadeVento": 14.2,
    "precipitacao": 0,
    "probabilidadePrecipitacao": 20,
    "codigoClima": 1,
    "descricao": "Predominantemente claro",
    "icone": "clear",
    "momento": "2026-08-27T15:00:00"
  },
  "horaria": [],
  "diaria": [],
  "obtidoEm": "2026-08-27T18:00:00Z",
  "deCache": false,
  "desatualizado": false,
  "provedor": "open-meteo"
}
```

400 se faltar coordenada ou estiver fora do intervalo. 503 se o provedor falhar **e** não houver cache stale.

## Cache

`IMemoryCache` (já usado no CNPJ). Sem tabela no PostgreSQL — clima atual é temporário.

- Chave: `clima:{lat}:{lng}` com **2 casas decimais** (~1,1 km), para não criar entrada por cada jitter de GPS.
- TTL fresco: `Clima:CacheMinutos` (padrão **15**).
- Retenção stale: `Clima:CacheStaleHoras` (padrão **6**). Se o provedor cair, devolve o último valor com `desatualizado: true`.
- Timeout HTTP: `Clima:TimeoutSegundos` (padrão **8**). Sem retry agressivo.

## Configuração

Em `appsettings.Exemplo.json` ou variáveis de ambiente:

```text
Clima__CacheMinutos=15
Clima__CacheStaleHoras=6
Clima__PrecisaoCoordenadas=2
Clima__TimeoutSegundos=8
```

Se um dia o provedor exigir chave: `Clima__ApiKey` via User Secrets / Railway. Nunca commitar o valor.

## Frontend

`Mobile/src/services/weatherService.js` → `GET /Weather` via `api.js`. Cache local 15 min (`feedCache`). A Home (`SearchScreen`) reutiliza o GPS de `locationService` (e, se faltar, `session.perfil.latitudeAtual`). `WeatherCard` é contextual: loading (skeleton), sucesso, erro discreto, offline, sem localização. Falha de clima **não** bloqueia busca/mapa.

Sem GPS: o card pede para ativar localização; o resto do app segue.

## Futuro (não implementado)

`Location + Weather + Products + Offers + Stores + Time + User` → `IContextBuilder` → LLM. Os modelos já são estruturados (`codigoClima`, probabilidade, min/max), não um texto único “28°C e ensolarado”.
