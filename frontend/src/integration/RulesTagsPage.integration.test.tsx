import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, afterEach } from 'vitest';
import RulesTagsPage from '../pages/RulesTagsPage';

const { mockNotify } = vi.hoisted(() => ({ mockNotify: vi.fn() }));

vi.mock('../components/notification', () => ({ notify: mockNotify, default: mockNotify }));

afterEach(() => {
  vi.clearAllMocks();
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  delete (global as any).fetch;
});

describe('RulesTagsPage integration', () => {
  it('loads, creates and deletes suites and tags', async () => {
    const fetchMock = vi.fn((input: RequestInfo, init?: RequestInit) => {
      const url = typeof input === 'string' ? input : input.url;
      if (url === '/api/v1/suites' && (!init || init.method === 'GET')) {
        return Promise.resolve(
          new Response(JSON.stringify([{ id: 's1', name: 'suite1' }]), {
            status: 200,
            headers: { 'Content-Type': 'application/json' },
          }),
        );
      }
      if (url === '/api/v1/tags' && (!init || init.method === 'GET')) {
        return Promise.resolve(
          new Response(JSON.stringify([{ id: 't1', name: 'tag1' }]), {
            status: 200,
            headers: { 'Content-Type': 'application/json' },
          }),
        );
      }
      if (url === '/api/v1/suites' && init?.method === 'POST') {
        return Promise.resolve(new Response('{}', { status: 200, headers: { 'Content-Type': 'application/json' } }));
      }
      if (url === '/api/v1/tags' && init?.method === 'POST') {
        return Promise.resolve(new Response('{}', { status: 200, headers: { 'Content-Type': 'application/json' } }));
      }
      if (url === '/api/v1/suites/s1' && init?.method === 'DELETE') {
        return Promise.resolve(new Response('{}', { status: 200, headers: { 'Content-Type': 'application/json' } }));
      }
      if (url === '/api/v1/tags/t1' && init?.method === 'DELETE') {
        return Promise.resolve(new Response('{}', { status: 200, headers: { 'Content-Type': 'application/json' } }));
      }
      return Promise.reject(new Error('unknown'));
    });
    global.fetch = fetchMock as any;

    render(<RulesTagsPage />);

    await waitFor(() => expect(fetchMock).toHaveBeenCalledWith('/api/v1/suites', expect.anything()));
    await waitFor(() => expect(fetchMock).toHaveBeenCalledWith('/api/v1/tags', expect.anything()));
    await waitFor(() => expect(screen.getByText('suite1')).toBeInTheDocument());
    await waitFor(() => expect(screen.getByText('tag1')).toBeInTheDocument());

    fireEvent.change(screen.getAllByPlaceholderText('Suite name')[0], { target: { value: 'suite2' } });
    fireEvent.click(screen.getByRole('button', { name: 'Add Suite' }));
    await waitFor(() =>
      expect(fetchMock).toHaveBeenCalledWith('/api/v1/suites', expect.objectContaining({ method: 'POST' })),
    );
    await waitFor(() => expect(mockNotify).toHaveBeenCalledWith('success', 'Suite created successfully.'));

    fireEvent.change(screen.getAllByPlaceholderText('Tag name')[0], { target: { value: 'tag2' } });
    fireEvent.click(screen.getByRole('button', { name: 'Add Tag' }));
    await waitFor(() => expect(fetchMock).toHaveBeenCalledWith('/api/v1/tags', expect.objectContaining({ method: 'POST' })));
    await waitFor(() => expect(mockNotify).toHaveBeenCalledWith('success', 'Tag created successfully.'));

    fireEvent.click(screen.getAllByText('Delete')[0]);
    await waitFor(() =>
      expect(fetchMock).toHaveBeenCalledWith('/api/v1/suites/s1', expect.objectContaining({ method: 'DELETE' })),
    );

    fireEvent.click(screen.getAllByText('Delete')[1]);
    await waitFor(() => expect(fetchMock).toHaveBeenCalledWith('/api/v1/tags/t1', expect.objectContaining({ method: 'DELETE' })));
  });
});
