import { render, screen } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import RetryAfterBanner from './RetryAfterBanner';

vi.useFakeTimers();

describe('RetryAfterBanner', () => {
  it.skip('counts down and calls onFinish', async () => {
    const fn = vi.fn();
    render(<RetryAfterBanner seconds={2} onFinish={fn} />);
    expect(screen.getByText(/2s/)).toBeInTheDocument();
    vi.advanceTimersByTime(1000);
    await Promise.resolve();
    vi.advanceTimersByTime(1000);
    await Promise.resolve();
    expect(fn).toHaveBeenCalled();
  });
});
