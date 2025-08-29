import { render, fireEvent, cleanup, waitFor } from '@testing-library/react';
import DocumentPreview from './DocumentPreview';
import { describe, it, expect, vi, afterEach } from 'vitest';

vi.mock('pdfjs-dist', () => ({ getDocument: vi.fn(), GlobalWorkerOptions: {} }));
vi.mock('pdfjs-dist/build/pdf.worker.min.mjs?url', () => ({ default: 'worker.js' }));

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
  afterEach(() => {
    vi.clearAllMocks();
    cleanup();
  });
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
    const { getByTestId } = render(
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
    expect(getByTestId('preview-scroll')).toBeTruthy();
  });

  it('uses flex layout to keep controls anchored', () => {
    const { getByTestId } = render(
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
    const outer = getByTestId('doc-preview');
    expect(outer.style.display).toBe('flex');
  });

  it('scales document to container size', async () => {
    const { getByTestId } = render(
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
    const scroll = getByTestId('preview-scroll');
    Object.defineProperty(scroll, 'clientWidth', { value: 200, configurable: true });
    Object.defineProperty(scroll, 'clientHeight', { value: 400, configurable: true });
    window.dispatchEvent(new Event('resize'));
    await waitFor(() => {
      const inner = getByTestId('preview-inner');
      expect(inner.style.transform).toContain('scale(2)');
    });
  });

  it('fits width when requested', async () => {
    const { getByTestId } = render(
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
        fitWidth
      />,
    );
    const scroll = getByTestId('preview-scroll');
    Object.defineProperty(scroll, 'clientWidth', { value: 200, configurable: true });
    Object.defineProperty(scroll, 'clientHeight', { value: 50, configurable: true });
    window.dispatchEvent(new Event('resize'));
    await waitFor(() => {
      const inner = getByTestId('preview-inner');
      expect(inner.style.transform).toContain('scale(2)');
    });
  });

  it('hides horizontal scroll when document fits', async () => {
    const { getByTestId } = render(
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
    const scroll = getByTestId('preview-scroll');
    Object.defineProperty(scroll, 'clientWidth', { value: 50, configurable: true });
    Object.defineProperty(scroll, 'clientHeight', { value: 100, configurable: true });
    Object.defineProperty(scroll, 'scrollWidth', { value: 50, configurable: true });
    window.dispatchEvent(new Event('resize'));
    await waitFor(() => {
      expect(scroll.style.overflowX).toBe('auto');
      expect(scroll.scrollWidth).toBeLessThanOrEqual(scroll.clientWidth);
    });
  });

  it('shows horizontal scroll when zoomed in', async () => {
    const { getByTestId } = render(
      <DocumentPreview
        docType={sample.docType}
        srcUrl={sample.srcUrl}
        pages={sample.pages}
        currentPage={1}
        zoom={3}
        selectedWordIds={new Set()}
        onWordClick={() => {}}
        onPageChange={() => {}}
        onZoomChange={() => {}}
      />,
    );
    const scroll = getByTestId('preview-scroll');
    Object.defineProperty(scroll, 'clientWidth', { value: 50, configurable: true });
    Object.defineProperty(scroll, 'clientHeight', { value: 100, configurable: true });
    Object.defineProperty(scroll, 'scrollWidth', { value: 150, configurable: true });
    window.dispatchEvent(new Event('resize'));
    await waitFor(() => {
      expect(scroll.style.overflowX).toBe('auto');
      expect(scroll.scrollWidth).toBeGreaterThan(scroll.clientWidth);
    });
  });

  it('centers on selected word', async () => {
    const { getByTestId, rerender } = render(
      <DocumentPreview
        docType={sample.docType}
        srcUrl={sample.srcUrl}
        pages={sample.pages}
        currentPage={1}
        zoom={2}
        selectedWordIds={new Set()}
        onWordClick={() => {}}
        onPageChange={() => {}}
        onZoomChange={() => {}}
      />,
    );
    const scroll = getByTestId('preview-scroll');
    Object.defineProperty(scroll, 'clientWidth', { value: 100, configurable: true });
    Object.defineProperty(scroll, 'clientHeight', { value: 200, configurable: true });
    scroll.scrollTo = vi.fn();
    rerender(
      <DocumentPreview
        docType={sample.docType}
        srcUrl={sample.srcUrl}
        pages={sample.pages}
        currentPage={1}
        zoom={2}
        selectedWordIds={new Set(['w1'])}
        onWordClick={() => {}}
        onPageChange={() => {}}
        onZoomChange={() => {}}
      />,
    );
    await waitFor(() => expect(scroll.scrollTo).toHaveBeenCalled());
  });

  it('invokes zoom controls', () => {
    const onZoom = vi.fn();
    const { getByTestId } = render(
      <DocumentPreview
        docType={sample.docType}
        srcUrl={sample.srcUrl}
        pages={sample.pages}
        currentPage={1}
        zoom={1}
        selectedWordIds={new Set()}
        onWordClick={() => {}}
        onPageChange={() => {}}
        onZoomChange={onZoom}
      />,
    );
    fireEvent.click(getByTestId('zoom-in'));
    expect(onZoom).toHaveBeenCalled();
    fireEvent.click(getByTestId('zoom-out'));
    fireEvent.click(getByTestId('zoom-reset'));
    expect(onZoom).toHaveBeenCalledTimes(3);
  });

  it('renders PDF pages via pdf.js', async () => {
    const pdfjs: any = await import('pdfjs-dist');
    const getPage = vi.fn(async () => ({
      getViewport: () => ({ width: 100, height: 200 }),
      render: () => ({ promise: Promise.resolve() }),
    }));
    (pdfjs.getDocument as any).mockReturnValue({
      promise: Promise.resolve({ getPage }),
    });
    const orig = HTMLCanvasElement.prototype.getContext;
    HTMLCanvasElement.prototype.getContext = vi.fn(() => ({}));
    render(
      <DocumentPreview
        docType="pdf"
        srcUrl="file.pdf"
        pages={[{ index: 1, width: 100, height: 200, words: [] }]}
        currentPage={1}
        zoom={1}
        selectedWordIds={new Set()}
        onWordClick={() => {}}
        onPageChange={() => {}}
        onZoomChange={() => {}}
      />,
    );
    await waitFor(() => {
      expect(pdfjs.getDocument).toHaveBeenCalledWith('file.pdf');
      expect(getPage).toHaveBeenCalledWith(1);
    });
    HTMLCanvasElement.prototype.getContext = orig;
  });

  it('renders PDF at device pixel ratio', async () => {
    const pdfjs: any = await import('pdfjs-dist');
    const getPage = vi.fn(async () => ({
      getViewport: ({ scale }: any) => ({ width: 100 * scale, height: 200 * scale }),
      render: () => ({ promise: Promise.resolve() }),
    }));
    (pdfjs.getDocument as any).mockReturnValue({
      promise: Promise.resolve({ getPage }),
    });
    const origCtx = HTMLCanvasElement.prototype.getContext;
    HTMLCanvasElement.prototype.getContext = vi.fn(() => ({}));
    const origRatio = window.devicePixelRatio;
    Object.defineProperty(window, 'devicePixelRatio', { value: 2, configurable: true });
    const { getByTestId } = render(
      <DocumentPreview
        docType="pdf"
        srcUrl="file.pdf"
        pages={[{ index: 1, width: 100, height: 200, words: [] }]}
        currentPage={1}
        zoom={1}
        selectedWordIds={new Set()}
        onWordClick={() => {}}
        onPageChange={() => {}}
        onZoomChange={() => {}}
      />,
    );
    await waitFor(() => expect(pdfjs.getDocument).toHaveBeenCalled());
    const canvas = getByTestId('pdf-canvas') as HTMLCanvasElement;
    expect(canvas.width).toBe(200);
    expect(canvas.style.width).toBe('100px');
    Object.defineProperty(window, 'devicePixelRatio', { value: origRatio, configurable: true });
    HTMLCanvasElement.prototype.getContext = origCtx;
  });
});
