-- Pré-voo manual: execute no SQL Editor do Supabase ANTES de aplicar UnificarUsuarios.
-- Resultado esperado: 0 em todas as contagens de problema.

-- 1) E-mails duplicados entre Clientes e Lojistas (bloqueia a migration)
SELECT lower(c."Email") AS email, COUNT(*) AS ocorrencias
FROM (
    SELECT "Email" FROM "Clientes"
    UNION ALL
    SELECT "Email" FROM "Lojistas"
) c
GROUP BY lower(c."Email")
HAVING COUNT(*) > 1;

-- 2) Lojas com LojistaId apontando para lojista inexistente
SELECT l."Id", l."NomeFantasia", l."LojistaId"
FROM "Lojas" l
WHERE l."LojistaId" IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM "Lojistas" lj WHERE lj."Id" = l."LojistaId");

-- 3) Lojistas sem loja (informativo — não bloqueia)
SELECT lj."Id", lj."Email", lj."NomeUsuario"
FROM "Lojistas" lj
WHERE NOT EXISTS (SELECT 1 FROM "Lojas" l WHERE l."LojistaId" = lj."Id");

-- 4) Contagens atuais (registro para backup manual)
SELECT 'Clientes' AS tabela, COUNT(*)::bigint AS total FROM "Clientes"
UNION ALL
SELECT 'Lojistas', COUNT(*)::bigint FROM "Lojistas"
UNION ALL
SELECT 'Lojas', COUNT(*)::bigint FROM "Lojas";

-- 4b) Opcional: SELECT COUNT(*) FROM "Carrinhos"; (se AddCarrinho já foi aplicada)
