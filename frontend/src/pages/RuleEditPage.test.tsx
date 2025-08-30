import { render, screen } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import RuleEditPage from './RuleEditPage';

vi.mock('../rules', () => ({
  RulesEditor: ({ ruleId }: { ruleId: string }) => <div>editor:{ruleId}</div>,
  RuleTestsPanel: ({ ruleId }: { ruleId: string }) => <div>panel:{ruleId}</div>,
  PropertyReport: ({ ruleId }: { ruleId: string }) => <div>report:{ruleId}</div>,
}));

describe('RuleEditPage', () => {
  it('renders subcomponents with rule id', () => {
    render(
      <MemoryRouter initialEntries={["/rules/123"]}>
        <Routes>
          <Route path="/rules/:id" element={<RuleEditPage />} />
        </Routes>
      </MemoryRouter>,
    );
    expect(screen.getByText('editor:123')).toBeInTheDocument();
    expect(screen.getByText('panel:123')).toBeInTheDocument();
    expect(screen.getByText('report:123')).toBeInTheDocument();
  });

  it('returns null when id missing', () => {
    const { container } = render(
      <MemoryRouter initialEntries={["/rules"]}>
        <Routes>
          <Route path="/rules" element={<RuleEditPage />} />
        </Routes>
      </MemoryRouter>,
    );
    expect(container.firstChild).toBeNull();
  });
});
