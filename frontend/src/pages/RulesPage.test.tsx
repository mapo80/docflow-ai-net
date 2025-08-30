import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import RulesPage from './RulesPage';
import { RulesService } from '../generated';

const rules = [{ id: 'r1', name: 'rule1', version: 1, enabled: true }];

const { mockNotify } = vi.hoisted(() => ({ mockNotify: vi.fn() }));
const { mockNav } = vi.hoisted(() => ({ mockNav: vi.fn() }));
vi.mock('../components/notification', () => ({ notify: mockNotify, default: mockNotify }));
vi.mock('react-router-dom', async (importOriginal) => {
  const actual = await importOriginal();
  return { ...actual, useNavigate: () => mockNav };
});
vi.mock('../generated', () => ({
  RulesService: {
    getApiV1Rules: vi.fn(),
    postApiV1RulesClone: vi.fn(),
  },
}));

beforeEach(() => {
  (RulesService.getApiV1Rules as any).mockResolvedValue(rules);
  (RulesService.postApiV1RulesClone as any).mockResolvedValue({ id: 'r2' });
  mockNotify.mockReset();
  mockNav.mockReset();
});

describe('RulesPage', () => {
  it('loads and searches rules', async () => {
    render(
      <MemoryRouter>
        <RulesPage />
      </MemoryRouter>,
    );
    await waitFor(() => expect(RulesService.getApiV1Rules).toHaveBeenCalled());
    await waitFor(() => expect(screen.getAllByText('Open').length).toBeGreaterThan(0));
    fireEvent.change(screen.getByPlaceholderText('search'), { target: { value: 'x' } });
    await waitFor(() => expect(RulesService.getApiV1Rules).toHaveBeenCalledTimes(2));
    await waitFor(() => expect(RulesService.getApiV1Rules).toHaveBeenLastCalledWith({ search: 'x' }));
  });

  it('clones a rule', async () => {
    render(
      <MemoryRouter>
        <RulesPage />
      </MemoryRouter>,
    );
    await waitFor(() => expect(RulesService.getApiV1Rules).toHaveBeenCalled());
    await waitFor(() => expect(screen.getAllByText('Clone + tests').length).toBeGreaterThan(0));
    fireEvent.click(screen.getAllByText('Clone + tests')[0]);
    await waitFor(() =>
      expect(RulesService.postApiV1RulesClone).toHaveBeenCalledWith({
        id: 'r1',
        requestBody: { name: 'rule1 (copy)', withTests: true },
      }),
    );
    await waitFor(() => expect(mockNotify).toHaveBeenCalledWith('success', 'Rule cloned successfully.'));
    await waitFor(() => expect(mockNav).toHaveBeenCalledWith('/rules/r2'));
  });

  it('handles clone error', async () => {
    (RulesService.postApiV1RulesClone as any).mockRejectedValueOnce(new Error('x'));
    render(
      <MemoryRouter>
        <RulesPage />
      </MemoryRouter>,
    );
    await waitFor(() => expect(RulesService.getApiV1Rules).toHaveBeenCalled());
    await waitFor(() => expect(screen.getAllByText('Clone + tests').length).toBeGreaterThan(0));
    fireEvent.click(screen.getAllByText('Clone + tests')[0]);
    await waitFor(() => expect(mockNotify).toHaveBeenCalledWith('error', 'Failed to clone rule.'));
  });

  it('handles load error', async () => {
    (RulesService.getApiV1Rules as any).mockRejectedValueOnce(new Error('x'));
    render(
      <MemoryRouter>
        <RulesPage />
      </MemoryRouter>,
    );
    await waitFor(() => expect(mockNotify).toHaveBeenCalledWith('error', 'Failed to load rules.'));
  });

  it('handles empty responses', async () => {
    (RulesService.getApiV1Rules as any).mockResolvedValueOnce(undefined);
    render(
      <MemoryRouter>
        <RulesPage />
      </MemoryRouter>,
    );
    await waitFor(() => expect(RulesService.getApiV1Rules).toHaveBeenCalled());
    expect(screen.getAllByText('No data').length).toBeGreaterThan(0);
  });

});
