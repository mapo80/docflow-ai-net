import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import ModelsPage from './ModelsPage';
import { ModelsService } from '../generated';
const { mockNotify } = vi.hoisted(() => ({ mockNotify: vi.fn() }));
vi.mock('../components/notification', () => ({ notify: mockNotify, default: mockNotify }));

vi.mock('../components/ModelModal', () => ({
  default: ({ onSaved, open }: any) =>
    open ? (
      <button
        onClick={() => {
          mockNotify('success', 'Model created successfully.');
          onSaved(true);
        }}
      >
        modal
      </button>
    ) : null,
}));

vi.mock('../components/ModelModal', () => ({
  default: ({ onSaved, open }: any) =>
    open ? <button onClick={() => onSaved(true)}>modal</button> : null,
}));

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
      createdAt: '2024-01-01T00:00:00Z',
      updatedAt: '2024-01-02T00:00:00Z',
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
      createdAt: '2024-01-03T00:00:00Z',
      updatedAt: '2024-01-04T00:00:00Z',
    },
  ];
  return {
    ModelsService: {
      modelsList: vi.fn().mockResolvedValue(list),
      modelsStartDownload: vi.fn().mockResolvedValue({}),
      modelsDelete: vi.fn().mockResolvedValue({}),
      modelsUpdate: vi.fn().mockResolvedValue({}),
    },
  };
});

describe('ModelsPage', () => {
  it('filters by type', async () => {
    render(<ModelsPage />);
    await screen.findAllByText('host');
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

  it('shows actions for hosted model', async () => {
    render(<ModelsPage />);
    await screen.findAllByText('host');
    expect(screen.getByLabelText('Edit model')).toBeInTheDocument();
    expect(screen.getAllByLabelText('Delete model').length).toBeGreaterThan(0);
  });

  it('shows created and updated dates', async () => {
    render(<ModelsPage />);
    await screen.findAllByText('host');
    expect(screen.getAllByText('Created: 2024-01-01 00:00').length).toBeGreaterThan(0);
    expect(screen.getAllByText('Updated: 2024-01-04 00:00').length).toBeGreaterThan(0);
  });

  it('shows success badge after model creation', async () => {
    render(<ModelsPage />);
    await screen.findAllByText('host');
    fireEvent.click(screen.getAllByText('Create Model')[0]);
    fireEvent.click(screen.getByText('modal'));
    await waitFor(() =>
      expect(screen.getByText('Model created successfully.')).toBeInTheDocument(),
    );
  });
});
