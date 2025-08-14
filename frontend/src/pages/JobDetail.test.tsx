import { render, screen, waitFor } from '@testing-library/react';
import { test, vi, expect } from 'vitest';
import { MemoryRouter, Routes, Route } from 'react-router-dom';
import JobDetail from './JobDetail';
import { DefaultService } from '../generated';

test('detail viewers render and have download', async () => {
  vi.spyOn(DefaultService, 'getJob').mockResolvedValue({
    id: '1',
    status: 'Succeeded',
    createdAt: '',
    updatedAt: '',
    attempts: 1,
    paths: { output: '/out.json', error: '/err.txt' },
  } as any);
  vi.spyOn(global, 'fetch' as any).mockImplementation((url: RequestInfo) => {
    if (typeof url === 'string' && url.endsWith('.json')) {
      return Promise.resolve(new Response(JSON.stringify({ a: 1 })));
    }
    return Promise.resolve(new Response('err'));
  });
  render(
    <MemoryRouter initialEntries={['/jobs/1']}>
      <Routes>
        <Route path="/jobs/:id" element={<JobDetail />} />
      </Routes>
    </MemoryRouter>,
  );
  await screen.findByText('Job 1');
  await waitFor(() => expect(screen.getAllByText('Scarica')).toHaveLength(2));
});
