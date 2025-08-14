import { isValidJson, validateFile, buildFormData, submitFormData } from './JobNew';
import { test, expect, vi } from 'vitest';

vi.mock('../api/fetcher', () => {
  class MockHttpError extends Error {
    status: number;
    data: any;
    retryAfter?: number;
    constructor(status: number, data: any, retryAfter?: number) {
      super();
      this.status = status;
      this.data = data;
      this.retryAfter = retryAfter;
    }
  }
  return {
    fetcher: vi.fn(),
    HttpError: MockHttpError,
  };
});

import { fetcher, HttpError } from '../api/fetcher';

test('isValidJson', () => {
  expect(isValidJson('{}')).toBe(true);
  expect(isValidJson('{')).toBe(false);
});

test('validateFile', () => {
  const ok = new File(['a'], 'a.txt', { type: 'text/plain' });
  expect(validateFile(ok)).toBeUndefined();
  const badExt = new File(['a'], 'a.exe');
  expect(validateFile(badExt)).toBe('Estensione non valida');
  const big = new File([new Uint8Array(11 * 1024 * 1024)], 'b.txt');
  expect(validateFile(big)).toBe('File troppo grande');
});

test('submitFormData branches', async () => {
  const form = buildFormData(new File(['a'], 'a.txt'), 'p', '{}');
  (fetcher as any).mockResolvedValueOnce(
    new Response(JSON.stringify({ id: '1', status: 'Succeeded' }), { status: 200 })
  );
  let res = await submitFormData(form, true);
  expect(res.status).toBe(200);
  (fetcher as any).mockResolvedValueOnce(
    new Response(JSON.stringify({ id: '2' }), { status: 202 })
  );
  res = await submitFormData(form, false);
  expect(res.status).toBe(202);
  (fetcher as any).mockRejectedValueOnce(
    new HttpError(429, { errorCode: 'immediate_capacity' }, 5)
  );
  await expect(submitFormData(form, true)).rejects.toBeInstanceOf(HttpError);
});
