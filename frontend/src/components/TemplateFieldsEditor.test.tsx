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
      },
    ),
    Select: ({ value, onChange, options }: any) => (
      <select value={value} onChange={(e) => onChange(e.target.value)}>
        {options.map((o: any) => (
          <option key={o.value} value={o.value}>
            {o.value}
          </option>
        ))}
      </select>
    ),
    Button: ({ children, onClick }: any) => <button onClick={onClick}>{children}</button>,
    Space: ({ children }: any) => <div>{children}</div>,
    Grid: { useBreakpoint },
  };
});

afterEach(() => {
  cleanup();
  vi.clearAllMocks();
});

test('mode switch preserves data', () => {
  const onChange = vi.fn();
  render(<TemplateFieldsEditor value={[]} onChange={onChange} />);
  fireEvent.click(screen.getByText('Aggiungi campo'));
  fireEvent.change(screen.getByPlaceholderText('Nome'), { target: { value: 'id' } });
  fireEvent.click(screen.getByText('JSON'));
  const textarea = screen.getByRole('textbox');
  expect(textarea.value).toContain('"name": "id"');
  fireEvent.change(textarea, {
    target: { value: '[{"name":"foo","type":"number"}]' },
  });
  fireEvent.click(screen.getByText('Visual'));
  expect(screen.getByDisplayValue('foo')).toBeTruthy();
});

test('renders on mobile', () => {
  (useBreakpoint as any).mockReturnValueOnce({ md: false });
  render(<TemplateFieldsEditor value={[]} onChange={() => {}} />);
  expect(screen.getByText('Aggiungi campo')).toBeTruthy();
});
