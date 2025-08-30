import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { describe, it, expect, vi, afterEach } from 'vitest';
import RulesPage from '../pages/RulesPage';

const { mockNotify } = vi.hoisted(() => ({ mockNotify: vi.fn() }));
const { mockNav } = vi.hoisted(() => ({ mockNav: vi.fn() }));

vi.mock('../components/notification', () => ({ notify: mockNotify, default: mockNotify }));
vi.mock('react-router-dom', async (importOriginal) => {
  const actual = await importOriginal();
  return { ...actual, useNavigate: () => mockNav };
});

const rules = [{ id: 'r1', name: 'rule1', version: 1, enabled: true }];

afterEach(() => {
  vi.clearAllMocks();
  // cleanup fetch mock between tests
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  delete (global as any).fetch;
});

describe('RulesPage integration', () => {
  it('loads, searches and clones using REST API', async () => {
    const fetchMock = vi.fn((input: RequestInfo, init?: RequestInit) => {
      const url = typeof input === 'string' ? input : input.url;
      if (url.startsWith('/api/v1/rules') && (!init || init.method === 'GET')) {
        return Promise.resolve(
          new Response(JSON.stringify(rules), {
            status: 200,
            headers: { 'Content-Type': 'application/json' },
          }),
        );
      }
      if (url === '/api/v1/rules/r1/clone' && init?.method === 'POST') {
        return Promise.resolve(
          new Response(JSON.stringify({ id: 'r2' }), {
            status: 200,
            headers: { 'Content-Type': 'application/json' },
          }),
        );
      }
      return Promise.reject(new Error('unknown'));
    });
    global.fetch = fetchMock as any;

    render(
      <MemoryRouter>
        <RulesPage />
      </MemoryRouter>,
    );

    await waitFor(() => expect(fetchMock).toHaveBeenCalledWith('/api/v1/rules?search=', expect.anything()));
    await waitFor(() => expect(screen.getByText('rule1')).toBeInTheDocument());

    fireEvent.change(screen.getByPlaceholderText('search'), { target: { value: 'x' } });
    await waitFor(() => expect(fetchMock).toHaveBeenCalledWith('/api/v1/rules?search=x', expect.anything()));

    fireEvent.click(screen.getByText('Clone + tests'));
    await waitFor(() => expect(fetchMock).toHaveBeenCalledWith('/api/v1/rules/r1/clone', expect.objectContaining({ method: 'POST' })));
    await waitFor(() => expect(mockNotify).toHaveBeenCalledWith('success', 'Rule cloned successfully.'));
    await waitFor(() => expect(mockNav).toHaveBeenCalledWith('/rules/r2'));

    fireEvent.click(screen.getByText('No-code Builder'));
    await waitFor(() => expect(mockNav).toHaveBeenCalledWith('/rules/builder'));
  });

  it('handles clone error', async () => {
    const fetchMock = vi.fn((input: RequestInfo, init?: RequestInit) => {
      const url = typeof input === 'string' ? input : input.url;
      if (url.startsWith('/api/v1/rules') && (!init || init.method === 'GET')) {
        return Promise.resolve(
          new Response(JSON.stringify(rules), {
            status: 200,
            headers: { 'Content-Type': 'application/json' },
          }),
        );
      }
      if (url === '/api/v1/rules/r1/clone' && init?.method === 'POST') {
        return Promise.reject(new Error('fail'));
      }
      return Promise.reject(new Error('unknown'));
    });
    global.fetch = fetchMock as any;

    render(
      <MemoryRouter>
        <RulesPage />
      </MemoryRouter>,
    );

    await waitFor(() => expect(screen.getAllByText('Clone + tests')[0]).toBeInTheDocument());
    fireEvent.click(screen.getAllByText('Clone + tests')[0]);
    await waitFor(() => expect(mockNotify).toHaveBeenCalledWith('error', 'Failed to clone rule.'));
  });

  it('handles load error', async () => {
    const fetchMock = vi.fn(() => Promise.reject(new Error('network')));
    global.fetch = fetchMock as any;

    render(
      <MemoryRouter>
        <RulesPage />
      </MemoryRouter>,
    );

    await waitFor(() => expect(mockNotify).toHaveBeenCalledWith('error', 'Failed to load rules.'));
  });
});
