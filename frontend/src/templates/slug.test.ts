import { test, expect } from 'vitest';
import { slugify, isSlug } from './slug';

test('slug utilities', () => {
  expect(slugify('Hello World!')).toBe('hello-world');
  expect(isSlug('good_token')).toBe(true);
  expect(isSlug('bad token')).toBe(false);
});
