import { test, expect } from '@playwright/test';

test('can add field on extract page', async ({ page }) => {
  await page.goto('/');
  await page.evaluate(() => localStorage.setItem('apiKey', 'test'));
  await page.reload();
  await page.getByRole('button', { name: 'Add Field' }).click();
  await expect(page.getByPlaceholder('Field name')).toBeVisible();
});
