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
    expect(res.fields[0].hasBbox).toBe(true);
  });

  it('returns null when input path is missing', () => {
    const job = {} as JobDetailResponse;
    const res = parseOutputToViewModel(job, {}, {});
    expect(res).toBeNull();
  });

  it('normalizes pages and filters words not linked to fields', () => {
    const job = {
      paths: { input: { path: '/doc.pdf' } },
    } as unknown as JobDetailResponse;
    const output = {
      fields: [
        {
          key: 'name',
          value: 'Hi',
          spans: [{ page: 0, x: 0, y: 0, width: 0.5, height: 0.5 }],
        },
      ],
    };
    const md = {
      pages: [{ width: 100, height: 100 }],
      boxes: [
        { page: 0, xNorm: 0, yNorm: 0, widthNorm: 0.5, heightNorm: 0.5, text: 'Hi' },
        { page: 0, xNorm: 0.6, yNorm: 0.6, widthNorm: 0.3, heightNorm: 0.3, text: 'extra' },
      ],
    };
    const res = parseOutputToViewModel(job, output, md)!;
    expect(res.fields[0].page).toBe(1);
    expect(res.pages[0].words).toHaveLength(1);
  });
});
