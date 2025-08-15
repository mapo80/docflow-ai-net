import { render, screen } from '@testing-library/react';
import { afterEach, describe, it, vi } from 'vitest';
import HealthPage from './HealthPage';

describe('HealthPage', () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it('shows icons for ready and live statuses', async () => {
    vi.stubGlobal(
      'fetch',
      vi
        .fn()
        .mockResolvedValueOnce({ json: () => Promise.resolve({ status: 'ok', reasons: [] }) })
        .mockResolvedValueOnce({ json: () => Promise.resolve({ status: 'ok', reasons: [] }) })
        .mockResolvedValueOnce({ json: () => Promise.resolve({ status: 'unhealthy', reasons: [] }) })
    );
    render(<HealthPage />);
    await screen.findByLabelText('ready-ok');
    await screen.findByLabelText('live-unhealthy');
  });
});
