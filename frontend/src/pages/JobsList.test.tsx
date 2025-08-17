import { render, fireEvent, waitFor, screen } from '@testing-library/react';
import { test, vi, expect } from 'vitest';
import { MemoryRouter } from 'react-router-dom';
import JobsList from './JobsList';
import * as jobsApi from '@/services/jobsApi';

vi.mock('antd', () => {
  const React = require('react');
  return {
    Table: ({ dataSource, pagination, columns }: any) => (
      <div>
        {dataSource.map((row: any) => (
          <div key={row.id}>
            <span>{row.id}</span>
            {columns[columns.length - 1].render(null, row)}
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
    Progress: () => <div />,
    Badge: () => <div />,
    Alert: () => null,
    Space: ({ children }: any) => <div>{children}</div>,
    DatePicker: { RangePicker: () => <div /> },
    Typography: { Title: ({ children }: any) => <h1>{children}</h1>, Text: ({ children }: any) => <span>{children}</span> },
    Tag: ({ children }: any) => <span>{children}</span>,
    Input: () => <input />,
    Select: () => <select />,
    Tooltip: ({ title, children }: any) => React.cloneElement(children, { title }),
    Popconfirm: ({ children, onConfirm, title }: any) => React.cloneElement(children, { onClick: onConfirm, title }),
    Switch: () => <div />,
    message: { success: vi.fn(), error: vi.fn() },
  };
});

test('pagination and cancel', async () => {
  const getSpy = vi
    .spyOn(jobsApi, 'listJobs')
    .mockResolvedValue({ items: [{ id: '1', status: 'Running', createdAt: '', updatedAt: '' } as any], page: 1, pageSize: 10, total: 20 } as any);
  const cancelSpy = vi.spyOn(jobsApi, 'cancelJob').mockResolvedValue(true);
  render(
    <MemoryRouter>
      <JobsList />
    </MemoryRouter>,
  );
  await waitFor(() => expect(getSpy).toHaveBeenCalled());
  expect(getSpy.mock.calls[0][0]).toMatchObject({ page: 1, pageSize: 20 });
  fireEvent.click(screen.getByText('2'));
  await waitFor(() => expect(getSpy).toHaveBeenLastCalledWith(expect.objectContaining({ page: 2, pageSize: 20 })));
  fireEvent.click(screen.getByTitle('Cancel'));
  await waitFor(() => expect(cancelSpy).toHaveBeenCalledWith('1'));
});
