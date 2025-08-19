import { render, screen, fireEvent } from '@testing-library/react';
import { expect, test, vi } from 'vitest';
import MarkdownPage, { convertFile } from './MarkdownPage';
import { ApiError } from '../generated';

vi.mock('../generated/core/request', () => ({
  request: vi.fn(),
}));

vi.mock('../components/ApiErrorProvider', () => ({
  useApiError: () => ({ showError: vi.fn() }),
}));

import { request as __request } from '../generated/core/request';

test('convertFile', async () => {
  const file = new File(['a'], 'a.png');
  (__request as any).mockResolvedValueOnce({ markdown: 'ok', pages: [], boxes: [] });
  const res = await convertFile(file);
  expect(res).toMatchObject({ markdown: 'ok' });
  (__request as any).mockRejectedValueOnce(
    new ApiError({ method: 'POST', url: '/markdown' } as any, { url: '', status: 500, statusText: 'err', body: {} }, 'err')
  );
  await expect(convertFile(file)).rejects.toBeInstanceOf(ApiError);
});

test('renders markdown and json', async () => {
  (__request as any).mockResolvedValueOnce({ markdown: 'hello', pages: [], boxes: [] });
  render(<MarkdownPage />);
  const file = new File(['a'], 'a.png');
  const input = document.querySelector('input[type="file"]') as HTMLInputElement;
  fireEvent.change(input, { target: { files: [file] } });
  fireEvent.click(screen.getByRole('button', { name: /convert/i }));
  await screen.findByText(/Execution time/);
  await screen.findByDisplayValue('hello');
  fireEvent.click(screen.getByRole('tab', { name: 'JSON' }));
  await screen.findByText('markdown');
});
