import {
  apenasDigitos,
  apenasDigitosCnpj,
  isCnpjValido,
  isEmailValido,
  isTelefoneValido,
} from './validacaoUtils.js';
import { formatApiError, mapApiError } from './apiErrorUtils.js';

export const FRASES = {
  cnpj_invalido: 'CNPJ inválido. Confira os números.',
  cnpj_nao_encontrado: 'Não encontramos esse CNPJ.',
  cnpj_inativo: 'Este CNPJ não está ativo na Receita.',
  cnpj_fonte_indisponivel: 'A consulta está fora do ar. Preencha manualmente ou tente mais tarde.',
};

export const FRASE_LIMITE = 'Muitas consultas. Aguarde um minuto.';
export const FRASE_GENERICA = 'Não foi possível consultar o CNPJ. Tente novamente.';

function texto(valor) {
  return valor == null ? '' : String(valor).trim();
}

function digitos(valor) {
  return apenasDigitos(valor == null ? '' : String(valor));
}

function por(campos, chave, valor) {
  if (valor) campos[chave] = valor;
}

function cep(valor) {
  const d = digitos(valor).slice(0, 8);
  return d.length === 8 ? `${d.slice(0, 5)}-${d.slice(5)}` : d;
}

function telefone(valor) {
  const d = digitos(valor).slice(0, 11);
  if (d.length === 11) return `(${d.slice(0, 2)}) ${d.slice(2, 7)}-${d.slice(7)}`;
  if (d.length === 10) return `(${d.slice(0, 2)}) ${d.slice(2, 6)}-${d.slice(6)}`;
  return d;
}

export function mapearRespostaCnpj(resposta) {
  if (!resposta || typeof resposta !== 'object') return {};
  const campos = {};
  const razao = texto(resposta.razaoSocial);
  por(campos, 'nomeFantasia', texto(resposta.nomeFantasia) || razao);
  por(campos, 'razaoSocial', razao);
  por(campos, 'email', texto(resposta.email));
  por(campos, 'telefone', telefone(resposta.telefone));
  const endereco = resposta.endereco;
  if (endereco && typeof endereco === 'object') {
    por(campos, 'logradouro', texto(endereco.logradouro));
    por(campos, 'numero', texto(endereco.numero));
    por(campos, 'bairro', texto(endereco.bairro));
    por(campos, 'cidade', texto(endereco.cidade));
    por(campos, 'estado', texto(endereco.uf).toUpperCase().slice(0, 2));
    por(campos, 'cep', cep(endereco.cep));
  }
  return campos;
}

export function planejarPreenchimento(resposta, atuais) {
  const vazios = {};
  const conflitos = {};
  for (const [campo, valor] of Object.entries(mapearRespostaCnpj(resposta))) {
    const atual = texto(atuais?.[campo]);
    if (!atual) vazios[campo] = valor;
    else if (atual !== valor) conflitos[campo] = valor;
  }
  return { vazios, conflitos };
}

export function mensagemErroConsultaCnpj(error) {
  const data = error?.response?.data;
  const headers = error?.response?.headers;
  const header = headers?.get?.('x-error-code') ?? headers?.['x-error-code'];
  const code = (typeof data?.code === 'string' && data.code) || (typeof header === 'string' && header) || '';
  if (FRASES[code]) return FRASES[code];
  if (error?.response?.status === 429) return FRASE_LIMITE;
  return FRASE_GENERICA;
}

/**
 * Valida os campos do passo "Loja" antes de avançar ou enviar à API.
 * Centralizar aqui evita divergência entre os passos e o DTO do backend.
 */
export function validarDadosLoja({ nomeFantasia, cnpj, telefone: tel, email } = {}) {
  const nome = texto(nomeFantasia);
  if (!nome) {
    return { title: 'Loja', message: 'Informe o nome fantasia.' };
  }
  if (nome.length < 2) {
    return { title: 'Loja', message: 'O nome fantasia deve ter pelo menos 2 caracteres.' };
  }
  // Abrir loja exige CNPJ válido — é o que torna o usuário um lojista.
  const cnpjLimpo = texto(cnpj);
  if (!cnpjLimpo) {
    return { title: 'Loja', message: 'Informe o CNPJ da loja.' };
  }
  if (!isCnpjValido(cnpjLimpo)) {
    return { title: 'Loja', message: 'CNPJ inválido. Verifique os números informados.' };
  }
  const telLimpo = texto(tel);
  if (telLimpo && !isTelefoneValido(telLimpo)) {
    return {
      title: 'Loja',
      message: 'Telefone inválido. Informe 8 dígitos (fixo) ou 9 dígitos (celular), com DDD opcional.',
    };
  }
  const emailLimpo = texto(email);
  if (emailLimpo && !isEmailValido(emailLimpo)) {
    return { title: 'Loja', message: 'E-mail da loja inválido.' };
  }
  return null;
}

/**
 * Valida os campos obrigatórios do passo "Endereço" em alinhamento com EnderecoDto.
 */
export function validarDadosEndereco({ cep: cepVal, logradouro, numero, cidade, estado } = {}) {
  const cepSomenteDigitos = digitos(cepVal);
  const uf = texto(estado).toUpperCase();
  if (
    cepSomenteDigitos.length !== 8 ||
    !texto(logradouro) ||
    !texto(numero) ||
    !texto(cidade) ||
    uf.length !== 2
  ) {
    return {
      title: 'Endereço',
      message: 'Preencha CEP (8 dígitos), logradouro, número, cidade e UF (2 letras).',
    };
  }
  return null;
}

function coordenadaValida(valor) {
  if (valor === null || valor === undefined || valor === '') return null;
  const num = Number(valor);
  return Number.isFinite(num) ? num : null;
}

/**
 * Monta o payload normalizado para POST /Lojas.
 * Envia CNPJ só com dígitos e e-mail nulo quando vazio para não falhar em DataAnnotations.
 */
export function montarPayloadCriarLoja({
  nomeFantasia,
  razaoSocial,
  cnpj,
  telefone: tel,
  emailLoja,
  sessionEmail,
  endereco = {},
} = {}) {
  const cepSomenteDigitos = digitos(endereco.cep);
  const estadoSigla = texto(endereco.estado).toUpperCase().slice(0, 2);
  const bairroNormalizado = texto(endereco.bairro) || 'Centro';

  return {
    nomeFantasia: texto(nomeFantasia),
    razaoSocial: texto(razaoSocial) || null,
    cnpj: apenasDigitosCnpj(cnpj),
    telefone: texto(tel) || null,
    email: texto(emailLoja) || texto(sessionEmail) || null,
    endereco: {
      cep: cepSomenteDigitos,
      logradouro: texto(endereco.logradouro),
      numero: texto(endereco.numero),
      bairro: bairroNormalizado,
      cidade: texto(endereco.cidade),
      estado: estadoSigla,
      latitude: coordenadaValida(endereco.latitude),
      longitude: coordenadaValida(endereco.longitude),
    },
  };
}

/**
 * Orquestra validação, geocodificação opcional, criação na API e transição final para Login.
 * Extraída da tela para permitir testes automatizados determinísticos contra regressões do bug #97.
 */
export async function executarCriacaoLoja({
  dados,
  session,
  garantirCnpjNaReceita,
  resolverCoordenadas,
  criarLojaFn,
  concluirParaLogin,
}) {
  const erroLoja = validarDadosLoja({
    nomeFantasia: dados?.nomeFantasia,
    cnpj: dados?.cnpj,
    telefone: dados?.telefone,
    email: dados?.emailLoja,
  });
  if (erroLoja) {
    return { status: 'validation_error', step: 0, feedback: erroLoja };
  }

  const erroEndereco = validarDadosEndereco(dados?.endereco);
  if (erroEndereco) {
    return { status: 'validation_error', step: 1, feedback: erroEndereco };
  }

  if (!session?.perfil?.id) {
    return {
      status: 'unauthenticated',
      feedback: { title: 'Sessão necessária', message: 'Faça login para abrir uma loja.' },
    };
  }

  if (!(await garantirCnpjNaReceita())) {
    return { status: 'cnpj_error', step: 0 };
  }

  let lat = dados?.endereco?.latitude ?? null;
  let lng = dados?.endereco?.longitude ?? null;

  if (lat === null || lng === null) {
    try {
      const coords = await resolverCoordenadas();
      lat = coords?.latitude ?? null;
      lng = coords?.longitude ?? null;
    } catch {
      lat = null;
      lng = null;
    }
  }

  try {
    await criarLojaFn(
      montarPayloadCriarLoja({
        nomeFantasia: dados.nomeFantasia,
        razaoSocial: dados.razaoSocial,
        cnpj: dados.cnpj,
        telefone: dados.telefone,
        emailLoja: dados.emailLoja,
        sessionEmail: session.perfil.email,
        endereco: {
          ...dados.endereco,
          latitude: lat,
          longitude: lng,
        },
      })
    );

    const notice = {
      title: 'Loja criada!',
      message: 'Sua conta agora é de lojista. Entre novamente para acessar o painel da loja.',
      variant: 'success',
    };
    await concluirParaLogin(notice);
    return { status: 'created', notice };
  } catch (error) {
    if (error?.response?.status === 409) {
      const notice = {
        title: 'Loja já cadastrada',
        message: formatApiError(error),
        variant: 'info',
      };
      await concluirParaLogin(notice);
      return { status: 'conflict_completed', notice };
    }

    return { status: 'api_error', feedback: mapApiError(error) };
  }
}

