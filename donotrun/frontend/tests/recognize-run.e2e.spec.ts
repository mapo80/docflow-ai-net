import { test, expect } from '@playwright/test';

test.beforeEach(async ({ page }) => {
  await page.addInitScript(() =>
    localStorage.setItem('apiKey', 'dev-secret-key-change-me')
  );
});

test('recognize run page renders', async ({ page }) => {
  await page.goto('/recognize-run');
  await expect(page.getByText('Run Recognition with Template')).toBeVisible();
});
