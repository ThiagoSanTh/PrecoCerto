# Scripts SQL — Backend

Scripts auxiliares para Supabase/PostgreSQL. Execute no SQL Editor do Supabase ou via `psql` quando indicado.

| Arquivo | Uso |
|---------|-----|
| `setup-produtos-imagens-storage.sql` | Bucket e políticas RLS para fotos de produtos |
| `apply-add-loja-id-produto.sql` | Coluna `LojaId` em produtos (migration manual) |
| `apply-add-imagem-url-produto.sql` | Coluna `ImagemUrl` em produtos (migration manual) |
| `apply-remove-lojista-loja-id.sql` | Remove `Lojistas.LojaId` redundante (FK única em `Lojas.LojistaId`) |

Preferencialmente use as migrations EF em `Pc.Infraestrutura/Migrations/` para alterações de schema versionadas.
