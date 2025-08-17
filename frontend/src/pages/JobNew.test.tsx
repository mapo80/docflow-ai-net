import { isValidJson, validateFile, buildPayload, submitPayload } from './JobNew';
import { test, expect, vi } from 'vitest';

test('isValidJson', () => {
  expect(isValidJson('{}')).toBe(true);
  expect(isValidJson('{')).toBe(false);
});

test('validateFile', () => {
  const ok = new File(['a'], 'a.pdf', { type: 'application/pdf' });
  expect(validateFile(ok)).toBeUndefined();
  const badExt = new File(['a'], 'a.exe');
  expect(validateFile(badExt)).toBe('Invalid extension');
  const big = new File([new Uint8Array(11 * 1024 * 1024)], 'b.pdf');
  expect(validateFile(big)).toBe('File too large');
});

test('submitPayload branches', async () => {
  const payload = await buildPayload(new File(['a'], 'a.pdf'), 'p', '{}');
  const fetchMock = vi
    .spyOn(global, 'fetch' as any)
    .mockResolvedValueOnce({ ok: true, json: async () => ({ job_id: '1', status: 'Succeeded' }) } as any)
    .mockResolvedValueOnce({ ok: true, json: async () => ({ job_id: '2', status: 'Pending' }) } as any)
    .mockResolvedValueOnce({ ok: false, text: async () => '' } as any);
  let res = await submitPayload(payload, true);
  expect(res).toMatchObject({ job_id: '1', status: 'Succeeded' });
  res = await submitPayload(payload, false);
  expect(res).toMatchObject({ job_id: '2', status: 'Pending' });
  await expect(submitPayload(payload, true)).rejects.toThrow('Submit failed');
  fetchMock.mockRestore();
});

