-- Execute no Supabase (SQL Editor) se "dotnet ef database update" não rodar.
-- Corrige: column p.LojaId does not exist

ALTER TABLE "Produtos" ADD COLUMN IF NOT EXISTS "LojaId" uuid NULL;

CREATE INDEX IF NOT EXISTS "IX_Produtos_LojaId" ON "Produtos" ("LojaId");

DO $$
BEGIN
  IF NOT EXISTS (
    SELECT 1 FROM pg_constraint WHERE conname = 'FK_Produtos_Lojas_LojaId'
  ) THEN
    ALTER TABLE "Produtos"
      ADD CONSTRAINT "FK_Produtos_Lojas_LojaId"
      FOREIGN KEY ("LojaId") REFERENCES "Lojas" ("Id") ON DELETE SET NULL;
  END IF;
END $$;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260519120000_AddLojaIdToProduto', '8.0.5')
ON CONFLICT ("MigrationId") DO NOTHING;
