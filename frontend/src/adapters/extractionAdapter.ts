import type { JobDetailResponse } from '../generated';
import { OpenAPI } from '../generated';

export interface BBox { x: number; y: number; width: number; height: number }
export interface OcrWord { id: string; page: number; text: string; bbox: BBox; conf?: number }
export interface ExtractedField {
  id: string;
  name: string;
  value: string;
  page: number;
  wordIds: string[];
  conf?: number;
}
export interface ExtractionViewModel {
  docType: 'pdf' | 'image';
  srcUrl: string;
  pages: { index: number; width: number; height: number; words: OcrWord[] }[];
  fields: ExtractedField[];
}

function buildSrcUrl(path?: string | null): string {
  if (!path) return '';
  return path.startsWith('http') ? path : `${OpenAPI.BASE}${path}`;
}

export function parseOutputToViewModel(
  job: JobDetailResponse,
  outputJson: any,
): ExtractionViewModel | null {
  const srcUrl = buildSrcUrl(job.paths?.input?.path);
  if (!srcUrl) return null;
  const docType = srcUrl.toLowerCase().endsWith('.pdf') ? 'pdf' : 'image';
  const pagesJson = Array.isArray(outputJson?.pages) ? outputJson.pages : [];
  const fieldsJson = Array.isArray(outputJson?.fields) ? outputJson.fields : [];

  const pages = pagesJson.map((p: any, idx: number) => {
    const width = p.width ?? 0;
    const height = p.height ?? 0;
    const words = Array.isArray(p.words)
      ? p.words.map((w: any, wi: number) => {
          const id = w.id ?? `w${idx}_${wi}`;
          let { x, y, width: bw, height: bh } = w.bbox || {};
          x = Number(x) ?? 0;
          y = Number(y) ?? 0;
          bw = Number(bw) ?? 0;
          bh = Number(bh) ?? 0;
          if (bw <= 1 && bh <= 1) {
            x = x * width;
            y = y * height;
            bw = bw * width;
            bh = bh * height;
          }
          return {
            id,
            page: p.index ?? idx + 1,
            text: w.text ?? '',
            bbox: { x, y, width: bw, height: bh },
            conf: w.conf,
          } as OcrWord;
        })
      : [];
    return {
      index: p.index ?? idx + 1,
      width,
      height,
      words,
    };
  });

  const fields = fieldsJson.map((f: any, fi: number) => {
    const id = f.id ?? `f${fi}`;
    return {
      id,
      name: f.name ?? id,
      value: f.value ?? '',
      page: f.page ?? 1,
      wordIds: Array.isArray(f.wordIds) ? f.wordIds : [],
      conf: f.conf,
    } as ExtractedField;
  });

  return { docType, srcUrl, pages, fields };
}
