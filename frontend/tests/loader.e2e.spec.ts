import { test, expect } from '@playwright/test';

test.beforeEach(async ({ page }) => {
  await page.addInitScript(() =>
    localStorage.setItem('apiKey', 'dev-secret-key-change-me')
  );
});

test('shows loader icon while job detail is loading', async ({ page }) => {
  await page.addInitScript(() => {
    const originalFetch = window.fetch;
    window.fetch = async (input, init) => {
      const url = typeof input === 'string' ? input : input.url;
      if (url.includes('/api/v1/jobs/1')) {
        await new Promise((r) => setTimeout(r, 1000));
        return new Response(
          JSON.stringify({
            id: '1',
            status: 'Succeeded',
            derivedStatus: 'Completed',
            progress: 100,
            attempts: 1,
            createdAt: '',
            updatedAt: '',
            paths: {},
          }),
          { headers: { 'Content-Type': 'application/json' } }
        );
      }
      return originalFetch(input, init);
    };
  });

  await page.goto('/jobs/1', { waitUntil: 'domcontentloaded' });
  await expect(page.getByTestId('loader')).toBeVisible();
  await expect(page.getByText('Job 1')).toBeVisible();
});

test('shows loader while job detail chunk is loading', async ({ page }) => {
  await page.route('**/assets/JobDetail*.js', async (route) => {
    await new Promise((r) => setTimeout(r, 1000));
    await route.continue();
  });
  await page.route('**/api/v1/jobs/1', (route) => {
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
  await expect(page.getByTestId('loader')).toBeVisible();
  await expect(page.getByText('Job 1')).toBeVisible();
});
