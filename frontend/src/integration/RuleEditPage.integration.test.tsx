import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { MemoryRouter, Routes, Route } from 'react-router-dom';
import { describe, it, expect, vi, afterEach } from 'vitest';
import RuleEditPage from '../pages/RuleEditPage';

afterEach(() => {
  vi.clearAllMocks();
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  delete (global as any).fetch;
});

describe('RuleEditPage integration', () => {
  it('loads rule, runs property checks and tests', async () => {
    const fetchMock = vi.fn((input: RequestInfo, init?: RequestInit) => {
      const url = typeof input === 'string' ? input : input.url;
      if (url === '/api/v1/rules/r1' && (!init || init.method === 'GET')) {
        return Promise.resolve(
          new Response(JSON.stringify({ id: 'r1', name: 'rule1', code: '', isBuiltin: false, enabled: true, version: '1', updatedAt: '' }), {
            status: 200,
            headers: { 'Content-Type': 'application/json' },
          }),
        );
      }
      if (url.startsWith('/api/v1/rules/r1/tests') && (!init || init.method === 'GET')) {
        return Promise.resolve(
          new Response(JSON.stringify({ items: [] }), {
            status: 200,
            headers: { 'Content-Type': 'application/json' },
          }),
        );
      }
      if (url === '/api/v1/suites' && (!init || init.method === 'GET')) {
        return Promise.resolve(new Response(JSON.stringify([]), { status: 200, headers: { 'Content-Type': 'application/json' } }));
      }
      if (url === '/api/v1/tags' && (!init || init.method === 'GET')) {
        return Promise.resolve(new Response(JSON.stringify([]), { status: 200, headers: { 'Content-Type': 'application/json' } }));
      }
      if (url.startsWith('/api/v1/rules/r1/properties/run') && init?.method === 'POST') {
        return Promise.resolve(
          new Response(JSON.stringify({ trials: 100, passed: 100, failed: 0, failures: [] }), {
            status: 200,
            headers: { 'Content-Type': 'application/json' },
          }),
        );
      }
      if (url === '/api/v1/rules/r1/tests/run' && init?.method === 'POST') {
        return Promise.resolve(
          new Response(JSON.stringify([{ name: 't1', passed: true }]), {
            status: 200,
            headers: { 'Content-Type': 'application/json' },
          }),
        );
      }
      return Promise.reject(new Error('unknown'));
    });
    global.fetch = fetchMock as any;

    render(
      <MemoryRouter initialEntries={['/rules/r1']}>
        <Routes>
          <Route path="/rules/:id" element={<RuleEditPage />} />
        </Routes>
      </MemoryRouter>,
    );

    await waitFor(() => expect(fetchMock).toHaveBeenCalledWith('/api/v1/rules/r1', expect.anything()));
    await waitFor(() => expect(fetchMock).toHaveBeenCalledWith('/api/v1/rules/r1/tests?page=1&pageSize=100', expect.anything()));
    await waitFor(() => expect(fetchMock).toHaveBeenCalledWith('/api/v1/suites', expect.anything()));
    await waitFor(() => expect(fetchMock).toHaveBeenCalledWith('/api/v1/tags', expect.anything()));

    fireEvent.click(screen.getByRole('button', { name: 'Run' }));
    await waitFor(() =>
      expect(fetchMock).toHaveBeenCalledWith(
        expect.stringContaining('/api/v1/rules/r1/properties/run'),
        expect.objectContaining({ method: 'POST' }),
      ),
    );

    fireEvent.click(screen.getByRole('button', { name: 'Run All' }));
    await waitFor(() =>
      expect(fetchMock).toHaveBeenCalledWith('/api/v1/rules/r1/tests/run', expect.objectContaining({ method: 'POST' })),
    );
  });
});
