import { test, expect } from '@playwright/test';

test.beforeEach(async ({ page }) => {
  await page.addInitScript(() =>
    localStorage.setItem('apiKey', 'dev-secret-key-change-me')
  );
});

test('shows loader icon while job detail is loading', async ({ page }) => {
  await page.route('**/jobs/1', async (route) => {
    await new Promise((r) => setTimeout(r, 1000));
    route.fulfill({
      json: {
        id: '1',
        status: 'Succeeded',
        derivedStatus: 'Completed',
        progress: 100,
        attempts: 1,
        createdAt: '',
        updatedAt: '',
        paths: {},
      },
    });
  });

  await page.goto('/jobs/1', { waitUntil: 'domcontentloaded' });
  await expect(page.getByLabel('loading')).toBeVisible();
  await expect(page.getByText('Job 1')).toBeVisible();
});

test('shows loader while job detail chunk is loading', async ({ page }) => {
  await page.route('**/JobDetail*.js', async (route) => {
    await new Promise((r) => setTimeout(r, 1000));
    await route.continue();
  });
  await page.route('**/jobs/1', (route) => {
    route.fulfill({
      json: {
        id: '1',
        status: 'Succeeded',
        derivedStatus: 'Completed',
        progress: 100,
        attempts: 1,
        createdAt: '',
        updatedAt: '',
        paths: {},
      },
    });
  });
  await page.goto('/jobs/1', { waitUntil: 'domcontentloaded' });
  await expect(page.getByLabel('loading')).toBeVisible();
  await expect(page.getByText('Job 1')).toBeVisible();
});
