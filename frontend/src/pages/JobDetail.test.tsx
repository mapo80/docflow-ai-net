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
      error: '/api/v1/jobs/1/files/error.txt',
    },
  } as any);
  vi.spyOn(global, 'fetch' as any).mockImplementation((url: RequestInfo) => {
    if (typeof url === 'string' && url.endsWith('output.json')) {
      return Promise.resolve(
        new Response(
          JSON.stringify([
            {
              FieldName: 'company_name',
              Value: 'ACME',
              Confidence: 0.9,
              Spans: [{ Page: 0, BBox: { X: 0, Y: 0, W: 1, H: 1 } }],
            },
          ]),
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
  await waitFor(() => expect(screen.getAllByTitle('Download')).toHaveLength(2));
  expect(screen.queryByText('error')).toBeNull();
});
