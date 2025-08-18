import { test, expect } from '@playwright/test';

test('uses custom title and favicon', async ({ page }) => {
  await page.goto('/');
  await expect(page).toHaveTitle('Docflow AI');
  await expect(page.locator('head link[rel="icon"]')).toHaveAttribute('href', '/favicon.svg');
});

test.skip('shows seeded jobs after login', async ({ page }) => {
  await page.route('**/api/v1/jobs*', (route) =>
    route.fulfill({
      json: {
        items: [
          { id: '11111111-1111-1111-1111-111111111111', status: 'Succeeded', createdAt: '', updatedAt: '' },
          { id: '22222222-2222-2222-2222-222222222222', status: 'Succeeded', createdAt: '', updatedAt: '' },
        ],
        page: 1,
        pageSize: 50,
        total: 2,
      },
    })
  );
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
