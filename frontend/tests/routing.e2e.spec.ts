import { test, expect } from '@playwright/test';

test('menu routing', async ({ page }) => {
  await page.goto('/');
  await page.evaluate(() =>
    localStorage.setItem('apiKey', 'dev-secret-key-change-me')
  );
  await page.reload();
  await page.getByRole('menuitem', { name: 'Jobs' }).click();
  await expect(page).toHaveURL(/\/jobs$/);
  await page.getByRole('menuitem', { name: 'Nuovo Job' }).click();
  await expect(page).toHaveURL(/\/jobs\/new$/);
  await page.getByRole('menuitem', { name: 'Health' }).click();
  await expect(page).toHaveURL(/\/health$/);
});

test('hangfire opens new window', async ({ page, context }) => {
  await page.route('**/hangfire**', async (route) => {
    if (route.request().method() === 'OPTIONS') {
      await route.fulfill({ status: 200 });
      return;
    }
    await route.fulfill({ status: 200, body: '<html></html>' });
  });
  await page.goto('/');
  await page.evaluate(() =>
    localStorage.setItem('apiKey', 'dev-secret-key-change-me')
  );
  await page.reload();
  const [newPage] = await Promise.all([
    context.waitForEvent('page'),
    page.getByRole('menuitem', { name: 'Hangfire' }).click(),
  ]);
  await newPage.waitForLoadState();
  expect(newPage.url()).toContain('api_key=dev-secret-key-change-me');
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
  await expect(
    page.getByRole('banner').locator('.ant-badge-status-dot'),
  ).toHaveCSS('background-color', 'rgb(245, 34, 45)');
  const badge = page.getByRole('banner').locator('.ant-badge-status');
  await badge.hover();
  await expect(page.getByRole('tooltip')).toHaveText('disk_full');
});
