import { render, screen, fireEvent, waitFor, cleanup } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import RuleTestsPanel from './RuleTestsPanel';
import { RuleTestsService } from '../../generated/services/RuleTestsService';
import { message, Modal } from 'antd';

vi.mock('@monaco-editor/react', () => ({
  __esModule: true,
  default: ({ value, onChange }: any) => (
    <textarea data-testid="editor" value={value} onChange={(e) => onChange(e.target.value)} />
  ),
}));

const testsResponse = {
  items: [
    { id: '1', name: 'T1', expectJson: '{}', inputJson: '{}' },
    { id: '2', name: 'T2', expectJson: '{}', inputJson: '{}' },
  ],
};

describe('RuleTestsPanel', () => {
  beforeEach(() => {
    vi.spyOn(message, 'success').mockImplementation(() => undefined);
    vi.spyOn(message, 'error').mockImplementation(() => undefined);
    vi.spyOn(Modal, 'info').mockImplementation(() => undefined as any);
    vi.spyOn(RuleTestsService, 'getApiV1RulesTests').mockResolvedValue(testsResponse as any);
    vi.spyOn(RuleTestsService, 'postApiV1RulesTestsRun').mockResolvedValue([
      { name: 'T1', passed: true, actual: { a: 1 } },
      { name: 'T2', passed: false, error: 'boom', logs: ['oops'], actual: { b: 2 } },
    ] as any);
    vi.spyOn(RuleTestsService, 'postApiV1RulesTestsRunSelected')
      .mockResolvedValueOnce([{ name: 'T2', passed: true }])
      .mockResolvedValueOnce([{ name: 'T1', passed: true }]);
    vi.spyOn(RuleTestsService, 'postApiV1RulesTests').mockResolvedValue(undefined as any);
    vi.spyOn(RuleTestsService, 'getApiV1RulesTestsCoverage').mockResolvedValue([
      { field: 'f', tested: 1, mutated: 0, hits: 1, pass: 1 },
    ] as any);
    (window as any).matchMedia = (query: string) => ({
      matches: true,
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

  it('loads and runs tests', async () => {
    const { getByRole, getAllByRole, getByText, container } = render(<RuleTestsPanel ruleId="r1" />);
    await waitFor(() => expect(RuleTestsService.getApiV1RulesTests).toHaveBeenCalled());
    await screen.findByText('T1');

    fireEvent.click(getByRole('button', { name: /run selected/i }));
    expect(RuleTestsService.postApiV1RulesTestsRunSelected).not.toHaveBeenCalled();

    fireEvent.click(getByRole('button', { name: /run all/i }));
    await waitFor(() => expect(RuleTestsService.postApiV1RulesTestsRun).toHaveBeenCalled());
    getByText('passed');
    getByText('failed');

    const checkboxes = getAllByRole('checkbox');
    fireEvent.click(checkboxes[1]);
    fireEvent.click(getByRole('button', { name: /run selected/i }));
    await waitFor(() =>
      expect(RuleTestsService.postApiV1RulesTestsRunSelected).toHaveBeenLastCalledWith({
        ruleId: 'r1',
        requestBody: { ids: ['1'] },
      })
    );

    fireEvent.click(getByRole('button', { name: /compute coverage/i }));
    await waitFor(() => expect(RuleTestsService.getApiV1RulesTestsCoverage).toHaveBeenCalled());
    expect(Modal.info).toHaveBeenCalled();

    fireEvent.click(getByRole('button', { name: /heatmap/i }));
    const runBtns = await screen.findAllByRole('button', { name: 'Run for Heatmap' });
    fireEvent.click(runBtns[runBtns.length - 1]);
    await waitFor(() => expect(RuleTestsService.postApiV1RulesTestsRun).toHaveBeenCalledTimes(2));
  });

  it('adds tests and handles invalid json', async () => {
    render(<RuleTestsPanel ruleId="r1" />);
    await waitFor(() => expect(RuleTestsService.getApiV1RulesTests).toHaveBeenCalled());

    fireEvent.click(screen.getByRole('button', { name: /add test/i }));
    await waitFor(() => expect(RuleTestsService.postApiV1RulesTests).toHaveBeenCalled());
    expect(message.success).toHaveBeenCalledWith('Test added');

    fireEvent.change(screen.getByTestId('editor'), { target: { value: 'oops' } });
    fireEvent.click(screen.getByRole('button', { name: /add test/i }));
    await waitFor(() => expect(message.error).toHaveBeenCalled());
  });

  it('renders list on mobile', async () => {
    (window as any).matchMedia = () => ({
      matches: false,
      media: '',
      onchange: null,
      addListener: vi.fn(),
      removeListener: vi.fn(),
      addEventListener: vi.fn(),
      removeEventListener: vi.fn(),
      dispatchEvent: vi.fn(),
    });
    render(<RuleTestsPanel ruleId="r1" />);
    await waitFor(() => expect(RuleTestsService.getApiV1RulesTests).toHaveBeenCalled());
    expect(document.querySelector('.ant-list')).toBeTruthy();
  });
});
