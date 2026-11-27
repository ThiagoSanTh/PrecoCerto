import assert from 'node:assert/strict';
import test from 'node:test';
import { mapApiError } from './apiErrorUtils.js';

test('login não traduz EMAIL_NOT_FOUND para texto que revela a conta', () => {
  const mapped = mapApiError({
    response: {
      status: 401,
      data: {
        code: 'EMAIL_NOT_FOUND',
        message: 'E-mail ou senha incorretos.',
      },
    },
  });

  assert.notEqual(mapped.title, 'E-mail não encontrado');
  assert.equal(mapped.message, 'E-mail ou senha incorretos.');
  assert.doesNotMatch(mapped.message, /não existe/i);
});

test('409 com mensagem específica de loja preserva o texto do backend', () => {
  const mapped = mapApiError({
    response: {
      status: 409,
      data: {
        message: 'Você já possui uma loja cadastrada. Saia e entre novamente para acessar o painel da loja.',
      },
    },
  });

  assert.equal(
    mapped.message,
    'Você já possui uma loja cadastrada. Saia e entre novamente para acessar o painel da loja.'
  );
  assert.notEqual(mapped.code, 'EMAIL_ALREADY_REGISTERED');
});

test('409 sem mensagem ou de e-mail duplicado continua mapeando para EMAIL_ALREADY_REGISTERED', () => {
  const semMsg = mapApiError({ response: { status: 409, data: {} } });
  assert.equal(semMsg.code, 'EMAIL_ALREADY_REGISTERED');

  const emailDup = mapApiError({
    response: { status: 409, data: { message: 'Este e-mail já está cadastrado.' } },
  });
  assert.equal(emailDup.code, 'EMAIL_ALREADY_REGISTERED');
});

