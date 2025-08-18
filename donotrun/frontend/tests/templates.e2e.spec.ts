import { test, expect } from '@playwright/test';

test.beforeEach(async ({ page }) => {
  await page.addInitScript(() =>
    localStorage.setItem('apiKey', 'dev-secret-key-change-me')
  );
});

test.skip('templates page renders', async ({ page }) => {
  await page.goto('/templates');
  await expect(page.getByText('Templates')).toBeVisible();
});
