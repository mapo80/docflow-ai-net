import { render, screen, fireEvent } from '@testing-library/react';
import { expect, test, vi, beforeEach } from 'vitest';
import MarkdownPage, { convertFile } from './MarkdownPage';
import { ApiError } from '../generated';

vi.mock('../generated/core/request', () => ({
  request: vi.fn(),
}));

const showError = vi.fn();
vi.mock('../components/ApiErrorProvider', () => ({
  useApiError: () => ({ showError }),
}));

import { request as __request } from '../generated/core/request';

beforeEach(() => {
  (__request as any).mockReset();
  showError.mockReset();
});

test('convertFile', async () => {
  const file = new File(['a'], 'a.png');
  (__request as any).mockResolvedValueOnce({ markdown: 'ok', pages: [], boxes: [] });
  const res = await convertFile(file, 'eng', 'ms');
  expect(res).toMatchObject({ markdown: 'ok' });
  expect((__request as any).mock.calls[0][1].query.language).toBe('eng');
  expect((__request as any).mock.calls[0][1].query.markdownSystemId).toBe('ms');
  (__request as any).mockRejectedValueOnce(
    new ApiError({ method: 'POST', url: '/markdown' } as any, { url: '', status: 500, statusText: 'err', body: {} }, 'err')
  );
  await expect(convertFile(file, 'eng', 'ms')).rejects.toBeInstanceOf(ApiError);
});

test('requires markdown system selection', async () => {
  (__request as any).mockResolvedValueOnce([{ id: 'ms1', name: 'sys' }]);
  render(<MarkdownPage />);
  const file = new File(['a'], 'a.png');
  const input = document.querySelector('input[type="file"]') as HTMLInputElement;
  fireEvent.change(input, { target: { files: [file] } });
  await screen.findByText('a.png');
  fireEvent.click(screen.getAllByRole('button', { name: /convert/i })[0]);
  await Promise.resolve();
  expect(showError).toHaveBeenCalledWith('Markdown system is required');
});
