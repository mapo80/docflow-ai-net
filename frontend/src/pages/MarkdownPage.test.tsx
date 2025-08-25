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
  const res = await convertFile(file, 'eng', 'tesseract');
  expect(res).toMatchObject({ markdown: 'ok' });
  expect((__request as any).mock.calls[0][1].query.language).toBe('eng');
  expect((__request as any).mock.calls[0][1].query.engine).toBe('tesseract');
  (__request as any).mockRejectedValueOnce(
    new ApiError({ method: 'POST', url: '/markdown' } as any, { url: '', status: 500, statusText: 'err', body: {} }, 'err')
  );
  await expect(convertFile(file, 'eng', 'tesseract')).rejects.toBeInstanceOf(ApiError);
});

test('renders markdown and json', async () => {
  (__request as any).mockResolvedValueOnce({ markdown: 'hello', pages: [], boxes: [] });
  render(<MarkdownPage />);
  const file = new File(['a'], 'a.png');
  const input = document.querySelector('input[type="file"]') as HTMLInputElement;
  fireEvent.change(input, { target: { files: [file] } });
  fireEvent.click(screen.getAllByRole('button', { name: /convert/i })[0]);
  await screen.findByText(/Execution time/);
  await screen.findByText('hello');
  fireEvent.click(screen.getByRole('tab', { name: 'JSON' }));
  await screen.findByText('markdown');
});

test('handles line breaks in markdown', async () => {
  (__request as any).mockResolvedValueOnce({ markdown: 'first\nsecond', pages: [], boxes: [] });
  render(<MarkdownPage />);
  const file = new File(['a'], 'a.png');
  const input = document.querySelector('input[type="file"]') as HTMLInputElement;
  fireEvent.change(input, { target: { files: [file] } });
  fireEvent.click(screen.getAllByRole('button', { name: /convert/i })[0]);
  await screen.findByText(/Execution time/);
  const el = document.querySelector('[data-color-mode="light"] p');
  expect(el?.innerHTML.replace(/\n/g, '')).toBe('first<br>second');
});
