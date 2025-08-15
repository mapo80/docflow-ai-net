import { render, screen, fireEvent } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import ModelSwitchSelect from './ModelSwitchSelect';

describe('ModelSwitchSelect', () => {
  it('calls onSwitch with selected model and context', async () => {
    const fn = vi.fn().mockResolvedValue(undefined);
    render(<ModelSwitchSelect models={['a.gguf']} onSwitch={fn} initialFile="a.gguf" />);
    const spin = screen.getByRole('spinbutton');
    fireEvent.change(spin, { target: { value: 2048 } });
    fireEvent.click(screen.getByTestId('switch-btn'));
    await new Promise((r) => setTimeout(r, 0));
    expect(fn).toHaveBeenCalledWith('a.gguf', 2048);
  });
});
