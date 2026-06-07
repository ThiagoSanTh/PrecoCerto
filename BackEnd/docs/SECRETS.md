# Segredos do backend (Pc.WebApi)

Nunca commite `appsettings.json` com credenciais reais. Use **User Secrets** em desenvolvimento.

## Rotacionar credenciais expostas

Se `appsettings.json` com senha do Supabase já foi commitado, rotacione a senha no Supabase Dashboard e atualize os secrets locais.

## User Secrets

```bash
cd "BackEnd/Pc.WebApi"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=...;Password=...;SSL Mode=Require"
dotnet user-secrets set "Jwt:Secret" "sua-chave-secreta-com-pelo-menos-32-caracteres"
```
