-- Limpeza manual: apaga usuários com e-mail NÃO confirmado e todas as suas dependências.
-- Execute no SQL Editor do Supabase ANTES do deploy que passa a exigir e-mail confirmado no login.
--
-- ATENÇÃO: operação destrutiva e irreversível. Rode primeiro o bloco de CONFERÊNCIA,
-- valide as contagens e só então execute o bloco de EXCLUSÃO (em transação).

-- ============================================================
-- 1) CONFERÊNCIA (somente leitura)
-- ============================================================

-- Usuários que serão apagados
SELECT u."Id", u."Email", u."NomeUsuario", u."Papel", u."DataCriacao"
FROM "Usuarios" u
WHERE u."EmailConfirmado" = false;

-- Lojas pertencentes a esses usuários (serão apagadas junto)
SELECT l."Id", l."NomeFantasia", l."Cnpj", u."Email" AS dono
FROM "Lojas" l
JOIN "Usuarios" u ON u."Id" = l."UsuarioId"
WHERE u."EmailConfirmado" = false;

-- Contagens do impacto
SELECT 'Usuarios não confirmados' AS item, COUNT(*)::bigint AS total
FROM "Usuarios" WHERE "EmailConfirmado" = false
UNION ALL
SELECT 'Lojas desses usuários', COUNT(*)::bigint
FROM "Lojas" l WHERE l."UsuarioId" IN (SELECT "Id" FROM "Usuarios" WHERE "EmailConfirmado" = false)
UNION ALL
SELECT 'Produtos dessas lojas', COUNT(*)::bigint
FROM "Produtos" p WHERE p."LojaId" IN (
    SELECT l."Id" FROM "Lojas" l
    WHERE l."UsuarioId" IN (SELECT "Id" FROM "Usuarios" WHERE "EmailConfirmado" = false));

-- ============================================================
-- 2) EXCLUSÃO (executar de uma vez, em transação)
-- ============================================================

BEGIN;

-- Conjuntos de trabalho
CREATE TEMP TABLE _usuarios_alvo ON COMMIT DROP AS
SELECT "Id" FROM "Usuarios" WHERE "EmailConfirmado" = false;

CREATE TEMP TABLE _lojas_alvo ON COMMIT DROP AS
SELECT "Id", "EnderecoId" FROM "Lojas" WHERE "UsuarioId" IN (SELECT "Id" FROM _usuarios_alvo);

CREATE TEMP TABLE _produtos_alvo ON COMMIT DROP AS
SELECT "Id" FROM "Produtos" WHERE "LojaId" IN (SELECT "Id" FROM _lojas_alvo);

-- 2.1 Itens de carrinho: dos carrinhos dos usuários-alvo OU que referenciam produtos das lojas-alvo
DELETE FROM "ItensCarrinho"
WHERE "CarrinhoId" IN (SELECT "Id" FROM "Carrinhos" WHERE "ClienteId" IN (SELECT "Id" FROM _usuarios_alvo))
   OR "ProdutoId" IN (SELECT "Id" FROM _produtos_alvo);

-- 2.2 Carrinhos dos usuários-alvo
DELETE FROM "Carrinhos"
WHERE "ClienteId" IN (SELECT "Id" FROM _usuarios_alvo);

-- 2.3 Favoritos: dos usuários-alvo OU apontando para produtos/lojas-alvo
DELETE FROM "Favoritos"
WHERE "ClienteId" IN (SELECT "Id" FROM _usuarios_alvo)
   OR "ProdutoId" IN (SELECT "Id" FROM _produtos_alvo)
   OR "LojaId" IN (SELECT "Id" FROM _lojas_alvo);

-- 2.4 Avaliações: dos usuários-alvo OU das lojas-alvo
DELETE FROM "Avaliacoes"
WHERE "ClienteId" IN (SELECT "Id" FROM _usuarios_alvo)
   OR "LojaId" IN (SELECT "Id" FROM _lojas_alvo);

-- 2.5 Histórico de pesquisa: dos usuários-alvo; vínculos BI de outros usuários ficam NULL
DELETE FROM "HistoricosPesquisa"
WHERE "ClienteId" IN (SELECT "Id" FROM _usuarios_alvo);

UPDATE "HistoricosPesquisa"
SET "ProdutoId" = NULL
WHERE "ProdutoId" IN (SELECT "Id" FROM _produtos_alvo);

UPDATE "HistoricosPesquisa"
SET "LojaId" = NULL
WHERE "LojaId" IN (SELECT "Id" FROM _lojas_alvo);

-- 2.6 Preferências dos usuários-alvo
DELETE FROM "PreferenciasClientes"
WHERE "ClienteId" IN (SELECT "Id" FROM _usuarios_alvo);

-- 2.7 Ofertas das lojas-alvo (ou de produtos-alvo)
DELETE FROM "Ofertas"
WHERE "LojaId" IN (SELECT "Id" FROM _lojas_alvo)
   OR "ProdutoId" IN (SELECT "Id" FROM _produtos_alvo);

-- 2.8 Produtos das lojas-alvo
DELETE FROM "Produtos"
WHERE "Id" IN (SELECT "Id" FROM _produtos_alvo);

-- 2.9 Vendedores confirmados vinculados às lojas-alvo perdem o vínculo (conta preservada)
UPDATE "Usuarios"
SET "LojaVinculadaId" = NULL
WHERE "LojaVinculadaId" IN (SELECT "Id" FROM _lojas_alvo);

-- 2.10 Lojas-alvo e seus endereços
DELETE FROM "Lojas"
WHERE "Id" IN (SELECT "Id" FROM _lojas_alvo);

DELETE FROM "Enderecos"
WHERE "Id" IN (SELECT "EnderecoId" FROM _lojas_alvo);

-- 2.11 Por fim, os usuários não confirmados
DELETE FROM "Usuarios"
WHERE "Id" IN (SELECT "Id" FROM _usuarios_alvo);

COMMIT;

-- ============================================================
-- 3) PÓS-CONFERÊNCIA (deve retornar 0)
-- ============================================================
SELECT COUNT(*) AS usuarios_nao_confirmados_restantes
FROM "Usuarios" WHERE "EmailConfirmado" = false;
