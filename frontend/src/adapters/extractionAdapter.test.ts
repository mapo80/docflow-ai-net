import { parseOutputToViewModel } from './extractionAdapter';
import type { JobDetailResponse } from '../generated';
import { describe, it, expect } from 'vitest';

describe('parseOutputToViewModel', () => {
  it('merges markdown boxes with field spans', () => {
    const job = {
      paths: { input: { path: '/doc.pdf' } },
    } as unknown as JobDetailResponse;
    const output = {
      fields: [
        {
          key: 'name',
          value: 'Hi',
          confidence: 0.9,
          spans: [{ page: 1, x: 0.1, y: 0.2, width: 0.3, height: 0.4 }],
        },
      ],
    };
    const md = {
      pages: [{ number: 1, width: 100, height: 200 }],
      boxes: [
        {
          page: 1,
          xNorm: 0.1,
          yNorm: 0.2,
          widthNorm: 0.3,
          heightNorm: 0.4,
          text: 'Hi',
        },
      ],
    };
    const res = parseOutputToViewModel(job, output, md)!;
    expect(res.pages[0].words[0].bbox.x).toBeCloseTo(10);
    expect(res.fields[0].wordIds).toEqual(['w0']);
  });

  it('returns null when input path is missing', () => {
    const job = {} as JobDetailResponse;
    const res = parseOutputToViewModel(job, {}, {});
    expect(res).toBeNull();
  });
});
