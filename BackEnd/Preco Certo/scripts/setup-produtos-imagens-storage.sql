-- Execute no Supabase SQL Editor (Storage + políticas para fotos de produto).

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

-- Leitura pública (pins no mapa e lista).
DROP POLICY IF EXISTS "produtos_imagens_public_read" ON storage.objects;
CREATE POLICY "produtos_imagens_public_read"
ON storage.objects FOR SELECT
TO public
USING (bucket_id = 'produtos-imagens');

-- Upload pelo app (anon key). Ajuste para auth quando houver login Supabase.
DROP POLICY IF EXISTS "produtos_imagens_anon_insert" ON storage.objects;
CREATE POLICY "produtos_imagens_anon_insert"
ON storage.objects FOR INSERT
TO anon, authenticated
WITH CHECK (bucket_id = 'produtos-imagens');

DROP POLICY IF EXISTS "produtos_imagens_anon_update" ON storage.objects;
CREATE POLICY "produtos_imagens_anon_update"
ON storage.objects FOR UPDATE
TO anon, authenticated
USING (bucket_id = 'produtos-imagens')
WITH CHECK (bucket_id = 'produtos-imagens');

DROP POLICY IF EXISTS "produtos_imagens_anon_select" ON storage.objects;
CREATE POLICY "produtos_imagens_anon_select"
ON storage.objects FOR SELECT
TO anon, authenticated
USING (bucket_id = 'produtos-imagens');
