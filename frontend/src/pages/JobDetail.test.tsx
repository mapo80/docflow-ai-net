import { render, waitFor, screen } from '@testing-library/react';
import { MemoryRouter, Routes, Route } from 'react-router-dom';
import { test, vi } from 'vitest';
import JobDetail from './JobDetail';
import { JobsService, ApiError } from '../generated';
import ApiErrorProvider from '../components/ApiErrorProvider';

const { mockNotify } = vi.hoisted(() => ({ mockNotify: vi.fn() }));
vi.mock('../components/notification', () => ({ notify: mockNotify, default: mockNotify }));

vi.mock('antd', () => ({
  Alert: ({ message }: any) => <div>{message}</div>,
  notification: { error: vi.fn() },
}));

test('shows error when job is missing', async () => {
  vi.spyOn(JobsService, 'jobsGetById').mockRejectedValueOnce(
    new ApiError(
      { method: 'GET', url: '/api/v1/jobs/1' } as any,
      { url: '', status: 404, statusText: 'Not Found', body: {} },
      'Not Found'
    ),
  );
  render(
    <ApiErrorProvider>
      <MemoryRouter initialEntries={['/jobs/1']}>
        <Routes>
          <Route path="/jobs/:id" element={<JobDetail />} />
        </Routes>
      </MemoryRouter>
    </ApiErrorProvider>,
  );
  await waitFor(() => screen.getByText('Job not found'));
});
