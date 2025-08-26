import { render, fireEvent, waitFor, screen } from '@testing-library/react';
import { test, vi, expect } from 'vitest';
import { MemoryRouter } from 'react-router-dom';
import JobsList from './JobsList';
import { JobsService } from '../generated';
import ApiErrorProvider from '../components/ApiErrorProvider';
import { notify } from '../components/notification';

const { mockNotify } = vi.hoisted(() => ({ mockNotify: vi.fn() }));
vi.mock('../components/notification', () => ({ notify: mockNotify, default: mockNotify }));

vi.mock('antd', () => ({
  Table: ({ dataSource, pagination, columns }: any) => (
    <div>
      {dataSource.map((row: any) => (
        <div key={row.id}>
          <span>{row.id}</span>
          <div>
            {columns[4].title}: {columns[4].render(row.language, row)}
          </div>
          {columns[8].render(null, row)}
        </div>
      ))}
      <button onClick={() => pagination.onChange(2, pagination.pageSize)}>2</button>
    </div>
  ),
  List: ({ dataSource, renderItem, pagination }: any) => (
    <div>
      {dataSource.map((row: any) => (
        <div key={row.id}>{renderItem(row)}</div>
      ))}
      <button onClick={() => pagination.onChange(2, pagination.pageSize)}>2</button>
    </div>
  ),
  Grid: { useBreakpoint: () => ({ md: true }) },
  Button: (props: any) => <button {...props} />,
  Badge: () => <div />,
  Alert: () => null,
  Space: ({ children }: any) => <div>{children}</div>,
}));

test('pagination and cancel', async () => {
  const getSpy = vi
    .spyOn(JobsService, 'jobsList')
    .mockResolvedValue({
      items: [
        { id: '1', status: 'Running', createdAt: '', updatedAt: '', templateToken: 't', model: 'm', language: 'eng', engine: 'tesseract' } as any,
      ],
      page: 1,
      pageSize: 10,
      total: 20,
    });
  const cancelSpy = vi.spyOn(JobsService, 'jobsDelete').mockResolvedValue(undefined as any);
  render(
    <ApiErrorProvider>
      <MemoryRouter>
        <JobsList />
      </MemoryRouter>
    </ApiErrorProvider>,
  );
  await waitFor(() => expect(getSpy).toHaveBeenCalledWith({ page: 1, pageSize: 10 }));
  await screen.findByText('OCR language: English');
  fireEvent.click(screen.getByText('2'));
  await waitFor(() => expect(getSpy).toHaveBeenLastCalledWith({ page: 2, pageSize: 10 }));
  fireEvent.click(screen.getByTitle('Cancel job'));
  await waitFor(() => expect(cancelSpy).toHaveBeenCalledWith({ id: '1' }));
  expect(notify).toHaveBeenCalledWith('success', 'Job canceled');
});

test('displays latin language', async () => {
  vi.restoreAllMocks();
  vi.spyOn(JobsService, 'jobsList').mockResolvedValue({
    items: [
      { id: '1', status: 'Running', createdAt: '', updatedAt: '', templateToken: 't', model: 'm', language: 'lat', engine: 'rapidocr' } as any,
    ],
    page: 1,
    pageSize: 10,
    total: 1,
  });
  render(
    <ApiErrorProvider>
      <MemoryRouter>
        <JobsList />
      </MemoryRouter>
    </ApiErrorProvider>,
  );
  await screen.findByText('OCR language: Latin');
});
