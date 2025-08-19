import { render, waitFor } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import ApiErrorProvider from './ApiErrorProvider';
import { notify } from './notification';

const { mockNotify } = vi.hoisted(() => ({ mockNotify: vi.fn() }));
vi.mock('./notification', () => ({ notify: mockNotify, default: mockNotify }));

describe('ApiErrorProvider', () => {
  it('shows notification on fetch error', async () => {
    const originalFetch = global.fetch;
    global.fetch = vi.fn().mockResolvedValue(
      new Response(JSON.stringify({ message: 'Oops' }), {
        status: 500,
        headers: { 'Content-Type': 'application/json' },
      })
    ) as any;
    render(
      <ApiErrorProvider>
        <div />
      </ApiErrorProvider>
    );
    await fetch('/api/test');
    await waitFor(() => {
      expect(notify).toHaveBeenCalledWith('error', 'Oops');
    });
    global.fetch = originalFetch;
  });
});
