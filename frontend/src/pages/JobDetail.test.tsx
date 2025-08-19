import { render, waitFor, screen } from '@testing-library/react';
import { MemoryRouter, Routes, Route } from 'react-router-dom';
import { test, vi, expect } from 'vitest';
import JobDetail from './JobDetail';
import { JobsService, ApiError } from '../generated';
import ApiErrorProvider from '../components/ApiErrorProvider';

const { mockNotify } = vi.hoisted(() => ({ mockNotify: vi.fn() }));
vi.mock('../components/notification', () => ({ notify: mockNotify, default: mockNotify }));

vi.mock('antd', async () => {
  const actual = await vi.importActual<any>('antd');
  return {
    ...actual,
    Alert: ({ message }: any) => <div>{message}</div>,
  };
});

const getByIdSpy = vi.spyOn(JobsService, 'jobsGetById');

test('shows error when job is missing', async () => {
  getByIdSpy.mockRejectedValueOnce(
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

test('does not show success notification when navigating to detail', async () => {
  mockNotify.mockClear();
  getByIdSpy.mockResolvedValueOnce({
    id: '1',
    status: 'Succeeded',
    attempts: 1,
    model: 'm',
    templateToken: 't',
    createdAt: '',
    updatedAt: '',
  } as any);
  render(
    <ApiErrorProvider>
      <MemoryRouter initialEntries={[{ pathname: '/jobs/1', state: { newJob: true } }]}> 
        <Routes>
          <Route path="/jobs/:id" element={<JobDetail />} />
        </Routes>
      </MemoryRouter>
    </ApiErrorProvider>,
  );
  await waitFor(() => screen.getByText('Attempts'));
  expect(mockNotify).not.toHaveBeenCalled();
});
