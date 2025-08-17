import { render, screen, waitFor } from '@testing-library/react';
import { test, vi, expect } from 'vitest';
import { MemoryRouter, Routes, Route } from 'react-router-dom';
import JobDetail from './JobDetail';
import { JobsService } from '../generated';

test('detail viewers render and have download', async () => {
  vi.spyOn(JobsService, 'jobsGetById').mockResolvedValue({
    id: '1',
    status: 'Succeeded',
    createdAt: '',
    updatedAt: '',
    attempts: 1,
    paths: {
      input: '/api/v1/jobs/1/files/input.pdf',
      output: '/api/v1/jobs/1/files/output.json',
      fields: '/api/v1/jobs/1/files/fields',
      error: '/api/v1/jobs/1/files/error.txt',
    },
    fields: [{ key: 'company_name', value: 'ACME', confidence: 0.9 }],
  } as any);
  vi.spyOn(global, 'fetch' as any).mockImplementation((url: RequestInfo) => {
    if (typeof url === 'string' && url.endsWith('output.json')) {
      return Promise.resolve(
        new Response(
          JSON.stringify({ promptLength: 12, fieldsLength: 1 }),
          { headers: { 'Content-Type': 'application/json' } },
        ),
      );
    }
    if (typeof url === 'string' && url.endsWith('fields')) {
      return Promise.resolve(
        new Response(
          JSON.stringify([
            { key: 'company_name', value: 'ACME', confidence: 0.9 },
          ]),
          { headers: { 'Content-Type': 'application/json' } },
        ),
      );
    }
    return Promise.resolve(new Response(''));
  });
  render(
    <MemoryRouter initialEntries={['/jobs/1']}>
      <Routes>
        <Route path="/jobs/:id" element={<JobDetail />} />
      </Routes>
    </MemoryRouter>,
  );
  await screen.findByText('Job 1');
  await screen.findByText('company_name');
  await screen.getByRole('tab', { name: 'Files' }).click();
  await waitFor(() => expect(screen.getAllByTitle('Download')).toHaveLength(3));
  const previews = await screen.findAllByTitle('Preview');
  expect(previews).toHaveLength(2);
  await previews[0].click();
  await screen.findByText((content) => content.includes('promptLength'));
  screen.getAllByLabelText('Close')[0].click();
  await previews[1].click();
  await screen.findByText('confidence');
  expect(screen.queryByText('error')).toBeNull();
});
