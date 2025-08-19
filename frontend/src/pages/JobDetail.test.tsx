import { render, screen, waitFor } from '@testing-library/react';
import { test, vi, expect } from 'vitest';
import { MemoryRouter, Routes, Route } from 'react-router-dom';
import JobDetail from './JobDetail';
import { JobsService, ModelsService, TemplatesService } from '../generated';
import ApiErrorProvider from '../components/ApiErrorProvider';

test('detail viewers render and have download', async () => {
  vi.spyOn(JobsService, 'jobsGetById').mockResolvedValue({
    id: '1',
    status: 'Succeeded',
    createdAt: '',
    updatedAt: '',
    attempts: 1,
    model: 'm',
    templateToken: 't',
    paths: {
      input: '/api/v1/jobs/1/files/input.pdf',
      output: '/api/v1/jobs/1/files/output.json',
      error: '/api/v1/jobs/1/files/error.txt',
    },
  } as any);
  vi.spyOn(ModelsService, 'modelsList').mockResolvedValue([{ name: 'm' } as any]);
  vi.spyOn(TemplatesService, 'templatesList').mockResolvedValue({
    items: [{ token: 't', name: 'template' }],
  } as any);
  vi.spyOn(global, 'fetch' as any).mockImplementation((url: RequestInfo) => {
    if (typeof url === 'string' && url.endsWith('output.json')) {
      return Promise.resolve(
        new Response(
          JSON.stringify({ template: 't', model: 'm' }),
          { headers: { 'Content-Type': 'application/json' } },
        ),
      );
    }
    return Promise.resolve(new Response(''));
  });
  render(
    <ApiErrorProvider>
      <MemoryRouter initialEntries={['/jobs/1']}>
        <Routes>
          <Route path="/jobs/:id" element={<JobDetail />} />
        </Routes>
      </MemoryRouter>
    </ApiErrorProvider>,
  );
  await screen.findByText('Job 1');
  await screen.getByRole('tab', { name: 'Files' }).click();
  await waitFor(() => expect(screen.getAllByTitle('Download')).toHaveLength(2));
  const previews = await screen.findAllByTitle('Preview');
  expect(previews).toHaveLength(1);
  await previews[0].click();
  await screen.findByText((content) => content.includes('template'));
  const closeBtns = await screen.findAllByLabelText('Close');
  closeBtns[0].click();
  expect(screen.queryByText('error')).toBeNull();
});

test('shows success badge for new job', async () => {
  vi.spyOn(JobsService, 'jobsGetById').mockResolvedValue({
    id: '1',
    status: 'Queued',
    createdAt: '',
    updatedAt: '',
    attempts: 0,
    model: 'm',
    templateToken: 't',
  } as any);
  vi.spyOn(ModelsService, 'modelsList').mockResolvedValue([]);
  vi.spyOn(TemplatesService, 'templatesList').mockResolvedValue({ items: [] } as any);
  vi.spyOn(global, 'fetch' as any).mockResolvedValue(new Response(''));
  render(
    <ApiErrorProvider>
      <MemoryRouter initialEntries={[{ pathname: '/jobs/1', state: { newJob: true } }]}> 
        <Routes>
          <Route path="/jobs/:id" element={<JobDetail />} />
        </Routes>
      </MemoryRouter>
    </ApiErrorProvider>,
  );
  await screen.findByText('Job created successfully.');
});
