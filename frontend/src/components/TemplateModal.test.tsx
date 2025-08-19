import { render, fireEvent, screen, waitFor, cleanup } from '@testing-library/react';
import { test, expect, vi, afterEach } from 'vitest';
import TemplateModal from './TemplateModal';
import { TemplatesService } from '../generated';
import { notify } from './notification';

const { mockNotify } = vi.hoisted(() => ({ mockNotify: vi.fn() }));
vi.mock('./notification', () => ({ notify: mockNotify, default: mockNotify }));

var useBreakpoint: any;
vi.mock('antd', () => {
  useBreakpoint = vi.fn(() => ({ md: true }));
  return {
  Modal: ({ open, children, footer, title, onCancel }: any) =>
    open ? (
      <div>
        <div>{title}</div>
        <button onClick={onCancel}>x</button>
        <div>{children}</div>
        <div>{footer}</div>
      </div>
    ) : null,
  Input: ({ value, onChange, placeholder, disabled }: any) => (
    <input
      placeholder={placeholder}
      value={value}
      onChange={(e) => onChange && onChange({ target: { value: e.target.value } })}
      disabled={disabled}
    />
  ),
  Button: ({ children, onClick, disabled, 'aria-label': aria, loading }: any) => (
    <button onClick={onClick} disabled={disabled || loading} aria-label={aria}>
      {children}
    </button>
  ),
  Space: ({ children }: any) => <div>{children}</div>,
  Grid: { useBreakpoint },
  message: { success: vi.fn() },
};
});

vi.mock('@uiw/react-md-editor', () => ({ default: ({ value, onChange }: any) => (
  <textarea value={value} onChange={(e) => onChange && onChange(e.target.value)} />
) }));

vi.mock('./TemplateFieldsEditor', () => ({
  default: ({ onChange }: any) => (
    <div>
      <button onClick={() => onChange([{ name: 'k', type: 'string' }])}>add</button>
    </div>
  ),
}));


afterEach(() => {
  cleanup();
  vi.clearAllMocks();
});

test('auto-generate token', () => {
  render(<TemplateModal open={true} onClose={() => {}} />);
  const name = screen.getByPlaceholderText('Template Name');
  fireEvent.change(name, { target: { value: 'Hello World' } });
  fireEvent.click(screen.getByText('Auto-generate from name'));
  const token = screen.getByPlaceholderText('Template Token');
  expect((token as HTMLInputElement).value).toBe('hello-world');
});

test('mobile layout renders', () => {
  (useBreakpoint as any).mockReturnValueOnce({ md: false });
  render(<TemplateModal open={true} onClose={() => {}} />);
  expect(screen.getByPlaceholderText('Template Name')).toBeTruthy();
});

test('create flow', async () => {
  const createSpy = vi
    .spyOn(TemplatesService, 'templatesCreate')
    .mockImplementation(() => new Promise((resolve) => setTimeout(() => resolve({} as any), 10)) as any);
  const onClose = vi.fn();
  render(<TemplateModal open={true} onClose={onClose} />);
  fireEvent.change(screen.getByPlaceholderText('Template Name'), { target: { value: 'A' } });
  fireEvent.change(screen.getByPlaceholderText('Template Token'), { target: { value: 'a' } });
  fireEvent.click(screen.getByText('add'));
  fireEvent.click(screen.getByText('Save'));
  expect(createSpy).toHaveBeenCalled();
  await waitFor(() => expect(onClose).toHaveBeenCalledWith(true));
  expect(notify).toHaveBeenCalledWith('success', 'Template created successfully.');
    expect(createSpy.mock.calls[0][0].requestBody.fieldsJson).toEqual([
      { name: 'k', type: 'string' },
    ]);
});

test('update flow', async () => {
    vi.spyOn(TemplatesService, 'templatesGet').mockResolvedValue({
      id: '1',
      name: 'a',
      token: 'a',
      fieldsJson: [],
    } as any);
  const updateSpy = vi.spyOn(TemplatesService, 'templatesUpdate').mockResolvedValue({} as any);
  const onClose = vi.fn();
  render(<TemplateModal open={true} templateId="1" onClose={onClose} />);
  await waitFor(() => screen.getByPlaceholderText('Template Name'));
  fireEvent.change(screen.getByPlaceholderText('Template Name'), { target: { value: 'B' } });
  fireEvent.change(screen.getByPlaceholderText('Template Token'), { target: { value: 'b' } });
  fireEvent.click(screen.getByText('Save'));
  await waitFor(() => expect(updateSpy).toHaveBeenCalled());
  expect(notify).toHaveBeenCalledWith('success', 'Template updated');
});

test('does not show timestamps on edit', async () => {
    vi.spyOn(TemplatesService, 'templatesGet').mockResolvedValue({
      id: '1',
      name: 'a',
      token: 'a',
      fieldsJson: [],
      createdAt: '2024-01-01T00:00:00Z',
      updatedAt: '2024-01-02T00:00:00Z',
    } as any);
  render(<TemplateModal open={true} templateId="1" onClose={() => {}} />);
  await waitFor(() => screen.getByPlaceholderText('Template Name'));
  expect(screen.queryByPlaceholderText('Created At')).toBeNull();
  expect(screen.queryByPlaceholderText('Last Updated')).toBeNull();
});
