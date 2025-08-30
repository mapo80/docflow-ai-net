import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
const breakpointMock = { md: true } as any;
vi.mock('antd', async (importOriginal) => {
  const actual = await importOriginal();
  return { ...actual, Grid: { ...actual.Grid, useBreakpoint: () => breakpointMock } };
});
import RulesTagsPage from './RulesTagsPage';
import { SuitesService, TagsService } from '../generated';

const suites = [{ id: 's1', name: 'suite1', color: 'red', description: 'd1' }];
const tags = [{ id: 't1', name: 'tag1', color: 'blue', description: 'd2' }];

const { mockNotify } = vi.hoisted(() => ({ mockNotify: vi.fn() }));
vi.mock('../components/notification', () => ({ notify: mockNotify, default: mockNotify }));

vi.mock('../generated', () => ({
  SuitesService: {
    getApiV1Suites: vi.fn(),
    postApiV1Suites: vi.fn(),
    deleteApiV1Suites: vi.fn(),
  },
  TagsService: {
    getApiV1Tags: vi.fn(),
    postApiV1Tags: vi.fn(),
    deleteApiV1Tags: vi.fn(),
  },
}));

beforeEach(() => {
  (SuitesService.getApiV1Suites as any).mockResolvedValue({ items: suites });
  (TagsService.getApiV1Tags as any).mockResolvedValue({ items: tags });
  (SuitesService.postApiV1Suites as any).mockResolvedValue({});
  (TagsService.postApiV1Tags as any).mockResolvedValue({});
  (SuitesService.deleteApiV1Suites as any).mockResolvedValue({});
  (TagsService.deleteApiV1Tags as any).mockResolvedValue({});
  mockNotify.mockReset();
});

describe('RulesTagsPage', () => {
  it('loads and refreshes suites and tags', async () => {
    render(<RulesTagsPage />);
    await screen.findAllByText('suite1');
    await screen.findAllByText('tag1');
    fireEvent.click(screen.getAllByText('Refresh')[0]);
    await waitFor(() => expect(SuitesService.getApiV1Suites).toHaveBeenCalledTimes(2));
    await waitFor(() => expect(TagsService.getApiV1Tags).toHaveBeenCalledTimes(2));
  });

  it('creates a suite', async () => {
    render(<RulesTagsPage />);
    await screen.findAllByText('suite1');
    fireEvent.change(screen.getAllByPlaceholderText('Suite name')[0], {
      target: { value: 's2' },
    });
    fireEvent.click(screen.getAllByText('Add Suite')[0]);
    await waitFor(() =>
      expect(SuitesService.postApiV1Suites).toHaveBeenCalledWith({
        requestBody: { name: 's2' },
      }),
    );
    await waitFor(() =>
      expect(mockNotify).toHaveBeenCalledWith('success', 'Suite created successfully.'),
    );
  });

  it('creates a tag', async () => {
    render(<RulesTagsPage />);
    await screen.findAllByText('tag1');
    fireEvent.change(screen.getAllByPlaceholderText('Tag name')[0], {
      target: { value: 't2' },
    });
    fireEvent.click(screen.getAllByText('Add Tag')[0]);
    await waitFor(() =>
      expect(TagsService.postApiV1Tags).toHaveBeenCalledWith({
        requestBody: { name: 't2' },
      }),
    );
    await waitFor(() =>
      expect(mockNotify).toHaveBeenCalledWith('success', 'Tag created successfully.'),
    );
  });

  it('handles tag creation error', async () => {
    (TagsService.postApiV1Tags as any).mockRejectedValueOnce(new Error('x'));
    render(<RulesTagsPage />);
    await screen.findAllByText('tag1');
    fireEvent.change(screen.getAllByPlaceholderText('Tag name')[0], {
      target: { value: 't2' },
    });
    fireEvent.click(screen.getAllByText('Add Tag')[0]);
    await waitFor(() =>
      expect(mockNotify).toHaveBeenCalledWith('error', 'Failed to create tag.'),
    );
  });

  it('deletes suite and tag', async () => {
    render(<RulesTagsPage />);
    await screen.findAllByText('suite1');
    const deletes = screen.getAllByText('Delete');
    fireEvent.click(deletes[0]);
    fireEvent.click(deletes[1]);
    await waitFor(() =>
      expect(SuitesService.deleteApiV1Suites).toHaveBeenCalledWith({ id: 's1' }),
    );
    await waitFor(() =>
      expect(TagsService.deleteApiV1Tags).toHaveBeenCalledWith({ id: 't1' }),
    );
  });

  it('notifies on load error', async () => {
    (SuitesService.getApiV1Suites as any).mockRejectedValueOnce(new Error('x'));
    render(<RulesTagsPage />);
    await waitFor(() =>
      expect(mockNotify).toHaveBeenCalledWith('error', 'Failed to load taxonomies.'),
    );
  });

  it('handles empty responses', async () => {
    (SuitesService.getApiV1Suites as any).mockResolvedValueOnce(undefined);
    (TagsService.getApiV1Tags as any).mockResolvedValueOnce(undefined);
    render(<RulesTagsPage />);
    await waitFor(() => expect(SuitesService.getApiV1Suites).toHaveBeenCalled());
    expect(screen.getAllByText('No data').length).toBeGreaterThanOrEqual(2);
  });

  it('renders lists on mobile', async () => {
    breakpointMock.md = false;
    render(<RulesTagsPage />);
    await screen.findAllByText('suite1');
    expect(document.querySelectorAll('.ant-list').length).toBe(2);
    breakpointMock.md = true;
  });
});
