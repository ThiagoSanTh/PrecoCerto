import { apenasDigitos } from './validacaoUtils.js';

export const ERROS_CNPJ = {
  invalido: 'CNPJ inválido. Confira os números.',
  naoEncontrado: 'Não encontramos esse CNPJ.',
  inativo: 'Este CNPJ não está ativo na Receita.',
  indisponivel: 'A consulta está fora do ar. Preencha manualmente ou tente mais tarde.',
  limite: 'Muitas consultas. Aguarde um minuto.',
  generico: 'Não foi possível consultar o CNPJ. Tente novamente.',
};

const MENSAGEM_POR_CODE = {
  cnpj_invalido: ERROS_CNPJ.invalido,
  cnpj_nao_encontrado: ERROS_CNPJ.naoEncontrado,
  cnpj_inativo: ERROS_CNPJ.inativo,
  cnpj_fonte_indisponivel: ERROS_CNPJ.indisponivel,
};

function texto(valor) {
  if (valor == null) return '';
  return String(valor).trim();
}

function digitosDe(valor) {
  return apenasDigitos(valor == null ? '' : String(valor));
}

export function formatarCep(valor) {
  const digitos = digitosDe(valor).slice(0, 8);
  if (digitos.length !== 8) return digitos;
  return `${digitos.slice(0, 5)}-${digitos.slice(5)}`;
}

export function formatarTelefone(valor) {
  const digitos = digitosDe(valor).slice(0, 11);
  if (digitos.length === 11) return `(${digitos.slice(0, 2)}) ${digitos.slice(2, 7)}-${digitos.slice(7)}`;
  if (digitos.length === 10) return `(${digitos.slice(0, 2)}) ${digitos.slice(2, 6)}-${digitos.slice(6)}`;
  if (digitos.length === 9) return `${digitos.slice(0, 5)}-${digitos.slice(5)}`;
  if (digitos.length === 8) return `${digitos.slice(0, 4)}-${digitos.slice(4)}`;
  return digitos;
}

function preencher(destino, campo, valor) {
  if (valor) destino[campo] = valor;
}

/** Campos que o formulário de loja já tem. Complemento não entra. */
export function mapearRespostaCnpj(resposta) {
  if (!resposta || typeof resposta !== 'object') return {};

  const campos = {};
  const razaoSocial = texto(resposta.razaoSocial);
  const nomeFantasia = texto(resposta.nomeFantasia);
  preencher(campos, 'nomeFantasia', nomeFantasia || razaoSocial);
  preencher(campos, 'razaoSocial', razaoSocial);
  preencher(campos, 'email', texto(resposta.email));
  preencher(campos, 'telefone', formatarTelefone(resposta.telefone));

  const endereco = resposta.endereco;
  if (endereco && typeof endereco === 'object') {
    preencher(campos, 'logradouro', texto(endereco.logradouro));
    preencher(campos, 'numero', texto(endereco.numero));
    preencher(campos, 'bairro', texto(endereco.bairro));
    preencher(campos, 'cidade', texto(endereco.cidade));
    const uf = texto(endereco.uf).toUpperCase();
    preencher(campos, 'estado', uf.slice(0, 2));
    preencher(campos, 'cep', formatarCep(endereco.cep));
  }

  return campos;
}

export function planejarPreenchimento(resposta, atuais) {
  const vindos = mapearRespostaCnpj(resposta);
  const vazios = {};
  const conflitos = {};

  for (const [campo, valor] of Object.entries(vindos)) {
    const atual = texto(atuais?.[campo]);
    if (!atual) vazios[campo] = valor;
    else if (atual !== valor) conflitos[campo] = valor;
  }

  return { vazios, conflitos };
}

function codigoDoCorpo(data) {
  if (!data || typeof data !== 'object') return '';
  return typeof data.code === 'string' ? data.code.trim().toLowerCase() : '';
}

function codigoDoHeader(headers) {
  if (!headers) return '';
  const bruto = typeof headers.get === 'function'
    ? headers.get('x-error-code')
    : headers['x-error-code'] ?? headers['X-Error-Code'];
  const valor = Array.isArray(bruto) ? bruto[0] : bruto;
  return typeof valor === 'string' ? valor.trim().toLowerCase() : '';
}

export function mensagemErroConsultaCnpj(error) {
  const code = codigoDoCorpo(error?.response?.data) || codigoDoHeader(error?.response?.headers);
  if (MENSAGEM_POR_CODE[code]) return MENSAGEM_POR_CODE[code];
  if (error?.response?.status === 429) return ERROS_CNPJ.limite;
  return ERROS_CNPJ.generico;
}
