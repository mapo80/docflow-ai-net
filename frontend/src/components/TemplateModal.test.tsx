import { render, fireEvent, screen, waitFor, cleanup } from '@testing-library/react';
import { test, expect, vi, afterEach } from 'vitest';
import TemplateModal from './TemplateModal';
import { TemplatesService } from '../generated';

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
};
});

vi.mock('@uiw/react-md-editor', () => ({ default: ({ value, onChange }: any) => (
  <textarea value={value} onChange={(e) => onChange && onChange(e.target.value)} />
) }));

vi.mock('./TemplateFieldsEditor', () => ({
  default: ({ onChange }: any) => (
    <div>
      <button onClick={() => onChange([{ key: 'k', value: '1' }])}>add</button>
    </div>
  ),
}));

vi.mock('dayjs', () => ({ default: (v: any) => ({ format: () => v }) }));

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
  expect(onClose).not.toHaveBeenCalled();
  await waitFor(() => expect(onClose).toHaveBeenCalledWith(true));
  expect(createSpy.mock.calls[0][0].requestBody.fieldsJson).toEqual({ k: 1 });
});

test('update flow', async () => {
  vi.spyOn(TemplatesService, 'templatesGet').mockResolvedValue({
    id: '1',
    name: 'a',
    token: 'a',
    fieldsJson: {},
  } as any);
  const updateSpy = vi.spyOn(TemplatesService, 'templatesUpdate').mockResolvedValue({} as any);
  const onClose = vi.fn();
  render(<TemplateModal open={true} templateId="1" onClose={onClose} />);
  await waitFor(() => screen.getByPlaceholderText('Template Name'));
  fireEvent.change(screen.getByPlaceholderText('Template Name'), { target: { value: 'B' } });
  fireEvent.change(screen.getByPlaceholderText('Template Token'), { target: { value: 'b' } });
  fireEvent.click(screen.getByText('Save'));
  await waitFor(() => expect(updateSpy).toHaveBeenCalled());
});
