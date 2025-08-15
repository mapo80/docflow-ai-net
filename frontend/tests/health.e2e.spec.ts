import { test, expect } from '@playwright/test';

// Ensure the health badge uses a light grey color while loading
// before the API responds.
test('health badge shows light color when loading', async ({ page }) => {
  await page.route('**/health/ready', async (route) => {
    await new Promise((r) => setTimeout(r, 5000));
    await route.fulfill({ json: { status: 'healthy' } });
  });

  await page.goto('/');
  await page.evaluate(() =>
    localStorage.setItem('apiKey', 'dev-secret-key-change-me')
  );
  await page.reload();
  const icon = page.locator('[aria-label="health-loading"]');
  await expect(icon).toHaveCSS('color', 'rgb(217, 217, 217)');
});
