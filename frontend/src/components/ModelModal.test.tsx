import { render, screen, fireEvent } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import ModelModal from './ModelModal';
import ApiErrorProvider from './ApiErrorProvider';

vi.mock('../generated', () => ({
  ModelsService: {
    modelsCreate: vi.fn().mockResolvedValue({}),
  },
  ApiError: class ApiError extends Error {},
}));

describe('ModelModal', () => {
  it('uses select for type', () => {
    render(
      <ApiErrorProvider>
        <ModelModal open onCancel={() => {}} onCreated={() => {}} existingNames={[]} />
      </ApiErrorProvider>,
    );
    const select = screen.getByLabelText('Type');
    expect(select).toHaveAttribute('role', 'combobox');
  });

  it('validates required fields', async () => {
    render(
      <ApiErrorProvider>
        <ModelModal open onCancel={() => {}} onCreated={() => {}} existingNames={[]} />
      </ApiErrorProvider>,
    );
    fireEvent.click(screen.getAllByRole('button', { name: 'Save' })[0]);
    expect(await screen.findByText('Name is required')).toBeInTheDocument();
  });
});

