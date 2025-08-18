import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import ModelsPage from './ModelsPage';
import { ModelsService } from '../generated';

vi.mock('../generated', () => {
  const list = [
    {
      id: '1',
      name: 'host',
      type: 'hosted-llm',
      provider: 'openai',
      baseUrl: 'https://x',
      hfRepo: null,
      modelFile: null,
      downloaded: null,
      downloadStatus: null,
      lastUsedAt: null,
      hasApiKey: true,
      hasHfToken: false,
    },
    {
      id: '2',
      name: 'local',
      type: 'local',
      provider: null,
      baseUrl: null,
      hfRepo: 'repo',
      modelFile: 'file',
      downloaded: false,
      downloadStatus: 'NotRequested',
      lastUsedAt: null,
      hasApiKey: false,
      hasHfToken: true,
    },
  ];
  return {
    ModelsService: {
      modelsList: vi.fn().mockResolvedValue(list),
      modelsStartDownload: vi.fn().mockResolvedValue({}),
    },
  };
});

describe('ModelsPage', () => {
  it('filters by type', async () => {
    render(<ModelsPage />);
    await screen.findByText('host');
    const select = screen.getByRole('combobox');
    fireEvent.mouseDown(select);
    fireEvent.click(screen.getByTitle('Local'));
    await waitFor(() => {
      expect(screen.queryByText('host')).toBeNull();
      expect(screen.getByText('local')).toBeInTheDocument();
    });
  });

  it('starts download for local model', async () => {
    render(<ModelsPage />);
    await screen.findByText('local');
    fireEvent.click(screen.getByLabelText('Start download'));
    await waitFor(() => {
      expect(ModelsService.modelsStartDownload).toHaveBeenCalledWith({ id: '2' });
    });
  });
});

