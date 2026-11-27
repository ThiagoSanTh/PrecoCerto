import { apenasDigitos } from './validacaoUtils.js';

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
