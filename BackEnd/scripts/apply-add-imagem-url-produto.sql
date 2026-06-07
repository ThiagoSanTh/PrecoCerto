-- Execute no Supabase (SQL Editor) se "dotnet ef database update" não rodar.
ALTER TABLE "Produtos" ADD COLUMN IF NOT EXISTS "ImagemUrl" text;
