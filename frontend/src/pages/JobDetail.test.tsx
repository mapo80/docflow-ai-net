import { render, waitFor, screen, cleanup, fireEvent } from '@testing-library/react';
import { MemoryRouter, Routes, Route } from 'react-router-dom';
import { test, vi, expect, afterEach } from 'vitest';
import JobDetail from './JobDetail';
import {
  JobsService,
  ApiError,
  ModelsService,
  TemplatesService,
} from '../generated';
import ApiErrorProvider from '../components/ApiErrorProvider';

const { mockNotify } = vi.hoisted(() => ({ mockNotify: vi.fn() }));
vi.mock('../components/notification', () => ({ notify: mockNotify, default: mockNotify }));

vi.mock('antd', async () => {
  const actual = await vi.importActual<any>('antd');
  return { ...actual, Alert: ({ message }: any) => <div>{message}</div> };
});

const getByIdSpy = vi.spyOn(JobsService, 'jobsGetById');
vi.spyOn(ModelsService, 'modelsList').mockResolvedValue([] as any);
vi.spyOn(TemplatesService, 'templatesList').mockResolvedValue({ items: [] } as any);

afterEach(() => {
  cleanup();
  vi.clearAllMocks();
});

test('shows error when job is missing', async () => {
  getByIdSpy.mockReset();
  getByIdSpy.mockRejectedValueOnce(
    new ApiError(
      { method: 'GET', url: '/api/v1/jobs/1' } as any,
      { url: '', status: 404, statusText: 'Not Found', body: {} },
      'Not Found',
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
  getByIdSpy.mockReset();
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

test('shows duration in seconds with suffix', async () => {
  getByIdSpy.mockReset();
  getByIdSpy.mockResolvedValueOnce({
    id: '1',
    status: 'Succeeded',
    attempts: 1,
    model: 'm',
    templateToken: 't',
    createdAt: '',
    updatedAt: '',
    metrics: { durationMs: 1234 },
  } as any);
  render(
    <ApiErrorProvider>
      <MemoryRouter initialEntries={['/jobs/1']}>
        <Routes>
          <Route path="/jobs/:id" element={<JobDetail />} />
        </Routes>
      </MemoryRouter>
    </ApiErrorProvider>,
  );
  await waitFor(() => screen.getByText('Duration'));
  expect(screen.getByText('1.23 sec')).toBeInTheDocument();
});

test('shows reload and cancel only when running', async () => {
  getByIdSpy.mockReset();
  getByIdSpy.mockResolvedValue({
    id: '1',
    status: 'Running',
    attempts: 1,
    model: 'm',
    templateToken: 't',
    createdAt: '',
    updatedAt: '',
    paths: { input: '/input.pdf' },
  } as any);
  const fetchSpy = vi.spyOn(global, 'fetch').mockResolvedValue({
    ok: true,
    headers: new Headers(),
  } as any);
  render(
    <ApiErrorProvider>
      <MemoryRouter initialEntries={['/jobs/1']}>
        <Routes>
          <Route path="/jobs/:id" element={<JobDetail />} />
        </Routes>
      </MemoryRouter>
    </ApiErrorProvider>,
  );
  await waitFor(() => screen.getByText('Reload'));
  expect(screen.getByText('Cancel')).toBeInTheDocument();
  const filesTab = screen.getAllByRole('tab', { name: 'Files' })[0];
  fireEvent.click(filesTab);
  await waitFor(() => screen.getByText('input'));
  expect(fetchSpy).not.toHaveBeenCalled();
  fetchSpy.mockRestore();

  cleanup();

  getByIdSpy.mockReset();
  getByIdSpy.mockResolvedValue({
    id: '1',
    status: 'Succeeded',
    attempts: 1,
    model: 'm',
    templateToken: 't',
    createdAt: '',
    updatedAt: '',
  } as any);
  const fetchSpy2 = vi
    .spyOn(global, 'fetch')
    .mockResolvedValue({ ok: true, headers: new Headers() } as any);
  render(
    <ApiErrorProvider>
      <MemoryRouter initialEntries={['/jobs/1']}>
        <Routes>
          <Route path="/jobs/:id" element={<JobDetail />} />
        </Routes>
      </MemoryRouter>
    </ApiErrorProvider>,
  );
  await waitFor(() =>
    expect(screen.getAllByText('Attempts').length).toBeGreaterThan(0),
  );
  expect(screen.queryByText('Reload')).toBeNull();
  expect(screen.queryByText('Cancel')).toBeNull();
  expect(fetchSpy2).not.toHaveBeenCalled();
});


