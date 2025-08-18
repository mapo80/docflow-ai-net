import { render, screen, waitFor } from '@testing-library/react';
import { describe, it, vi, expect } from 'vitest';
import ModelLogModal from './ModelLogModal';
import { ModelsService } from '../generated';

vi.mock('../generated');

describe('ModelLogModal', () => {
  it('loads and displays log text', async () => {
    (ModelsService.modelsDownloadLog as unknown as vi.Mock).mockResolvedValue('line1');
    render(<ModelLogModal open modelId="1" onClose={() => {}} />);
    await waitFor(() => expect(ModelsService.modelsDownloadLog).toHaveBeenCalled());
    expect(await screen.findByText('line1')).toBeInTheDocument();
  });
});
