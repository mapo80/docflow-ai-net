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
  },
} as any;

vi.spyOn(JobsService, 'jobsGetById').mockResolvedValue(jobMock);

const output = {
  pages: [
    {
      index: 1,
      width: 100,
      height: 100,
      words: [
        { id: 'w1', text: 'Hi', bbox: { x: 10, y: 10, width: 20, height: 20 } },
      ],
    },
  ],
  fields: [
    { id: 'f1', name: 'n', value: 'v', page: 1, wordIds: ['w1'], conf: 0.9 },
  ],
};

vi.spyOn(global, 'fetch').mockResolvedValue({
  ok: true,
  json: async () => output,
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
    await waitFor(() => screen.getByTestId('bbox-w1'));
    const row = screen.getByTestId('row-f1');
    fireEvent.click(row);
    const rect = screen.getByTestId('bbox-w1');
    expect(rect.getAttribute('fill')).toContain('0,123,255');
    fireEvent.click(rect);
    expect(row.className).toContain('selected-row');
  });
});
