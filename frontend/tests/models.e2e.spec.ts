import { test, expect } from '@playwright/test';

test.beforeEach(async ({ page }) => {
  await page.addInitScript(() =>
    localStorage.setItem('apiKey', 'dev-secret-key-change-me')
  );
});

test('models page renders', async ({ page }) => {
  await page.goto('/models');
  await expect(page.getByText('GGUF Models')).toBeVisible();
});
