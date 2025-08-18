import { render, screen, waitFor } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import ApiErrorProvider from './ApiErrorProvider';

describe('ApiErrorProvider', () => {
  it('shows badge on fetch error', async () => {
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
      expect(screen.getByText('Oops')).toBeInTheDocument();
    });
    global.fetch = originalFetch;
  });
});
