import { render, fireEvent, waitFor, screen, cleanup } from '@testing-library/react';
import { test, vi, expect, afterEach } from 'vitest';
import { MemoryRouter } from 'react-router-dom';
import TemplatesList from './TemplatesList';
import { TemplatesService } from '../generated';
import ApiErrorProvider from '../components/ApiErrorProvider';
import { notify } from '../components/notification';

const { mockNotify } = vi.hoisted(() => ({ mockNotify: vi.fn() }));
vi.mock('../components/notification', () => ({ notify: mockNotify, default: mockNotify }));

vi.mock('../components/TemplateModal', () => ({
  default: ({ onClose }: any) => <button onClick={() => onClose(true)}>modal</button>,
}));
var useBreakpoint: any;
vi.mock('antd', () => {
  useBreakpoint = vi.fn(() => ({ md: true }));
  return {
    Table: ({ dataSource, pagination, columns }: any) => (
      <div data-testid="table">
        {dataSource.map((row: any) => (
          <div key={row.id}>
            <span>{row.name}</span>
            {columns[4].render(null, row)}
          </div>
        ))}
        <button onClick={() => pagination.onChange(2, pagination.pageSize)}>2</button>
      </div>
    ),
    List: ({ dataSource, renderItem, pagination }: any) => (
      <div data-testid="list">
        {dataSource.map((row: any) => (
          <div key={row.id}>{renderItem(row)}</div>
        ))}
        <button onClick={() => pagination.onChange(2, pagination.pageSize)}>2</button>
      </div>
    ),
    Grid: { useBreakpoint },
    Button: ({ icon, ...props }: any) => <button {...props} />,
    Input: ({ value, onChange, placeholder }: any) => (
      <input value={value} onChange={onChange} placeholder={placeholder} />
    ),
    Popconfirm: ({ onConfirm, children }: any) => (
      <span onClick={() => onConfirm()}>{children}</span>
    ),
    Typography: { Text: ({ children }: any) => <span>{children}</span> },
    Space: ({ children }: any) => <div>{children}</div>,
    Form: Object.assign(({ children }: any) => <div>{children}</div>, {
      Item: ({ children }: any) => <div>{children}</div>,
    }),
    Select: Object.assign(
      ({ value, onChange, children, 'data-testid': testid }: any) => (
        <select data-testid={testid} value={value} onChange={(e) => onChange(e.target.value)}>
          {children}
        </select>
      ),
      { Option: ({ value, children }: any) => <option value={value}>{children}</option> },
    ),
    message: { success: vi.fn() },
});

test('create edit delete flows', async () => {
  const listSpy = vi
    .spyOn(TemplatesService, 'templatesList')
    .mockResolvedValue({
      items: [
        { id: '1', name: 'A', token: 't1', createdAt: '', updatedAt: '' },
        { id: '2', name: 'B', token: 't2', createdAt: '', updatedAt: '' },
      ],
      total: 2,
      page: 1,
      pageSize: 10,
    });
  const delSpy = vi.spyOn(TemplatesService, 'templatesDelete').mockResolvedValue(undefined as any);
  render(
    <ApiErrorProvider>
      <MemoryRouter>
        <TemplatesList />
      </MemoryRouter>
    </ApiErrorProvider>,
  );
  await waitFor(() => expect(listSpy).toHaveBeenCalledWith({ q: '', page: 1, pageSize: 10, sort: 'createdAt desc' }));
  const names = screen.getAllByText(/^(A|B)$/).map((el) => el.textContent);
  expect(names).toEqual(['A', 'B']);
  fireEvent.change(screen.getByTestId('sort-select'), { target: { value: 'name asc' } });
  await waitFor(() =>
    expect(listSpy).toHaveBeenLastCalledWith({ q: '', page: 1, pageSize: 10, sort: 'name asc' }),
  );
  fireEvent.click(screen.getAllByLabelText('Edit')[0]);
  fireEvent.click(screen.getByText('modal'));
  await waitFor(() => expect(listSpy).toHaveBeenCalledTimes(3));
  fireEvent.click(screen.getAllByLabelText('Delete')[0]);
  await waitFor(() => expect(delSpy).toHaveBeenCalledWith({ id: '1' }));
  expect(notify).toHaveBeenCalledWith('success', 'Template deleted');
  await waitFor(() => expect(listSpy).toHaveBeenCalledTimes(4));
  fireEvent.click(screen.getByText('Create Template'));
  fireEvent.click(screen.getByText('modal'));
  await waitFor(() => expect(listSpy).toHaveBeenCalledTimes(5));
  await screen.findByText('Template created successfully.');
});

test('renders list on mobile', async () => {
  (useBreakpoint as any).mockReturnValue({ md: false });
  const listSpy = vi
    .spyOn(TemplatesService, 'templatesList')
    .mockResolvedValue({ items: [], total: 0, page: 1, pageSize: 10 });
  render(
    <ApiErrorProvider>
      <MemoryRouter>
        <TemplatesList />
      </MemoryRouter>
    </ApiErrorProvider>,
  );
  await waitFor(() => expect(listSpy).toHaveBeenCalled());
  expect(screen.queryByTestId('table')).toBeNull();
  expect(screen.getByTestId('list')).toBeTruthy();
});

afterEach(() => {
  cleanup();
  vi.clearAllMocks();
});
