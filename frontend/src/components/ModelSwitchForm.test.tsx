import { render, screen, fireEvent } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import ModelSwitchForm from './ModelSwitchForm';

vi.mock('./presetStore', () => ({ savePreset: vi.fn() }));

describe('ModelSwitchForm', () => {
  it('validates and submits payload', async () => {
    const fn = vi.fn().mockResolvedValue(undefined);
    render(<ModelSwitchForm onSubmit={fn} />);
    fireEvent.change(screen.getByLabelText(/HF token/), { target: { value: 't' } });
    fireEvent.change(screen.getByLabelText(/HF repo/), { target: { value: 'r' } });
    fireEvent.change(screen.getByLabelText(/Model file/), { target: { value: 'f' } });
    fireEvent.change(screen.getByLabelText(/Context size/), { target: { value: 1024 } });
    fireEvent.click(screen.getByTestId('submit-switch'));
    await new Promise((r) => setTimeout(r, 0));
    expect(fn).toHaveBeenCalledWith({ hfKey: 't', modelRepo: 'r', modelFile: 'f', contextSize: 1024 });
  });
});
