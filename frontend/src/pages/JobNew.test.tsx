import { validateFile, buildPayload, submitPayload } from './JobNew';
import { test, expect, vi } from 'vitest';
import { ApiError } from '../generated';

vi.mock('../generated/core/request', () => ({
  request: vi.fn(),
}));

import { request as __request } from '../generated/core/request';

test('validateFile', () => {
  const ok = new File(['a'], 'a.pdf', { type: 'application/pdf' });
  expect(validateFile(ok)).toBeUndefined();
  const badExt = new File(['a'], 'a.exe');
  expect(validateFile(badExt)).toBe('Invalid extension');
  const big = new File([new Uint8Array(11 * 1024 * 1024)], 'b.pdf');
  expect(validateFile(big)).toBe('File too large');
});

test('submitPayload', async () => {
  const payload = await buildPayload(new File(['a'], 'a.pdf'), 'm', 't', 'eng', 'tesseract');
  (__request as any).mockResolvedValueOnce({ job_id: '1', status: 'Queued' });
  const res = await submitPayload(payload);
  expect(res).toMatchObject({ job_id: '1', status: 'Queued' });
  (__request as any).mockRejectedValueOnce(
    new ApiError(
      { method: 'POST', url: '/jobs' } as any,
      { url: '', status: 429, statusText: 'Too Many Requests', body: {} },
      'Too Many Requests'
    )
  );
  await expect(submitPayload(payload)).rejects.toBeInstanceOf(ApiError);
});
