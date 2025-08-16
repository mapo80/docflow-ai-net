import { test, expect } from '@playwright/test';

test.beforeEach(async ({ page }) => {
  await page.addInitScript(() =>
    localStorage.setItem('apiKey', 'dev-secret-key-change-me'),
  );
});

test('opens hangfire dashboard with api key', async ({ page, context }) => {
  await context.route(
    '**/hangfire?api_key=dev-secret-key-change-me',
    (route) =>
      route.fulfill({
        status: 200,
        contentType: 'text/html',
        body: '<html></html>',
      }),
  );
  await page.goto('/jobs');
  const [popup] = await Promise.all([
    page.waitForEvent('popup'),
    page.getByRole('menuitem', { name: 'Hangfire' }).click(),
  ]);
  await popup.waitForLoadState();
  expect(popup.url()).toBe(
    'http://localhost:5214/hangfire?api_key=dev-secret-key-change-me',
  );
  const html = await popup.content();
  expect(html).toContain('<html');
});
