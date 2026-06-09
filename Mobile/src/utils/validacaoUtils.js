// Validação simples de e-mail no cliente (a API valida novamente no servidor).
const EMAIL_REGEX = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

export function isEmailValido(email) {
  if (!email || typeof email !== 'string') return false;
  return EMAIL_REGEX.test(email.trim());
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
