import { render, screen, waitFor, cleanup } from '@testing-library/react';
import { test, vi, expect, afterEach } from 'vitest';
import { MemoryRouter, Routes, Route } from 'react-router-dom';
import JobDetail from './JobDetail';
import { JobsService } from '../generated';

afterEach(() => {
  cleanup();
  vi.clearAllMocks();
});

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
      fields: '/api/v1/jobs/1/files/fields.json',
      error: '/api/v1/jobs/1/files/error.txt',
    },
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
    if (typeof url === 'string' && url.endsWith('fields.json')) {
      return Promise.resolve(
        new Response(
          JSON.stringify([
            { FieldName: 'company_name', Value: 'ACME', Confidence: 0.9 },
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
  await screen.findByText('Confidence');
  expect(screen.queryByText('error')).toBeNull();
});

test('does not fallback to output fields when fields path missing', async () => {
  vi.spyOn(JobsService, 'jobsGetById').mockResolvedValue({
    id: '1',
    status: 'Succeeded',
    createdAt: '',
    updatedAt: '',
    attempts: 1,
    paths: {
      input: '/api/v1/jobs/1/files/input.pdf',
      output: '/api/v1/jobs/1/files/output.json',
    },
  } as any);
  const fetchMock = vi
    .spyOn(global, 'fetch' as any)
    .mockImplementation((url: RequestInfo) => {
      if (typeof url === 'string' && url.endsWith('output.json')) {
        return Promise.resolve(
          new Response(
            JSON.stringify([
              { FieldName: 'company_name', Value: 'ACME', Confidence: 0.9 },
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
  expect(fetchMock).not.toHaveBeenCalled();
  expect(screen.queryByText('company_name')).toBeNull();
});
