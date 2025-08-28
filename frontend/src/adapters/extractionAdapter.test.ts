import { parseOutputToViewModel } from './extractionAdapter';
import type { JobDetailResponse } from '../generated';
import { describe, it, expect } from 'vitest';

describe('parseOutputToViewModel', () => {
  it('parses fields and denormalizes coordinates', () => {
    const job = {
      paths: { input: { path: '/doc.pdf' } },
    } as unknown as JobDetailResponse;
    const output = {
      pages: [
        {
          index: 1,
          width: 100,
          height: 200,
          words: [
            {
              id: 'w1',
              text: 'Hi',
              bbox: { x: 0.1, y: 0.2, width: 0.3, height: 0.4 },
            },
          ],
        },
      ],
      fields: [
        {
          id: 'f1',
          name: 'name',
          value: 'Hi',
          page: 1,
          wordIds: ['w1'],
          conf: 0.9,
        },
      ],
    };
    const res = parseOutputToViewModel(job, output)!;
    expect(res.docType).toBe('pdf');
    expect(res.pages[0].words[0].bbox.x).toBeCloseTo(10);
    expect(res.fields[0].wordIds).toEqual(['w1']);
  });

  it('returns null when input path is missing', () => {
    const job = {} as JobDetailResponse;
    const res = parseOutputToViewModel(job, {});
    expect(res).toBeNull();
  });
});
