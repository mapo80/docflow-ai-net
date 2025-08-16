import { test, expect } from '@playwright/test';

test('uses custom title and favicon', async ({ page }) => {
  await page.goto('/');
  await expect(page).toHaveTitle('Docflow AI');
  await expect(page.locator('head link[rel="icon"]')).toHaveAttribute('href', '/favicon.svg');
});
