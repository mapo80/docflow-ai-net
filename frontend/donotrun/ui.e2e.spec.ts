import { test, expect } from '@playwright/test';

test('uses custom title and favicon', async ({ page }) => {
  await page.goto('/');
  await expect(page).toHaveTitle('Docflow AI');
  await expect(page.locator('head link[rel="icon"]')).toHaveAttribute('href', '/favicon.svg');
});

test('shows seeded jobs after login', async ({ page }) => {
  await page.goto('/');
  await page.getByLabel('API Key').fill('dev-secret-key-change-me');
  await page.getByRole('button', { name: 'Save' }).click();
  await page.waitForURL('**/jobs');
  await expect(
    page.getByRole('cell', { name: '11111111-1111-1111-1111-111111111111' }),
  ).toBeVisible();
  await expect(
    page.getByRole('cell', { name: '22222222-2222-2222-2222-222222222222' }),
  ).toBeVisible();
});
