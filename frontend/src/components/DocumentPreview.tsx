import { useEffect, useRef } from 'react';
import type { OcrWord } from '../adapters/extractionAdapter';
import { Select, Space, Button } from 'antd';
import { ZoomInOutlined, ZoomOutOutlined, ReloadOutlined } from '@ant-design/icons';

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
  const page = pages.find((p) => p.index === currentPage) || pages[0];

  useEffect(() => {
    if (!page || docType !== 'pdf') return;
    let cancelled = false;
    (async () => {
      const pdfjs = await import('pdfjs-dist');
      const worker = await import('pdfjs-dist/build/pdf.worker.min.mjs?url');
      pdfjs.GlobalWorkerOptions.workerSrc = worker.default;
      const pdf = await pdfjs.getDocument(srcUrl).promise;
      if (cancelled) return;
      const pdfPage = await pdf.getPage(currentPage);
      const viewport = pdfPage.getViewport({ scale: 1 });
      const canvas = canvasRef.current!;
      const ctx = canvas.getContext('2d')!;
      canvas.width = viewport.width;
      canvas.height = viewport.height;
      await pdfPage.render({ canvasContext: ctx, viewport }).promise;
    })();
    return () => {
      cancelled = true;
    };
  }, [docType, srcUrl, currentPage, page]);

  useEffect(() => {
    if (docType !== 'image') return;
    const img = imgRef.current;
    if (img && page) {
      img.width = page.width;
      img.height = page.height;
    }
  }, [docType, page]);

  if (!page) return null;

  const words = page.words;

  return (
    <div data-testid="doc-preview" style={{ width: '100%' }}>
      <div
        style={{
          display: 'flex',
          justifyContent: 'space-between',
          marginBottom: 8,
        }}
      >
        <Select
          value={currentPage}
          onChange={onPageChange}
          options={pages.map((p) => ({ value: p.index, label: `Page ${p.index}` }))}
          style={{ width: 120 }}
        />
        <Space>
          <Button
            icon={<ZoomInOutlined />}
            onClick={() => onZoomChange(Math.min(zoom * 1.25, 3))}
            aria-label="zoom in"
            data-testid="zoom-in"
            type="text"
          />
          <Button
            icon={<ZoomOutOutlined />}
            onClick={() => onZoomChange(Math.max(zoom / 1.25, 0.5))}
            aria-label="zoom out"
            data-testid="zoom-out"
            type="text"
          />
          <Button
            icon={<ReloadOutlined />}
            onClick={() => onZoomChange(1)}
            aria-label="reset zoom"
            data-testid="zoom-reset"
            type="text"
          />
        </Space>
      </div>
      <div
        style={{
          overflow: 'auto',
          position: 'relative',
          width: '100%',
          maxHeight: '80vh',
        }}
      >
        <div
          style={{
            transform: `scale(${zoom})`,
            transformOrigin: 'top left',
            width: page.width,
            height: page.height,
            position: 'relative',
          }}
        >
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
              style={{ display: 'block', width: page.width, height: page.height }}
            />
          )}
          <svg
            width={page.width}
            height={page.height}
            style={{ position: 'absolute', top: 0, left: 0 }}
          >
            {words.map((w) => (
              <rect
                key={w.id}
                data-testid={`bbox-${w.id}`}
                data-word-id={w.id}
                x={w.bbox.x}
                y={w.bbox.y}
                width={w.bbox.width}
                height={w.bbox.height}
                fill={selectedWordIds.has(w.id) ? 'rgba(0,123,255,0.3)' : 'transparent'}
                stroke={selectedWordIds.has(w.id) ? '#1890ff' : 'rgba(0,0,0,0.2)'}
                strokeWidth={selectedWordIds.has(w.id) ? 2 : 1}
                onClick={() => onWordClick(w.id)}
              />
            ))}
          </svg>
        </div>
      </div>
    </div>
  );
}
