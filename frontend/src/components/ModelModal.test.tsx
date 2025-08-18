import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';

vi.mock('../generated', () => ({
  ModelsService: {
    modelsCreate: vi.fn().mockResolvedValue({}),
    modelsGet: vi.fn().mockResolvedValue({ id: '1', name: 'host', type: 'hosted-llm', provider: 'openai', baseUrl: 'https://x', hasApiKey: true }),
    modelsUpdate: vi.fn().mockResolvedValue({}),
  },
  ApiError: class ApiError extends Error {},
}));

import ModelModal from './ModelModal';
import ApiErrorProvider from './ApiErrorProvider';
import { ModelsService } from '../generated';

describe('ModelModal', () => {
  it('uses select for type', () => {
    render(
      <ApiErrorProvider>
        <ModelModal open onCancel={() => {}} onSaved={() => {}} existingNames={[]} />
      </ApiErrorProvider>,
    );
    const select = screen.getByLabelText('Type');
    expect(select).toHaveAttribute('role', 'combobox');
  });

  it('validates required fields', async () => {
    render(
      <ApiErrorProvider>
        <ModelModal open onCancel={() => {}} onSaved={() => {}} existingNames={[]} />
      </ApiErrorProvider>,
    );
    fireEvent.click(screen.getAllByRole('button', { name: 'Save' })[0]);
    expect(await screen.findByText('Name is required')).toBeInTheDocument();
  });

});

