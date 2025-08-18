import { render, fireEvent, screen, cleanup } from '@testing-library/react';
import { test, expect, vi, afterEach } from 'vitest';
import TemplateFieldsEditor from './TemplateFieldsEditor';

var useBreakpoint: any;
vi.mock('antd', () => {
  useBreakpoint = vi.fn(() => ({ md: true }));
  return {
  Tabs: ({ items, activeKey, onChange }: any) => (
    <div>
      {items.map((it: any) => (
        <div key={it.key}>
          <button onClick={() => onChange(it.key)}>{it.label}</button>
          {activeKey === it.key && <div>{it.children}</div>}
        </div>
      ))}
    </div>
  ),
  Input: Object.assign(
    ({ value, onChange, placeholder, ...rest }: any) => (
      <input
        placeholder={placeholder}
        value={value}
        onChange={(e) => onChange && onChange({ target: { value: e.target.value } })}
        {...rest}
      />
    ),
    {
      TextArea: ({ value, onChange }: any) => (
        <textarea
          value={value}
          onChange={(e) => onChange && onChange({ target: { value: e.target.value } })}
        />
      ),
    }
  ),
  Button: ({ children, onClick, 'aria-label': aria }: any) => (
    <button onClick={onClick} aria-label={aria}>
      {children}
    </button>
  ),
  Grid: { useBreakpoint },
};
});

vi.mock('@uiw/react-json-view', () => ({ default: ({ value }: any) => <pre>{JSON.stringify(value)}</pre> }));

afterEach(() => {
  cleanup();
  vi.clearAllMocks();
});

test('mode switch preserves data', () => {
  const onChange = vi.fn();
  render(<TemplateFieldsEditor value={[]} onChange={onChange} />);
  fireEvent.click(screen.getByText('Add field'));
  const inputs = screen.getAllByPlaceholderText('Key');
  fireEvent.change(inputs[0], { target: { value: 'a' } });
  const valInputs = screen.getAllByPlaceholderText('Value');
  fireEvent.change(valInputs[0], { target: { value: '1' } });
  fireEvent.click(screen.getByText('JSON'));
  const textarea = screen.getByRole('textbox');
  expect(textarea.value).toContain('"a": 1');
  fireEvent.change(textarea, { target: { value: '{"a":1,"b":2}' } });
  fireEvent.click(screen.getByText('Simple'));
  const keys = screen.getAllByPlaceholderText('Key').map((i) => (i as HTMLInputElement).value);
  expect(keys).toEqual(['a', 'b']);
});

test('renders on mobile', () => {
  (useBreakpoint as any).mockReturnValueOnce({ md: false });
  render(<TemplateFieldsEditor value={[]} onChange={() => {}} />);
  expect(screen.getByText('Add field')).toBeTruthy();
});
