import { useEffect, useRef, useState } from 'react';
import type { OcrWord } from '../adapters/extractionAdapter';
import { Select, Slider, Space } from 'antd';

interface PageInfo {
  index: number;
  width: number;
  height: number;
  words: OcrWord[];
}

export interface DocumentPreviewProps {
  docType: 'pdf' | 'image';
  srcUrl: string;
  pages: PageInfo[];
  currentPage: number;
  zoom: number;
  selectedWordIds: Set<string>;
  onWordClick: (id: string) => void;
  onPageChange: (p: number) => void;
  onZoomChange: (z: number) => void;
}

export default function DocumentPreview({
  docType,
  srcUrl,
  pages,
  currentPage,
  zoom,
  selectedWordIds,
  onWordClick,
  onPageChange,
  onZoomChange,
}: DocumentPreviewProps) {
  const canvasRef = useRef<HTMLCanvasElement>(null);
  const imgRef = useRef<HTMLImageElement>(null);
  const [rendered, setRendered] = useState<{ width: number; height: number }>({
    width: 0,
    height: 0,
  });
  const page = pages.find((p) => p.index === currentPage) || pages[0];

  useEffect(() => {
    if (!page) return;
    let cancelled = false;
    if (docType === 'pdf') {
      (async () => {
        const pdfjs = await import('pdfjs-dist');
        const worker = await import('pdfjs-dist/build/pdf.worker.min.mjs?url');
        pdfjs.GlobalWorkerOptions.workerSrc = worker.default;
        const pdf = await pdfjs.getDocument(srcUrl).promise;
        if (cancelled) return;
        const pdfPage = await pdf.getPage(currentPage);
        const viewport = pdfPage.getViewport({ scale: zoom });
        const canvas = canvasRef.current!;
        const ctx = canvas.getContext('2d')!;
        canvas.width = viewport.width;
        canvas.height = viewport.height;
        await pdfPage.render({ canvasContext: ctx, viewport }).promise;
        if (!cancelled) {
          setRendered({ width: viewport.width, height: viewport.height });
        }
      })();
    } else if (docType === 'image') {
      const img = imgRef.current;
      if (img) {
        img.width = page.width * zoom;
        img.height = page.height * zoom;
        setRendered({ width: img.width, height: img.height });
      }
    }
    return () => {
      cancelled = true;
    };
  }, [docType, srcUrl, currentPage, zoom, page]);

  if (!page) return null;

  const sx = rendered.width / page.width;
  const sy = rendered.height / page.height;
  const words = page.words;

  return (
    <div data-testid="doc-preview" style={{ width: '100%', overflowX: 'auto' }}>
      <Space style={{ marginBottom: 8 }}>
        <Select
          value={currentPage}
          onChange={onPageChange}
          options={pages.map((p) => ({ value: p.index, label: `Page ${p.index}` }))}
          style={{ width: 120 }}
        />
        <Slider
          style={{ width: 150 }}
          min={0.5}
          max={3}
          step={0.1}
          value={zoom}
          onChange={onZoomChange}
        />
      </Space>
      <div style={{ position: 'relative', display: 'inline-block' }}>
        {docType === 'pdf' ? (
          <canvas
            data-testid="pdf-canvas"
            ref={canvasRef}
            style={{ display: 'block' }}
          />
        ) : (
          <img
            ref={imgRef}
            data-testid="img-preview"
            src={srcUrl}
            alt="document"
            style={{ display: 'block' }}
          />
        )}
        <svg
          width={rendered.width}
          height={rendered.height}
          style={{ position: 'absolute', top: 0, left: 0 }}
        >
          {words.map((w) => (
            <rect
              key={w.id}
              data-testid={`bbox-${w.id}`}
              data-word-id={w.id}
              x={w.bbox.x * sx}
              y={w.bbox.y * sy}
              width={w.bbox.width * sx}
              height={w.bbox.height * sy}
              fill={selectedWordIds.has(w.id) ? 'rgba(0,123,255,0.3)' : 'transparent'}
              stroke={selectedWordIds.has(w.id) ? '#1890ff' : 'rgba(0,0,0,0.2)'}
              strokeWidth={selectedWordIds.has(w.id) ? 2 : 1}
              onClick={() => onWordClick(w.id)}
            />
          ))}
        </svg>
      </div>
    </div>
  );
}
