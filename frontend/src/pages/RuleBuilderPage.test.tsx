import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import RuleBuilderPage from './RuleBuilderPage';
import { RuleBuilderService, PropertiesService, RulesService } from '../generated';

const { mockNotify } = vi.hoisted(() => ({ mockNotify: vi.fn() }));
const { mockNav } = vi.hoisted(() => ({ mockNav: vi.fn() }));

vi.mock('../components/notification', () => ({ notify: mockNotify, default: mockNotify }));
vi.mock('react-router-dom', async (importOriginal) => {
  const actual = await importOriginal();
  return { ...actual, useNavigate: () => mockNav };
});
vi.mock('../generated', () => ({
  RuleBuilderService: { postApiV1RulebuilderCompile: vi.fn() },
  PropertiesService: { postApiV1RulesPropertiesRunFromBlocks: vi.fn() },
  RulesService: { postApiV1Rules: vi.fn() },
}));

beforeEach(() => {
  (RuleBuilderService.postApiV1RulebuilderCompile as any).mockResolvedValue({ code: 'x' });
  (PropertiesService.postApiV1RulesPropertiesRunFromBlocks as any).mockResolvedValue({ result: 1 });
  (RulesService.postApiV1Rules as any).mockResolvedValue({ id: 'r1' });
  mockNotify.mockReset();
  mockNav.mockReset();
});

describe('RuleBuilderPage', () => {
  it('compiles blocks', async () => {
    render(
      <MemoryRouter>
        <RuleBuilderPage />
      </MemoryRouter>,
    );
    fireEvent.click(screen.getAllByText('Add Exists')[0]);
    fireEvent.click(screen.getAllByText('Compile')[0]);
    await waitFor(() => expect(RuleBuilderService.postApiV1RulebuilderCompile).toHaveBeenCalled());
    const arg = (RuleBuilderService.postApiV1RulebuilderCompile as any).mock.calls[0][0];
    expect(arg.requestBody.blocks[0].type).toBe('exists');
  });

  it('handles compile error', async () => {
    (RuleBuilderService.postApiV1RulebuilderCompile as any).mockRejectedValueOnce(new Error('x'));
    render(
      <MemoryRouter>
        <RuleBuilderPage />
      </MemoryRouter>,
    );
    fireEvent.click(screen.getAllByText('Compile')[0]);
    await waitFor(() => expect(mockNotify).toHaveBeenCalledWith('error', 'Failed to compile blocks.'));
  });

  it('runs property check', async () => {
    render(
      <MemoryRouter>
        <RuleBuilderPage />
      </MemoryRouter>,
    );
    fireEvent.click(screen.getAllByText('Add Exists')[0]);
    fireEvent.click(screen.getAllByText('Property Check')[0]);
    await waitFor(() => expect(PropertiesService.postApiV1RulesPropertiesRunFromBlocks).toHaveBeenCalled());
    await waitFor(() => expect(screen.getByText(/result/)).toBeInTheDocument());
  });

  it('handles property check error', async () => {
    (PropertiesService.postApiV1RulesPropertiesRunFromBlocks as any).mockRejectedValueOnce(new Error('x'));
    render(
      <MemoryRouter>
        <RuleBuilderPage />
      </MemoryRouter>,
    );
    fireEvent.click(screen.getAllByText('Property Check')[0]);
    await waitFor(() => expect(mockNotify).toHaveBeenCalledWith('error', 'Property check failed.'));
  });

  it('creates rule', async () => {
    render(
      <MemoryRouter>
        <RuleBuilderPage />
      </MemoryRouter>,
    );
    fireEvent.click(screen.getAllByText('Create Rule')[0]);
    await waitFor(() => expect(RulesService.postApiV1Rules).toHaveBeenCalled());
    await waitFor(() => expect(mockNotify).toHaveBeenCalledWith('success', 'Rule created successfully.'));
    await waitFor(() => expect(mockNav).toHaveBeenCalledWith('/rules/r1'));
  });

  it('handles create rule error', async () => {
    (RulesService.postApiV1Rules as any).mockRejectedValueOnce(new Error('x'));
    render(
      <MemoryRouter>
        <RuleBuilderPage />
      </MemoryRouter>,
    );
    fireEvent.click(screen.getAllByText('Create Rule')[0]);
    await waitFor(() => expect(mockNotify).toHaveBeenCalledWith('error', 'Failed to create rule.'));
  });
});

