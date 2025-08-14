import { render, fireEvent, waitFor, screen } from '@testing-library/react';
import { test, vi, expect } from 'vitest';
import { MemoryRouter } from 'react-router-dom';
import JobsList from './JobsList';
import { DefaultService } from '../generated';

vi.mock('antd', () => ({
  Table: ({ dataSource, pagination, columns }: any) => (
    <div>
      {dataSource.map((row: any) => (
        <div key={row.id}>
          <span>{row.id}</span>
          {columns[6].render(null, row)}
        </div>
      ))}
      <button onClick={() => pagination.onChange(2, pagination.pageSize)}>2</button>
    </div>
  ),
  Button: (props: any) => <button {...props} />,
  Progress: () => <div />, 
  Badge: () => <div />, 
  Alert: () => null,
  Space: ({ children }: any) => <div>{children}</div>,
  message: { success: vi.fn(), error: vi.fn() },
}));

test('pagination and cancel', async () => {
  const getSpy = vi
    .spyOn(DefaultService, 'getJobs')
    .mockResolvedValue({ items: [{ id: '1', status: 'Running', createdAt: '', updatedAt: '' } as any], page: 1, pageSize: 10, total: 20 });
  const cancelSpy = vi.spyOn(DefaultService, 'cancelJob').mockResolvedValue(undefined as any);
  render(
    <MemoryRouter>
      <JobsList />
    </MemoryRouter>,
  );
  await waitFor(() => expect(getSpy).toHaveBeenCalledWith({ page: 1, pageSize: 10 }));
  fireEvent.click(screen.getByText('2'));
  await waitFor(() => expect(getSpy).toHaveBeenLastCalledWith({ page: 2, pageSize: 10 }));
  fireEvent.click(screen.getByText('Cancel'));
  await waitFor(() => expect(cancelSpy).toHaveBeenCalledWith({ id: '1' }));
});
