import { createClient } from '@supabase/supabase-js';

function normalizeSupabaseUrl(url) {
  if (!url) return url;

  const trimmed = url.trim().replace(/\/$/, '');

  const dashboardMatch = trimmed.match(/supabase\.com\/dashboard\/project\/([a-z0-9]+)/i);
  if (dashboardMatch) {
    console.warn(
      'EXPO_PUBLIC_SUPABASE_URL parece ser a URL do painel Supabase. Use a Project URL da API.'
    );
    return `https://${dashboardMatch[1]}.supabase.co`;
  }

  if (!/^https:\/\/[a-z0-9-]+\.supabase\.co$/i.test(trimmed)) {
    console.warn(
      'EXPO_PUBLIC_SUPABASE_URL inválida. Use o formato https://SEU_PROJECT_REF.supabase.co (Settings → API → Project URL).'
    );
  }

  return trimmed;
}

const supabaseUrl = normalizeSupabaseUrl(process.env.EXPO_PUBLIC_SUPABASE_URL);
const supabaseAnonKey = process.env.EXPO_PUBLIC_SUPABASE_ANON_KEY?.trim();

if (!supabaseUrl || !supabaseAnonKey) {
  console.warn(
    'Supabase: defina EXPO_PUBLIC_SUPABASE_URL e EXPO_PUBLIC_SUPABASE_ANON_KEY no .env ou na Vercel.'
  );
}

export const supabase =
  supabaseUrl && supabaseAnonKey
    ? createClient(supabaseUrl, supabaseAnonKey)
    : null;

export const BUCKET_PRODUTOS_IMAGENS = 'produtos-imagens';
