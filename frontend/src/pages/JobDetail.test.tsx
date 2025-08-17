import { render, screen } from '@testing-library/react';
import { test, expect, vi } from 'vitest';
import { MemoryRouter, Routes, Route } from 'react-router-dom';
import JobDetail from './JobDetail';

test('renders job information', async () => {
  const fetchMock = vi.spyOn(global, 'fetch' as any).mockResolvedValue({
    ok: true,
    json: async () => ({
      id: '1',
      status: 'Succeeded',
      progress: 100,
      templateName: 'T',
      modelName: 'M',
      preview: { markdown: 'md', fields: [{ key: 'company_name', value: 'ACME' }] },
    }),
  } as any);

  render(
    <MemoryRouter initialEntries={['/jobs/1']}>
      <Routes>
        <Route path="/jobs/:id" element={<JobDetail />} />
      </Routes>
    </MemoryRouter>,
  );

  await screen.findByText('Job');
  await screen.getByRole('tab', { name: /Fields/ }).click();
  await screen.findByText(/company_name/);
  fetchMock.mockRestore();
});

