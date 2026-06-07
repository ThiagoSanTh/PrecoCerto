# Segredos do backend (Pc.WebApi)

Nunca commite `appsettings.json` com credenciais reais. Use **User Secrets** em desenvolvimento.

## Rotacionar credenciais expostas

Se `appsettings.json` com senha do Supabase já foi commitado, rotacione a senha no Supabase Dashboard e atualize os secrets locais.

## User Secrets

No Supabase: **Project Settings → Database → Connection string → URI** (modo *Direct*).

| Campo na connection string | Valor correto (exemplo) |
|----------------------------|-------------------------|
| `Host` | `db.SEU_PROJECT_REF.supabase.co` |
| `Username` | `postgres` |
| `Password` | senha do banco (não a anon key) |

**Erro comum:** usar `Host=postgres.SEU_PROJECT_REF` — isso é o **username** do pooler, não o host. O DNS falha com *"Este host não é conhecido"*.

**Pooler (IPv4, produção):**

```text
Host=aws-1-sa-east-1.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.SEU_PROJECT_REF;Password=SUA_SENHA;SSL Mode=Require
```

**Direct (migrations locais):**

```text
Host=db.SEU_PROJECT_REF.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=SUA_SENHA;SSL Mode=Require
```

```bash
cd "BackEnd/Pc.WebApi"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=db.SEU_PROJECT_REF.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=SUA_SENHA;SSL Mode=Require"
dotnet user-secrets set "Jwt:Secret" "sua-chave-secreta-com-pelo-menos-32-caracteres"
dotnet ef database update
```
