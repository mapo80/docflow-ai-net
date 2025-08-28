import { render, waitFor, screen, cleanup, fireEvent, within } from '@testing-library/react';
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
  getByIdSpy.mockResolvedValue({
    id: '1',
    status: 'Succeeded',
    attempts: 1,
    model: 'm',
    templateToken: 't',
    language: 'eng',
    markdownSystem: 'ms',
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
    language: 'eng',
    markdownSystem: 'ms',
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
    language: 'eng',
    markdownSystem: 'ms',
    createdAt: '',
    updatedAt: '',
    paths: { input: { path: '/input.pdf', createdAt: 'x' } },
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
    language: 'eng',
    markdownSystem: 'ms',
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

test('lists markdown file while running', async () => {
  getByIdSpy.mockReset();
  getByIdSpy.mockResolvedValueOnce({
    id: '1',
    status: 'Running',
    attempts: 1,
    model: 'm',
    templateToken: 't',
    language: 'eng',
    markdownSystem: 'ms',
    createdAt: '',
    updatedAt: '',
    paths: { input: { path: '/i.pdf' }, markdown: { path: '/m.md' } },
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
  const filesTab = screen.getAllByRole('tab', { name: 'Files' })[0];
  fireEvent.click(filesTab);
  await waitFor(() => screen.getByText('markdown'));
  expect(fetchSpy).not.toHaveBeenCalled();
});

test('lists all paths without extra requests', async () => {
  getByIdSpy.mockReset();
  getByIdSpy.mockResolvedValueOnce({
    id: '1',
    status: 'Failed',
    attempts: 1,
    model: 'm',
    templateToken: 't',
    language: 'eng',
    createdAt: '',
    updatedAt: '',
    paths: { input: { path: '/input.pdf' }, error: { path: '/error.txt' }, prompt: { path: '/prompt.md', createdAt: '2024-01-01' } },
  } as any);
  const fetchSpy = vi.spyOn(global, 'fetch');
  render(
    <ApiErrorProvider>
      <MemoryRouter initialEntries={['/jobs/1']}>
        <Routes>
          <Route path="/jobs/:id" element={<JobDetail />} />
        </Routes>
      </MemoryRouter>
    </ApiErrorProvider>,
  );
  await waitFor(() => screen.getByText('Attempts'));
  const filesTab = screen.getAllByRole('tab', { name: 'Files' })[0];
  fireEvent.click(filesTab);
  await waitFor(() => screen.getByText('input'));
  expect(screen.getByText('error')).toBeInTheDocument();
  expect(screen.getByText('prompt')).toBeInTheDocument();
  expect(screen.getByText('2024-01-01')).toBeInTheDocument();
  expect(fetchSpy).not.toHaveBeenCalled();
});

test('previews markdown file', async () => {
  getByIdSpy.mockReset();
  getByIdSpy.mockResolvedValueOnce({
    id: '1',
    status: 'Succeeded',
    attempts: 1,
    model: 'm',
    templateToken: 't',
    language: 'eng',
    createdAt: '',
    updatedAt: '',
    paths: { input: { path: '/input.pdf' }, markdown: { path: '/markdown.md' }, prompt: { path: '/prompt.md', createdAt: '2024-01-01' } },
  } as any);
  const fetchSpy = vi.spyOn(global, 'fetch').mockResolvedValue({
    ok: true,
    headers: new Headers({ 'content-type': 'text/markdown' }),
    text: async () => '# hi',
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
  await waitFor(() => screen.getByText('Attempts'));
  const filesTab = screen.getAllByRole('tab', { name: 'Files' })[0];
  fireEvent.click(filesTab);
  await waitFor(() => screen.getByText('markdown'));
  const row = screen.getByText('markdown').closest('tr')!;
  const previewBtn = within(row).getByLabelText('Preview');
  fireEvent.click(previewBtn);
  await waitFor(() => screen.getByText('hi'));
  expect(fetchSpy).toHaveBeenCalledTimes(1);
});

test('opens document preview modal', async () => {
  getByIdSpy.mockReset();
  getByIdSpy.mockResolvedValueOnce({
    id: '1',
    status: 'Succeeded',
    attempts: 1,
    model: 'm',
    templateToken: 't',
    language: 'eng',
    createdAt: '',
    updatedAt: '',
    paths: {
      input: { path: '/file.png' },
      output: { path: '/out.json' },
      markdown: { path: '/m.md' },
    },
  } as any);
  vi.spyOn(global, 'fetch')
    .mockResolvedValueOnce({
      ok: true,
      json: async () => ({
        fields: [
          {
            key: 'name',
            value: 'hi',
            confidence: 0.9,
            spans: [{ page: 1, x: 0.1, y: 0.1, width: 0.1, height: 0.1 }],
          },
        ],
      }),
      headers: new Headers(),
    } as any)
    .mockResolvedValueOnce({
      ok: true,
      json: async () => ({
        pages: [{ number: 1, width: 100, height: 100 }],
        boxes: [
          {
            page: 1,
            xNorm: 0.1,
            yNorm: 0.1,
            widthNorm: 0.1,
            heightNorm: 0.1,
            text: 'hi',
          },
        ],
      }),
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

  await waitFor(() => screen.getByText('Attempts'));
  fireEvent.click(screen.getByTestId('open-preview'));
  await waitFor(() => expect(getByIdSpy).toHaveBeenCalledTimes(2));
});


