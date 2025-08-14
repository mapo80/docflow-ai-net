import { render, screen } from '@testing-library/react';
import { afterEach, describe, it, vi } from 'vitest';
import HealthBadge from './HealthBadge';

vi.mock('../generated', () => ({
  DefaultService: {
    getHealth: vi.fn(),
  },
}));

const { DefaultService } = await import('../generated');

describe('HealthBadge', () => {
  afterEach(() => {
    vi.resetAllMocks();
  });

  it('shows green on ok', async () => {
    (DefaultService.getHealth as any).mockResolvedValue({ status: 'ok', reasons: [] });
    render(<HealthBadge />);
    await screen.findAllByText('Health');
  });

  it('shows red on unhealthy', async () => {
    (DefaultService.getHealth as any).mockResolvedValue({ status: 'unhealthy', reasons: ['bad'] });
    render(<HealthBadge />);
    await screen.findAllByText('Health');
  });

  it('shows orange on backpressure', async () => {
    (DefaultService.getHealth as any).mockResolvedValue({ status: 'backpressure', reasons: ['busy'] });
    render(<HealthBadge />);
    await screen.findAllByText('Health');
  });
});
