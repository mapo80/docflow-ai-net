import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { describe, it, expect, vi, afterEach } from 'vitest';
import RuleBuilderPage from '../pages/RuleBuilderPage';

const { mockNotify } = vi.hoisted(() => ({ mockNotify: vi.fn() }));
const { mockNav } = vi.hoisted(() => ({ mockNav: vi.fn() }));

vi.mock('../components/notification', () => ({ notify: mockNotify, default: mockNotify }));
vi.mock('react-router-dom', async (importOriginal) => {
  const actual = await importOriginal();
  return { ...actual, useNavigate: () => mockNav };
});

afterEach(() => {
  vi.clearAllMocks();
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  delete (global as any).fetch;
});

describe('RuleBuilderPage integration', () => {
  it('compiles blocks, runs properties and creates rule', async () => {
    const fetchMock = vi.fn((input: RequestInfo, init?: RequestInit) => {
      const url = typeof input === 'string' ? input : input.url;
      if (url === '/api/v1/rulebuilder/compile' && init?.method === 'POST') {
        return Promise.resolve(
          new Response(JSON.stringify({ code: '// compiled' }), {
            status: 200,
            headers: { 'Content-Type': 'application/json' },
          }),
        );
      }
      if (url === '/api/v1/rules/properties/run-from-blocks' && init?.method === 'POST') {
        return Promise.resolve(
          new Response(JSON.stringify({ trials: 100, passed: 100, failed: 0, failures: [] }), {
            status: 200,
            headers: { 'Content-Type': 'application/json' },
          }),
        );
      }
      if (url === '/api/v1/rules' && init?.method === 'POST') {
        return Promise.resolve(
          new Response(JSON.stringify({ id: 'r123' }), {
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
        <RuleBuilderPage />
      </MemoryRouter>,
    );

    fireEvent.click(screen.getByText('Add Exists'));
    fireEvent.click(screen.getByText('Add Compare'));
    fireEvent.click(screen.getByText('Add Regex'));
    fireEvent.click(screen.getByText('Add Set'));
    fireEvent.click(screen.getByText('Add Normalize'));
    fireEvent.click(screen.getByText('Add Map'));
    fireEvent.click(screen.getByText('Add Deduce'));
    fireEvent.click(screen.getAllByText('Remove')[0]);

    fireEvent.click(screen.getByText('Compile'));
    await waitFor(() =>
      expect(fetchMock).toHaveBeenCalledWith('/api/v1/rulebuilder/compile', expect.objectContaining({ method: 'POST' })),
    );

    fireEvent.click(screen.getByText('Property Check'));
    await waitFor(() =>
      expect(fetchMock).toHaveBeenCalledWith(
        '/api/v1/rules/properties/run-from-blocks',
        expect.objectContaining({ method: 'POST' }),
      ),
    );

    fireEvent.click(screen.getByText('Create Rule'));
    await waitFor(() =>
      expect(fetchMock).toHaveBeenCalledWith('/api/v1/rules', expect.objectContaining({ method: 'POST' })),
    );
    await waitFor(() => expect(mockNotify).toHaveBeenCalledWith('success', 'Rule created successfully.'));
    await waitFor(() => expect(mockNav).toHaveBeenCalledWith('/rules/r123'));
  });
});
