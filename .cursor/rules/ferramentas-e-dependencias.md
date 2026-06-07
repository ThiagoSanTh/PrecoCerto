# Ferramentas e dependências — Preço Certo

Guia para instalar e restaurar o ambiente Node.js do projeto no **CMD** (Windows).

---

## 1. Ferramentas obrigatórias (instalar uma vez no PC)

| Ferramenta | Versão recomendada | Para que serve |
|------------|-------------------|----------------|
| **Node.js** | LTS 20.x ou 22.x | Runtime JavaScript |
| **npm** | Vem com o Node.js | Gerenciador de pacotes |
| **npx** | Vem com o npm | Executa CLIs locais (Expo) sem instalação global |

### Verificar se já estão instalados (CMD)

```cmd
node --version
npm --version
npx --version
```

Saída esperada (exemplo): `v22.x.x`, `10.x.x` ou `11.x.x`.

### Instalar Node.js no Windows (se os comandos acima falharem)

**Opção A — site oficial**

1. Acesse https://nodejs.org/
2. Baixe a versão **LTS**
3. Execute o instalador e marque a opção para adicionar ao PATH

**Opção B — winget (CMD como administrador)**

```cmd
winget install OpenJS.NodeJS.LTS
```

Feche e abra o CMD novamente após instalar.

---

## 2. Instalar dependências do app mobile (CMD)

O app mobile é desenvolvido **sempre dentro de `Mobile\`**.

```cmd
cd C:\Users\thiag\Desktop\PrecoCerto\Mobile
npm ci
```

> Alternativa com reinstalação limpa:

```cmd
cd C:\Users\thiag\Desktop\PrecoCerto\Mobile
rmdir /s /q node_modules
del package-lock.json
npm install
```

---

## 3. Comandos para rodar o app mobile

Sempre a partir da pasta `Mobile\`:

```cmd
cd C:\Users\thiag\Desktop\PrecoCerto\Mobile
npm start
```

Outras opções:

```cmd
npm run android
npm run ios
npm run web
```

Equivalente com npx:

```cmd
npx expo start
npx expo start --android
npx expo start --web
```

### Variáveis de ambiente

O Expo carrega o arquivo `Mobile\.env`. Variáveis usadas:

- `EXPO_PUBLIC_API_URL`
- `EXPO_PUBLIC_GOOGLE_MAPS_API_KEY`
- `EXPO_PUBLIC_SUPABASE_ANON_KEY`
- `EXPO_PUBLIC_SUPABASE_URL`

---

## 4. Principais dependências do Mobile

Instaladas automaticamente pelo `npm ci` / `npm install` em `Mobile\`:

| Pacote | Uso no projeto |
|--------|----------------|
| `expo` (~54) | Framework e CLI do app |
| `react` / `react-native` | UI mobile |
| `@react-navigation/*` | Navegação (tabs e stack) |
| `axios` | Chamadas à API .NET |
| `@supabase/supabase-js` | Storage de imagens |
| `expo-image-picker` | Foto do produto |
| `expo-location` | GPS do cliente |
| `expo-secure-store` | Token JWT |
| `react-native-webview` | Mapa Leaflet |

Não é necessário instalar `expo` globalmente (`npm install -g expo`). Use sempre `npm start` ou `npx expo` dentro de `Mobile\`.

---

## 5. Backend (.NET) — fora do Node.js

A API REST fica em `BackEnd\`. Requer:

| Ferramenta | Comando de verificação |
|------------|------------------------|
| **.NET SDK 8+** | `dotnet --version` |

Rodar a API:

```cmd
cd C:\Users\thiag\Desktop\PrecoCerto\BackEnd\Pc.WebApi
dotnet restore
dotnet run
```

---

## 6. Problemas comuns e soluções

| Erro | Causa | Solução (CMD) |
|------|-------|----------------|
| `'node' não é reconhecido` | Node não está no PATH | Reinstale Node.js (seção 1) |
| `Cannot determine Expo SDK version` | `node_modules` ausente em `Mobile\` | `cd Mobile` → `npm ci` |
| `npm error could not determine executable to run` | Comando errado (`npx start`) | Use `npm start` ou `npx expo start` |
| `Unknown command: "expo"` | Tentou `npm expo start` | Use `npx expo start` |
| Pacotes corrompidos | Instalação parcial | Apague `node_modules` e rode `npm ci` de novo |
| `Bad Request - Invalid Hostname` no login | API só aceita `localhost` no `AllowedHosts` | Reinicie a API após atualizar `appsettings.Development.json` com `"AllowedHosts": "*"` |
| `Este host não é conhecido` no `dotnet ef database update` | `Host` errado na connection string (ex.: `postgres.PROJECT_REF`) | Supabase → Database → copie `Host=db.PROJECT_REF.supabase.co` e `Username=postgres` — ver `BackEnd/docs/SECRETS.md` |
| App não alcança a API no celular | IP errado no `.env` | Rode `ipconfig`, atualize `EXPO_PUBLIC_API_URL` em `Mobile\.env` com o IPv4 do PC (ex.: `http://192.168.1.72:5132/api`) |

### Testar se a API responde pelo IP (CMD)

```cmd
curl http://192.168.1.72:5132/swagger/index.html
```

Substitua pelo IP do seu PC. Se retornar HTML de erro "Invalid Hostname", a API precisa ser reiniciada com `AllowedHosts` corrigido.

---

## 7. Resumo rápido (copiar e colar)

Instalação completa do zero:

```cmd
cd C:\Users\thiag\Desktop\PrecoCerto\Mobile
npm ci
npm start
```

Última verificação bem-sucedida: **jun/2026** — Node v22.22.0, npm 11.6.2, Expo 54.0.23, 716 pacotes em `Mobile\`.
