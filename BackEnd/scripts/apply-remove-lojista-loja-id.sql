-- Remove coluna redundante Lojistas.LojaId (vínculo oficial: Lojas.LojistaId)
-- Execute no SQL Editor do Supabase se não usar dotnet ef database update

UPDATE "Lojas" lo
SET "LojistaId" = lj."Id"
FROM "Lojistas" lj
WHERE lj."LojaId" = lo."Id"
  AND lo."LojistaId" IS NULL
  AND lj."LojaId" IS NOT NULL;

ALTER TABLE "Lojistas" DROP COLUMN IF EXISTS "LojaId";

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260607055637_RemoveRedundantLojistaLojaId', '8.0.5')
ON CONFLICT ("MigrationId") DO NOTHING;
