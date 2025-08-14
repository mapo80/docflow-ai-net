import { render, screen } from '@testing-library/react';
import { afterEach, describe, it, vi } from 'vitest';
import HealthBadge from './HealthBadge';

describe('HealthBadge', () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it('shows green on ok', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue({ json: () => Promise.resolve({ status: 'ok', reasons: [] }) }));
    render(<HealthBadge />);
    await screen.findAllByText('Health');
  });

  it('shows red on unhealthy', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue({ json: () => Promise.resolve({ status: 'unhealthy', reasons: ['bad'] }) }));
    render(<HealthBadge />);
    await screen.findAllByText('Health');
  });

  it('shows orange on backpressure', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue({ json: () => Promise.resolve({ status: 'backpressure', reasons: ['busy'] }) }));
    render(<HealthBadge />);
    await screen.findAllByText('Health');
  });
});
