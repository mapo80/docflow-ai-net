import { render, screen, fireEvent } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import ModelDownloadForm from './ModelDownloadForm';

describe('ModelDownloadForm', () => {
  it('validates and submits payload', async () => {
    const fn = vi.fn().mockResolvedValue(undefined);
    render(<ModelDownloadForm onSubmit={fn} />);
    fireEvent.change(screen.getByLabelText(/HF token/), { target: { value: 't' } });
    fireEvent.change(screen.getByLabelText(/HF repo/), { target: { value: 'r' } });
    fireEvent.change(screen.getByLabelText(/Model file/), { target: { value: 'f' } });
    fireEvent.click(screen.getByTestId('submit-download'));
    await new Promise((r) => setTimeout(r, 0));
    expect(fn).toHaveBeenCalledWith({ hfKey: 't', modelRepo: 'r', modelFile: 'f' });
  });
});
