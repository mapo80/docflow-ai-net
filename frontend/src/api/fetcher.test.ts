import { fetcher, HttpError } from './fetcher';
import { describe, it, expect } from 'vitest';

describe('fetcher', () => {
  it('handles 429 with retry-after header', async () => {
    global.fetch = async () =>
      new Response(JSON.stringify({ errorCode: 'queue_full' }), {
        status: 429,
        headers: { 'Retry-After': '5', 'Content-Type': 'application/json' },
      });
    await expect(fetcher('/test', { method: 'GET' })).rejects.toMatchObject({
      status: 429,
      retryAfter: 5,
    });
  });

  it('handles 413 payload too large', async () => {
    global.fetch = async () =>
      new Response(JSON.stringify({ errorCode: 'payload_too_large' }), {
        status: 413,
        headers: { 'Content-Type': 'application/json' },
      });
    await expect(fetcher('/test', { method: 'GET' })).rejects.toBeInstanceOf(HttpError);
  });
});
