# Scripts SQL — Backend

Scripts auxiliares para Supabase/PostgreSQL. Execute no SQL Editor do Supabase ou via `psql` quando indicado.

| Arquivo | Uso |
|---------|-----|
| `setup-produtos-imagens-storage.sql` | Bucket e políticas RLS para fotos de produtos |
| `limpar-usuarios-nao-confirmados.sql` | Manutenção: remove contas sem e-mail confirmado |

Alterações de schema são feitas **somente** pelas migrations EF em `Pc.Infraestrutura/Migrations/` (aplicadas no startup da API).
