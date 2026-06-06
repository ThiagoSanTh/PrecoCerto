-- Execute no Supabase SQL Editor (Storage + políticas para fotos de produto).
-- Leitura pública; escrita apenas para usuários autenticados (JWT Supabase futuro)
-- ou via path restrito por loja. Anon não pode mais INSERT/UPDATE.

INSERT INTO storage.buckets (id, name, public, file_size_limit, allowed_mime_types)
VALUES (
  'produtos-imagens',
  'produtos-imagens',
  true,
  5242880,
  ARRAY['image/jpeg', 'image/png', 'image/webp']
)
ON CONFLICT (id) DO UPDATE SET
  public = EXCLUDED.public,
  file_size_limit = EXCLUDED.file_size_limit,
  allowed_mime_types = EXCLUDED.allowed_mime_types;

DROP POLICY IF EXISTS "produtos_imagens_public_read" ON storage.objects;
CREATE POLICY "produtos_imagens_public_read"
ON storage.objects FOR SELECT
TO public
USING (bucket_id = 'produtos-imagens');

DROP POLICY IF EXISTS "produtos_imagens_anon_insert" ON storage.objects;
DROP POLICY IF EXISTS "produtos_imagens_anon_update" ON storage.objects;
DROP POLICY IF EXISTS "produtos_imagens_anon_select" ON storage.objects;

DROP POLICY IF EXISTS "produtos_imagens_auth_insert" ON storage.objects;
CREATE POLICY "produtos_imagens_auth_insert"
ON storage.objects FOR INSERT
TO authenticated
WITH CHECK (
  bucket_id = 'produtos-imagens'
  AND (storage.foldername(name))[1] IS NOT NULL
);

DROP POLICY IF EXISTS "produtos_imagens_auth_update" ON storage.objects;
CREATE POLICY "produtos_imagens_auth_update"
ON storage.objects FOR UPDATE
TO authenticated
USING (bucket_id = 'produtos-imagens')
WITH CHECK (bucket_id = 'produtos-imagens');

DROP POLICY IF EXISTS "produtos_imagens_auth_delete" ON storage.objects;
CREATE POLICY "produtos_imagens_auth_delete"
ON storage.objects FOR DELETE
TO authenticated
USING (bucket_id = 'produtos-imagens');
