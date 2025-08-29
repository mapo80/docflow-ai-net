import { render, screen, fireEvent } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';

const { mockNotify } = vi.hoisted(() => ({ mockNotify: vi.fn() }));
vi.mock('./notification', () => ({ default: mockNotify, notify: mockNotify }));

vi.mock('../generated', () => ({
  MarkdownSystemsService: {
    markdownSystemsCreate: vi.fn().mockResolvedValue({}),
    markdownSystemsGet: vi.fn().mockResolvedValue({ id: '1', name: 's', provider: 'docling', endpoint: 'http://x', hasApiKey: true }),
    markdownSystemsUpdate: vi.fn().mockResolvedValue({}),
  },
  ApiError: class ApiError extends Error {},
}));

import MarkdownSystemModal from './MarkdownSystemModal';
import ApiErrorProvider from './ApiErrorProvider';

describe('MarkdownSystemModal', () => {
  it('uses select for provider', () => {
    render(
      <ApiErrorProvider>
        <MarkdownSystemModal open onCancel={() => {}} onSaved={() => {}} existingNames={[]} />
      </ApiErrorProvider>,
    );
    const select = screen.getByLabelText('Provider');
    expect(select).toHaveAttribute('role', 'combobox');
  });

  it('validates required fields', async () => {
    render(
      <ApiErrorProvider>
        <MarkdownSystemModal open onCancel={() => {}} onSaved={() => {}} existingNames={[]} />
      </ApiErrorProvider>,
    );
    fireEvent.click(screen.getAllByRole('button', { name: 'Save' })[0]);
    expect(await screen.findByText('Name is required')).toBeInTheDocument();
  });
});
