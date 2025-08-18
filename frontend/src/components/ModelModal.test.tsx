import { render, screen, fireEvent } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import ModelModal from './ModelModal';

vi.mock('../generated', () => ({
  ModelsService: {
    modelsCreate: vi.fn().mockResolvedValue({}),
  },
}));

describe('ModelModal', () => {
  it('uses select for type', () => {
    render(
      <ModelModal open onCancel={() => {}} onCreated={() => {}} existingNames={[]} />,
    );
    const select = screen.getByLabelText('Type');
    expect(select).toHaveAttribute('role', 'combobox');
  });

  it('validates required fields', async () => {
    render(
      <ModelModal open onCancel={() => {}} onCreated={() => {}} existingNames={[]} />,
    );
    fireEvent.click(screen.getAllByRole('button', { name: 'Save' })[0]);
    expect(await screen.findByText('Name is required')).toBeInTheDocument();
  });
});

