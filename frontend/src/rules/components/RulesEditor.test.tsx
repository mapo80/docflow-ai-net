import { render, screen, fireEvent, waitFor, cleanup } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import RulesEditor, { EditorSyncBadge, type RuleDetail } from './RulesEditor';
import { RulesService } from '../../generated/services/RulesService';
import * as lsp from '../lsp';
import { message } from 'antd';

vi.mock('@monaco-editor/react', () => ({
  __esModule: true,
  default: ({ value, onChange }: any) => (
    <textarea data-testid="editor" value={value} onChange={(e) => onChange(e.target.value)} />
  ),
}));

const rule: RuleDetail = {
  id: 'r1',
  name: 'Rule 1',
  version: '1',
  isBuiltin: false,
  enabled: true,
  code: 'initial',
  updatedAt: 'now',
};

describe('RulesEditor', () => {
  beforeEach(() => {
    vi.spyOn(message, 'success').mockImplementation(() => undefined);
    vi.spyOn(message, 'error').mockImplementation(() => undefined);
    vi.spyOn(lsp, 'syncWorkspace').mockResolvedValue(undefined);
    vi.spyOn(RulesService, 'getApiV1Rules1').mockResolvedValue(rule);
    vi.spyOn(RulesService, 'putApiV1Rules').mockResolvedValue(undefined as any);
    vi.spyOn(RulesService, 'postApiV1RulesCompile').mockResolvedValue({ ok: true, errors: [] });
    (window as any).matchMedia = (query: string) => ({
      matches: false,
      media: query,
      onchange: null,
      addListener: vi.fn(),
      removeListener: vi.fn(),
      addEventListener: vi.fn(),
      removeEventListener: vi.fn(),
      dispatchEvent: vi.fn(),
    });
  });
  afterEach(() => {
    vi.clearAllMocks();
    cleanup();
  });

  it('loads and displays rule', async () => {
    render(<RulesEditor ruleId="r1" />);
    expect(RulesService.getApiV1Rules1).toHaveBeenCalledWith({ id: 'r1' });
    await waitFor(() => expect(screen.getByTestId('editor')).toHaveValue('initial'));
    expect(screen.getByText('Rule 1')).toBeInTheDocument();
  });

  it('saves changes', async () => {
    render(<RulesEditor ruleId="r1" />);
    await waitFor(() => expect(screen.getByTestId('editor')).toHaveValue('initial'));
    fireEvent.change(screen.getByTestId('editor'), { target: { value: 'new code' } });
    await fireEvent.click(screen.getByRole('button', { name: /save/i }));
    await waitFor(() => expect(RulesService.putApiV1Rules).toHaveBeenCalled());
    expect(message.success).toHaveBeenCalledWith('Rule saved');
  });

  it('compiles and reports errors', async () => {
    (RulesService.postApiV1RulesCompile as any).mockResolvedValue({ ok: false, errors: ['oops'] });
    render(<RulesEditor ruleId="r1" />);
    await waitFor(() => expect(screen.getByTestId('editor')).toBeInTheDocument());
    await fireEvent.click(screen.getByRole('button', { name: /compile/i }));
    await waitFor(() => expect(message.error).toHaveBeenCalledWith('oops'));
  });

  it('syncs code after edit', async () => {
    render(<RulesEditor ruleId="r1" />);
    await waitFor(() => expect(screen.getByTestId('editor')).toHaveValue('initial'));
    fireEvent.change(screen.getByTestId('editor'), { target: { value: 'sync' } });
    await waitFor(() => expect(lsp.syncWorkspace).toHaveBeenCalledWith('r1', 'sync'));
  });
});

describe('EditorSyncBadge', () => {
  it('shows states', () => {
    const { rerender } = render(<EditorSyncBadge saving={true} synced={false} />);
    expect(screen.getByText('saving…')).toBeInTheDocument();
    rerender(<EditorSyncBadge saving={false} synced={true} />);
    expect(screen.getByText('synced')).toBeInTheDocument();
  });
});
