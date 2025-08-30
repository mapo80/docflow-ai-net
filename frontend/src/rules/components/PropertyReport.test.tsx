import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import PropertyReport from './PropertyReport';
import { PropertiesService, SuitesService, TagsService } from '../../generated';
import { message } from 'antd';

vi.mock('../../generated', () => ({
  PropertiesService: {
    postApiV1RulesPropertiesRun: vi.fn(),
    postApiV1RulesPropertiesImportFailures: vi.fn(),
  },
  SuitesService: { getApiV1Suites: vi.fn() },
  TagsService: { getApiV1Tags: vi.fn() },
}));
vi.mock('antd', async (importOriginal) => {
  const actual = await importOriginal();
  return { ...actual, message: { success: vi.fn(), warning: vi.fn() } };
});

const runResult = {
  trials: 1,
  passed: 0,
  failed: 1,
  failures: [{ property: 'p', message: 'm', counterexample: { a: 1 } }],
};

beforeEach(() => {
  (SuitesService.getApiV1Suites as any).mockResolvedValue([{ name: 'suite1' }]);
  (TagsService.getApiV1Tags as any).mockResolvedValue([{ name: 'tag1' }]);
  (PropertiesService.postApiV1RulesPropertiesRun as any).mockResolvedValue(runResult);
  (PropertiesService.postApiV1RulesPropertiesImportFailures as any).mockResolvedValue({ imported: 1 });
  (message.success as any).mockReset();
  (message.warning as any).mockReset();
});

describe('PropertyReport', () => {
  it('runs properties and imports failures', async () => {
    render(<PropertyReport ruleId="r1" />);
    await waitFor(() => expect(SuitesService.getApiV1Suites).toHaveBeenCalled());
    fireEvent.click(screen.getByText('Run'));
    await waitFor(() => expect(PropertiesService.postApiV1RulesPropertiesRun).toHaveBeenCalled());
    await waitFor(() => expect(screen.getByText('p')).toBeInTheDocument());
    fireEvent.click(screen.getAllByRole('checkbox')[1]);
    fireEvent.click(screen.getByText('Select all'));
    fireEvent.click(screen.getByText('Import as tests'));
    await waitFor(() =>
      expect(PropertiesService.postApiV1RulesPropertiesImportFailures).toHaveBeenCalledWith({
        ruleId: 'r1',
        requestBody: {
          failures: runResult.failures,
          suite: 'property-fails',
          tags: ['property'],
        },
      }),
    );
    await waitFor(() => expect(message.warning).toHaveBeenCalled());
  });

  it('shows success when no failures', async () => {
    (PropertiesService.postApiV1RulesPropertiesRun as any).mockResolvedValueOnce({
      trials: 1,
      passed: 1,
      failed: 0,
      failures: [],
    });
    render(<PropertyReport ruleId="r1" />);
    fireEvent.click(screen.getAllByText('Run')[0]);
    await waitFor(() => expect(message.success).toHaveBeenCalledWith('All properties passed.'));
  });
});
