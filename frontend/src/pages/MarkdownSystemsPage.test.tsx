import { render, screen, fireEvent } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';

vi.mock('../generated', () => ({
  MarkdownSystemsService: {
    markdownSystemsList: vi.fn().mockResolvedValue([]),
    markdownSystemsDelete: vi.fn().mockResolvedValue({}),
  },
}));

vi.mock('../components/MarkdownSystemModal', () => ({
  __esModule: true,
  default: () => <div data-testid="modal" />,
}));

import { MarkdownSystemsService } from '../generated';
import MarkdownSystemsPage from './MarkdownSystemsPage';

describe('MarkdownSystemsPage', () => {
  it('loads systems on mount', () => {
    render(<MarkdownSystemsPage />);
    expect(MarkdownSystemsService.markdownSystemsList).toHaveBeenCalled();
  });

  it('opens modal', () => {
    render(<MarkdownSystemsPage />);
    const btn = screen.getAllByRole('button', { name: 'Add system' })[0];
    fireEvent.click(btn);
    expect(screen.getByTestId('modal')).toBeInTheDocument();
  });
});
