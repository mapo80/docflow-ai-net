import { useEffect, useRef, useState } from 'react';
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
  fitWidth?: boolean;
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
  fitWidth = false,
}: DocumentPreviewProps) {
  const canvasRef = useRef<HTMLCanvasElement>(null);
  const imgRef = useRef<HTMLImageElement>(null);
  const wrapperRef = useRef<HTMLDivElement>(null);
  const [baseScale, setBaseScale] = useState(1);
  const [showScrollX, setShowScrollX] = useState(false);
  const page = pages.find((p) => p.index === currentPage) || pages[0];

  useEffect(() => {
    const updateScale = () => {
      if (!page || !wrapperRef.current) return;
      const w = wrapperRef.current.clientWidth;
      const h = wrapperRef.current.clientHeight;
      if (w > 0 && h > 0) {
        const scaleW = w / page.width;
        const scaleH = h / page.height;
        setBaseScale(fitWidth ? scaleW : Math.min(scaleW, scaleH));
      }
    };
    updateScale();
    window.addEventListener('resize', updateScale);
    return () => window.removeEventListener('resize', updateScale);
  }, [page, fitWidth]);

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
      const ratio = window.devicePixelRatio || 1;
      const rawViewport = pdfPage.getViewport({ scale: 1 });
      const viewport = pdfPage.getViewport({ scale: ratio });
      const canvas = canvasRef.current!;
      const ctx = canvas.getContext('2d')!;
      canvas.width = viewport.width;
      canvas.height = viewport.height;
      canvas.style.width = `${rawViewport.width}px`;
      canvas.style.height = `${rawViewport.height}px`;
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
  const scale = zoom * baseScale;

  useEffect(() => {
    const updateOverflow = () => {
      const wrapper = wrapperRef.current;
      if (!wrapper) return;
      const w = wrapper.clientWidth;
      if (w === 0) return;
      setShowScrollX(page.width * scale > w);
    };
    updateOverflow();
    window.addEventListener('resize', updateOverflow);
    return () => window.removeEventListener('resize', updateOverflow);
  }, [scale, page]);

  useEffect(() => {
    if (!wrapperRef.current || selectedWordIds.size === 0) return;
    const word = words.find((w) => selectedWordIds.has(w.id));
    if (!word) return;
    const s = scale;
    const rectX = word.bbox.x * s + word.bbox.width * s / 2;
    const rectY = word.bbox.y * s + word.bbox.height * s / 2;
    const left = rectX - wrapperRef.current.clientWidth / 2;
    const top = rectY - wrapperRef.current.clientHeight / 2;
    wrapperRef.current.scrollTo?.({
      left: Math.max(left, 0),
      top: Math.max(top, 0),
      behavior: 'smooth',
    });
  }, [selectedWordIds, scale, words]);

  return (
    <div
      data-testid="doc-preview"
      style={{
        width: '100%',
        height: '100%',
        display: 'flex',
        flexDirection: 'column',
      }}
    >
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
            onClick={() => {
              onZoomChange(1);
              wrapperRef.current?.scrollTo?.({ left: 0, top: 0 });
            }}
            aria-label="reset zoom"
            data-testid="zoom-reset"
            type="text"
          />
        </Space>
      </div>
      <div
        ref={wrapperRef}
        data-testid="preview-scroll"
        style={{
          overflowY: 'auto',
          overflowX: showScrollX ? 'auto' : 'hidden',
          position: 'relative',
          width: '100%',
          flex: 1,
          minHeight: 0,
        }}
      >
        <div
          data-testid="preview-inner"
          style={{
            transform: `scale(${scale})`,
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
