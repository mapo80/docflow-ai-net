import { render, fireEvent, cleanup } from '@testing-library/react';
import DocumentPreview from './DocumentPreview';
import { describe, it, expect, vi, afterEach } from 'vitest';

const sample = {
  docType: 'image' as const,
  srcUrl: 'img.png',
  pages: [
    {
      index: 1,
      width: 100,
      height: 200,
      words: [
        { id: 'w1', page: 1, text: 'hi', bbox: { x: 10, y: 20, width: 30, height: 40 } },
        { id: 'w2', page: 1, text: 'there', bbox: { x: 50, y: 60, width: 20, height: 20 } },
      ],
    },
  ],
};

describe('DocumentPreview', () => {
  afterEach(() => cleanup());
  it('renders bounding boxes and handles click', () => {
    const onWordClick = vi.fn();
    const { getByTestId } = render(
      <DocumentPreview
        docType={sample.docType}
        srcUrl={sample.srcUrl}
        pages={sample.pages}
        currentPage={1}
        zoom={1}
        selectedWordIds={new Set(['w1'])}
        onWordClick={onWordClick}
        onPageChange={() => {}}
        onZoomChange={() => {}}
      />,
    );
    const rect = getByTestId('bbox-w1');
    expect(rect.getAttribute('x')).toBe('10');
    fireEvent.click(rect);
    expect(onWordClick).toHaveBeenCalledWith('w1');
  });

  it('wraps preview in scroll container', () => {
    const { container } = render(
      <DocumentPreview
        docType={sample.docType}
        srcUrl={sample.srcUrl}
        pages={sample.pages}
        currentPage={1}
        zoom={1}
        selectedWordIds={new Set()}
        onWordClick={() => {}}
        onPageChange={() => {}}
        onZoomChange={() => {}}
      />,
    );
    const wrapper = container.querySelector(
      '[data-testid="doc-preview"]',
    ) as HTMLElement;
    expect(wrapper.style.overflowX).toBe('auto');
  });

  // no filtering test since component always shows all words
});
