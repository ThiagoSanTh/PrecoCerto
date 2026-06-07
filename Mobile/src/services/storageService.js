import { Platform } from 'react-native';
import { supabase, BUCKET_PRODUTOS_IMAGENS } from './supabaseClient';

function extensaoPorMime(mimeType) {
  if (mimeType?.includes('png')) return 'png';
  if (mimeType?.includes('webp')) return 'webp';
  return 'jpg';
}

function base64ParaBytes(base64) {
  const binary = atob(base64);
  const bytes = new Uint8Array(binary.length);
  for (let i = 0; i < binary.length; i += 1) {
    bytes[i] = binary.charCodeAt(i);
  }
  return bytes;
}

async function uriParaBytes(uri) {
  if (Platform.OS === 'web') {
    const response = await fetch(uri);
    if (!response.ok) {
      throw new Error('Não foi possível ler a imagem selecionada.');
    }
    const buffer = await response.arrayBuffer();
    if (!buffer?.byteLength) {
      throw new Error('Não foi possível ler a imagem selecionada.');
    }
    return new Uint8Array(buffer);
  }

  const FileSystem = require('expo-file-system/legacy');
  const base64 = await FileSystem.readAsStringAsync(uri, {
    encoding: FileSystem.EncodingType.Base64,
  });

  if (!base64?.length) {
    throw new Error('Não foi possível ler a imagem selecionada.');
  }

  return base64ParaBytes(base64);
}

/**
 * Envia foto do produto para Supabase Storage e retorna URL pública.
 * @param {{ uri: string, lojaId: string, fileName?: string, mimeType?: string }} params
 */
export async function uploadImagemProduto({ uri, lojaId, fileName, mimeType }) {
  if (!supabase) {
    throw new Error(
      'Supabase não configurado. Adicione EXPO_PUBLIC_SUPABASE_URL e EXPO_PUBLIC_SUPABASE_ANON_KEY no .env'
    );
  }

  if (!lojaId) {
    throw new Error('Loja não identificada para upload da imagem.');
  }

  const ext = extensaoPorMime(mimeType);
  const nomeArquivo = fileName || `${Date.now()}.${ext}`;
  const path = `${lojaId}/${nomeArquivo}`;
  const contentType = mimeType || `image/${ext === 'png' ? 'png' : 'jpeg'}`;

  const fileData = await uriParaBytes(uri);

  const { error } = await supabase.storage
    .from(BUCKET_PRODUTOS_IMAGENS)
    .upload(path, fileData, {
      contentType,
      upsert: true,
    });

  if (error) {
    if (error.message?.toLowerCase().includes('bucket not found')) {
      throw new Error(
        'Bucket "produtos-imagens" não existe no Supabase. Crie em Storage → New bucket ou execute o script setup-produtos-imagens-storage.sql no SQL Editor.'
      );
    }
    throw new Error(error.message || 'Falha ao enviar imagem do produto.');
  }

  const { data } = supabase.storage.from(BUCKET_PRODUTOS_IMAGENS).getPublicUrl(path);

  if (!data?.publicUrl) {
    throw new Error('Não foi possível obter a URL pública da imagem.');
  }

  return data.publicUrl;
}
