import { test, expect } from 'vitest';
import {
  simpleToObject,
  objectToSimple,
  simpleToJsonText,
  jsonTextToSimple,
  slugify,
  isSlug,
} from './fieldsConversion';

test('slug utilities', () => {
  expect(slugify('Hello World!')).toBe('hello-world');
  expect(isSlug('good_token')).toBe(true);
  expect(isSlug('bad token')).toBe(false);
});

test('simple to object and back', () => {
  const simple = [
    { key: 'a', value: '1' },
    { key: 'a', value: '2' },
    { key: 'b', value: '{"c":3}' },
  ];
  const obj = simpleToObject(simple);
  expect(obj).toEqual({ a: [1, 2], b: { c: 3 } });
  const round = objectToSimple(obj);
  expect(round).toEqual([
    { key: 'a', value: '1' },
    { key: 'a', value: '2' },
    { key: 'b', value: '{"c":3}' },
  ]);
});

test('json text conversions', () => {
  const simple = [
    { key: 'x', value: 'true' },
    { key: 'y', value: 'null' },
  ];
  const json = simpleToJsonText(simple);
  expect(json).toBe('{\n  "x": true,\n  "y": null\n}');
  const back = jsonTextToSimple(json);
  expect(back).toEqual([
    { key: 'x', value: 'true' },
    { key: 'y', value: 'null' },
  ]);
});
