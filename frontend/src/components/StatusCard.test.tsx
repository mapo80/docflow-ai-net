import { render, screen } from '@testing-library/react';
import { describe, it, vi, expect } from 'vitest';
import StatusCard from './StatusCard';
import { DefaultService } from '../generated';

vi.useFakeTimers();

describe('StatusCard', () => {
  it.skip('polls until completed', async () => {
    const spy = vi
      .spyOn(DefaultService, 'getModelStatus')
      .mockResolvedValueOnce({ completed: false, percentage: 0 })
      .mockResolvedValueOnce({ completed: true, percentage: 100 });
    render(<StatusCard active={true} />);
    await Promise.resolve();
    await vi.advanceTimersByTimeAsync(2000);
    await screen.findByText('Completato');
    expect(spy).toHaveBeenCalledTimes(2);
  });
});
