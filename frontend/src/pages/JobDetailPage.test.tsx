import { render, screen, waitFor, fireEvent } from '@testing-library/react';
import { MemoryRouter, Routes, Route } from 'react-router-dom';
import { describe, it, expect, vi } from 'vitest';
import JobDetailPage from './JobDetailPage';
import { JobsService } from '../generated';
import ApiErrorProvider from '../components/ApiErrorProvider';

const jobMock = {
  id: '1',
  paths: {
    input: { path: '/doc.png' },
    output: { path: '/output.json' },
    markdown: { path: '/md.md' },
  },
  markdownSystem: 'ms',
} as any;

vi.spyOn(JobsService, 'jobsGetById').mockResolvedValue(jobMock);

const output = {
  fields: [
    {
      key: 'n',
      value: 'v',
      confidence: 0.9,
      spans: [{ page: 1, x: 0.1, y: 0.1, width: 0.2, height: 0.2 }],
    },
  ],
};
const md = {
  pages: [{ number: 1, width: 100, height: 100 }],
  boxes: [
    {
      page: 1,
      xNorm: 0.1,
      yNorm: 0.1,
      widthNorm: 0.2,
      heightNorm: 0.2,
      text: 'Hi',
    },
  ],
};

vi.spyOn(global, 'fetch')
  .mockResolvedValueOnce({
    ok: true,
    json: async () => output,
    headers: new Headers(),
  } as any)
  .mockResolvedValueOnce({
    ok: true,
    json: async () => md,
    headers: new Headers(),
  } as any);

describe('JobDetailPage', () => {
  it('selects field and bbox bidirectionally', async () => {
    render(
      <ApiErrorProvider>
        <MemoryRouter initialEntries={[{ pathname: '/jobs/1' }]}>
          <Routes>
            <Route path="/jobs/:id" element={<JobDetailPage />} />
          </Routes>
        </MemoryRouter>
      </ApiErrorProvider>,
    );
    await waitFor(() => screen.getByTestId('bbox-w0'));
    const row = screen.getByTestId('row-n');
    fireEvent.click(row);
    const rect = screen.getByTestId('bbox-w0');
    expect(rect.getAttribute('fill')).toContain('0,123,255');
    fireEvent.click(rect);
    expect(row.className).toContain('selected-row');
  });
});
