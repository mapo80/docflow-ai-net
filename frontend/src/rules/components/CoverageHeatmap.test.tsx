import { render, fireEvent } from '@testing-library/react';
import CoverageHeatmap, { cellColor } from './CoverageHeatmap';
import { describe, it, expect, vi } from 'vitest';

const testsData = [
  { id: '1', name: 'Test 1', expect: { fields: { a: 1 } } },
  { id: '2', name: 'Test 2', expect: { fields: { a: 1, b: 1 } } },
  { id: '3', name: 'Test 3', expect: { fields: { a: 1 } } },
];

const results: Record<string, { diff?: Array<{ field: string }> }> = {
  'Test 1': { diff: [] },
  'Test 2': { diff: [{ field: 'a' }] },
  // Test 3 omitted to simulate notrun
};

describe('CoverageHeatmap', () => {
  it('renders heatmap and calculates percentages', () => {
    const { getByText, getAllByText } = render(
      <CoverageHeatmap tests={testsData} results={results} onRunAll={async () => {}} />,
    );

    expect(getAllByText('100%').length).toBeGreaterThan(0);
    getByText('50%');
    getByText('0%');
    getByText('33%');
    getByText('Not asserted');
  });

  it('runs tests with concurrency and exports csv', async () => {
    const run = vi.fn().mockResolvedValue(undefined);
    const create = vi.fn(() => 'blob:url');
    const revoke = vi.fn();
    (global as any).URL.createObjectURL = create;
    (global as any).URL.revokeObjectURL = revoke;
    const origCreate = document.createElement;
    const elSpy = vi.spyOn(document, 'createElement').mockImplementation((tag: string) => {
      if (tag === 'a') {
        return { href: '', click: vi.fn(), download: '' } as any;
      }
      return origCreate.call(document, tag);
    });

    const { getAllByRole, getAllByText } = render(
      <CoverageHeatmap tests={testsData} results={results} onRunAll={run} />,
    );

    const exportBtns = getAllByText('Export Coverage CSV');
    fireEvent.click(exportBtns[exportBtns.length - 1]);
    expect(create).toHaveBeenCalled();

    const runBtns = getAllByRole('button', { name: 'Run for Heatmap' });
    fireEvent.click(runBtns[runBtns.length - 1]);
    expect(run).toHaveBeenCalledWith({ maxParallelism: 4 });

    const spinInputs = getAllByRole('spinbutton');
    const input = spinInputs[spinInputs.length - 1];
    fireEvent.change(input, { target: { value: '2' } });
    const runBtns2 = getAllByRole('button', { name: 'Run for Heatmap' });
    fireEvent.click(runBtns2[runBtns2.length - 1]);
    expect(run).toHaveBeenLastCalledWith({ maxParallelism: 2 });

    expect(revoke).toHaveBeenCalled();
    elSpy.mockRestore();
  });

  it('opens drill modal on cell click', () => {
    const { container, getByText } = render(
      <CoverageHeatmap tests={testsData} results={results} onRunAll={async () => {}} />,
    );
    const cell = container.querySelector('td div');
    expect(cell).toBeTruthy();
    fireEvent.click(cell!);
    getByText('Outcome:');
    getByText('pass');
  });

  it('maps status to colors', () => {
    expect(cellColor('pass')).toBe('var(--ant-color-success-bg)');
    expect(cellColor('fail')).toBe('var(--ant-color-error-bg)');
    expect(cellColor('notrun')).toBe('var(--ant-color-warning-bg)');
    expect(cellColor('na')).toBe('var(--ant-color-bg-container)');
  });
});
