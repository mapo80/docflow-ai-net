import type { JobDetailResponse } from '../generated';
import { OpenAPI } from '../generated';

export interface BBox { x: number; y: number; width: number; height: number }
export interface OcrWord {
  id: string;
  page: number;
  text: string;
  bbox: BBox;
  conf?: number;
}
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
  mdJson: any,
): ExtractionViewModel | null {
  const srcUrl = buildSrcUrl(job.paths?.input?.path);
  if (!srcUrl) return null;
  const docType = srcUrl.toLowerCase().endsWith('.pdf') ? 'pdf' : 'image';
  const boxes = Array.isArray(mdJson?.boxes) ? mdJson.boxes : [];
  const pagesMeta = Array.isArray(mdJson?.pages) ? mdJson.pages : [];
  const pageDims = new Map<number, { width: number; height: number }>();
  pagesMeta.forEach((p: any, idx: number) => {
    const num = p.number ?? idx + 1;
    pageDims.set(num, { width: Number(p.width ?? 0), height: Number(p.height ?? 0) });
  });

  const wordsByPage = new Map<number, OcrWord[]>();
  boxes.forEach((b: any, idx: number) => {
    const page = b.page ?? 1;
    const dims = pageDims.get(page) || { width: 0, height: 0 };
    const x = Number(b.xNorm ?? b.x ?? 0) * dims.width;
    const y = Number(b.yNorm ?? b.y ?? 0) * dims.height;
    const bw = Number(b.widthNorm ?? b.width ?? 0) * dims.width;
    const bh = Number(b.heightNorm ?? b.height ?? 0) * dims.height;
    const word: OcrWord = {
      id: `w${idx}`,
      page,
      text: b.text ?? '',
      bbox: { x, y, width: bw, height: bh },
      conf: b.conf,
    };
    if (!wordsByPage.has(page)) wordsByPage.set(page, []);
    wordsByPage.get(page)!.push(word);
  });

  const pages = pagesMeta.map((p: any, idx: number) => {
    const num = p.number ?? idx + 1;
    return {
      index: num,
      width: Number(p.width ?? 0),
      height: Number(p.height ?? 0),
      words: wordsByPage.get(num) ?? [],
    };
  });

  const fieldsJson = Array.isArray(outputJson?.fields) ? outputJson.fields : [];

  function overlap(a: BBox, b: BBox): boolean {
    return (
      a.x < b.x + b.width &&
      a.x + a.width > b.x &&
      a.y < b.y + b.height &&
      a.y + a.height > b.y
    );
  }

  const fields = fieldsJson.map((f: any, fi: number) => {
    const id = f.key ?? f.id ?? `f${fi}`;
    const name = f.key ?? f.name ?? id;
    const spans = Array.isArray(f.spans) ? f.spans : [];
    const page = spans[0]?.page ?? 1;
    const wordIds: string[] = [];
    spans.forEach((s: any) => {
      const words = wordsByPage.get(s.page ?? page) ?? [];
      words.forEach((w) => {
        const dims = pageDims.get(w.page)!;
        const norm = {
          x: w.bbox.x / dims.width,
          y: w.bbox.y / dims.height,
          width: w.bbox.width / dims.width,
          height: w.bbox.height / dims.height,
        };
        if (
          overlap(norm, {
            x: Number(s.x ?? 0),
            y: Number(s.y ?? 0),
            width: Number(s.width ?? 0),
            height: Number(s.height ?? 0),
          })
        ) {
          wordIds.push(w.id);
        }
      });
    });
    return {
      id,
      name,
      value: f.value ?? '',
      page,
      wordIds,
      conf: f.confidence ?? f.conf,
    } as ExtractedField;
  });

  return { docType, srcUrl, pages, fields };
}
