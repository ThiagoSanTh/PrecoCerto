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
