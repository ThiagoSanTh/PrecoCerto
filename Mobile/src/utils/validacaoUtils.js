// Validação de e-mail no cliente (a API valida novamente no servidor).
const EMAIL_REGEX = /^[^\s@]+@[^\s@]+\.[^\s@]{2,}$/i;

const PROVEDORES_POPULARES = [
  'gmail.com',
  'googlemail.com',
  'hotmail.com',
  'outlook.com',
  'live.com',
  'yahoo.com',
  'yahoo.com.br',
  'icloud.com',
  'uol.com.br',
  'bol.com.br',
  'terra.com.br',
  'ig.com.br',
  'globo.com',
  'proton.me',
  'protonmail.com',
];

const DOMINIOS_TYPO_EXPLICITOS = new Set([
  'ail.com',
  'gmial.com',
  'gmai.com',
  'gamil.com',
  'gnail.com',
  'gmal.com',
  'gmil.com',
  'gmaill.com',
  'gmail.co',
  'gmail.con',
  'gmail.cm',
  'gmail.coom',
  'gmail.comn',
  'gmsil.com',
  'hotmial.com',
  'hotmal.com',
  'homail.com',
  'outlok.com',
  'outloook.com',
  'yaho.com',
  'yahooo.com',
  'uol.com.b',
  'bol.com.b',
  'terra.com.b',
  'ig.com.b',
]);

function obterDominio(email) {
  const at = email.lastIndexOf('@');
  if (at < 0 || at === email.length - 1) return null;
  return email.slice(at + 1).trim().toLowerCase();
}

function distanciaLevenshtein(a, b) {
  const n = a.length;
  const m = b.length;
  const d = Array.from({ length: n + 1 }, () => Array(m + 1).fill(0));

  for (let i = 0; i <= n; i += 1) d[i][0] = i;
  for (let j = 0; j <= m; j += 1) d[0][j] = j;

  for (let i = 1; i <= n; i += 1) {
    for (let j = 1; j <= m; j += 1) {
      const custo = a[i - 1] === b[j - 1] ? 0 : 1;
      d[i][j] = Math.min(d[i - 1][j] + 1, d[i][j - 1] + 1, d[i - 1][j - 1] + custo);
    }
  }

  return d[n][m];
}

function ehDominioSuspeito(dominio) {
  if (DOMINIOS_TYPO_EXPLICITOS.has(dominio)) return true;
  if (PROVEDORES_POPULARES.includes(dominio)) return false;

  return PROVEDORES_POPULARES.some((provedor) => {
    const distancia = distanciaLevenshtein(dominio, provedor);
    return distancia >= 1 && distancia <= 2;
  });
}

export function isEmailValido(email) {
  if (!email || typeof email !== 'string') return false;

  const trimmed = email.trim();
  if (!EMAIL_REGEX.test(trimmed)) return false;

  const dominio = obterDominio(trimmed);
  if (!dominio || ehDominioSuspeito(dominio)) return false;

  return true;
}

// Mantém apenas os dígitos do CNPJ.
export function apenasDigitosCnpj(cnpj) {
  if (!cnpj || typeof cnpj !== 'string') return '';
  return cnpj.replace(/\D/g, '');
}

// Valida os dígitos verificadores do CNPJ (sem consulta à Receita).
export function isCnpjValido(cnpj) {
  const digitos = apenasDigitosCnpj(cnpj);
  if (digitos.length !== 14) return false;
  if (/^(\d)\1{13}$/.test(digitos)) return false;

  const calcular = (base, pesos) => {
    const soma = pesos.reduce((acc, peso, i) => acc + Number(base[i]) * peso, 0);
    const resto = soma % 11;
    return resto < 2 ? 0 : 11 - resto;
  };

  const pesos1 = [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
  const pesos2 = [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
  const dv1 = calcular(digitos, pesos1);
  const dv2 = calcular(digitos.slice(0, 12) + dv1, pesos2);

  return digitos.endsWith(`${dv1}${dv2}`);
}

// Mantém apenas os dígitos (telefone, CPF, etc.).
export function apenasDigitos(valor) {
  if (!valor || typeof valor !== 'string') return '';
  return valor.replace(/\D/g, '');
}

// Telefone brasileiro: 8 dígitos (fixo) ou 9 (celular), com DDD opcional (10–11 no total).
export function isTelefoneValido(telefone) {
  const digitos = apenasDigitos(telefone);
  if (digitos.length < 8 || digitos.length > 11) return false;
  if (/^(\d)\1+$/.test(digitos)) return false;

  // Celular deve começar com 9 (sem DDD) ou ter 9 na terceira posição (com DDD).
  if (digitos.length === 9 && digitos[0] !== '9') return false;
  if (digitos.length === 11 && digitos[2] !== '9') return false;

  return true;
}

// Valida os dígitos verificadores do CPF (sem consulta à Receita).
export function isCpfValido(cpf) {
  const digitos = apenasDigitos(cpf);
  if (digitos.length !== 11) return false;
  if (/^(\d)\1{10}$/.test(digitos)) return false;

  const calcular = (base, pesoInicial) => {
    let soma = 0;
    for (let i = 0; i < base.length; i += 1) {
      soma += Number(base[i]) * (pesoInicial - i);
    }
    const resto = soma % 11;
    return resto < 2 ? 0 : 11 - resto;
  };

  const dv1 = calcular(digitos.slice(0, 9), 10);
  const dv2 = calcular(digitos.slice(0, 9) + dv1, 11);

  return digitos.endsWith(`${dv1}${dv2}`);
}
