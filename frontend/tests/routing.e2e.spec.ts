import { test, expect } from '@playwright/test';

test('menu routing', async ({ page }) => {
  await page.goto('/');
  await page.evaluate(() =>
    localStorage.setItem('apiKey', 'dev-secret-key-change-me')
  );
  await page.reload();
  await page.getByRole('menuitem', { name: 'Jobs' }).click();
  await expect(page).toHaveURL(/\/jobs$/);
  await page.getByRole('menuitem', { name: 'New Job' }).click();
  await expect(page).toHaveURL(/\/jobs\/new$/);
  await page.getByRole('menuitem', { name: 'Health' }).click();
  await expect(page).toHaveURL(/\/health$/);
});


test('health badge shows state', async ({ page }) => {
  await page.route('**/health/ready', (route) => {
    route.fulfill({ json: { status: 'unhealthy', reasons: ['disk_full'] } });
  });
  await page.goto('/');
  await page.evaluate(() =>
    localStorage.setItem('apiKey', 'dev-secret-key-change-me')
  );
  await page.reload();
  await expect(page.getByLabel('health-unhealthy')).toBeVisible();
  const badge = page.getByRole('banner').locator('.ant-badge-status');
  await badge.hover();
  await expect(page.getByRole('tooltip')).toHaveText('disk_full');
});
