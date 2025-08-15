import { render, screen, fireEvent } from '@testing-library/react';
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

  it('shows loader while retrying', async () => {
    const fetchMock = vi
      .fn()
      .mockResolvedValueOnce({ json: () => Promise.resolve({ status: 'ok', reasons: [] }) })
      .mockResolvedValueOnce({ json: () => Promise.resolve({ status: 'ok', reasons: [] }) })
      .mockResolvedValueOnce({ json: () => Promise.resolve({ status: 'ok', reasons: [] }) })
      .mockImplementationOnce(() => new Promise(() => {}));
    vi.stubGlobal('fetch', fetchMock);
    render(<HealthPage />);
    await screen.findByLabelText('ready-ok');
    const [retry] = screen.getAllByRole('button', { name: /retry/i });
    fireEvent.click(retry);
    screen.getByLabelText('ready-loading');
  });
});
