import { test, expect } from '@playwright/test';

test.beforeEach(async ({ page }) => {
  await page.addInitScript(() =>
    localStorage.setItem('apiKey', 'dev-secret-key-change-me')
  );
});

test('shows immediate flag and previews artifacts', async ({ page }) => {
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
        immediate: true,
        paths: {
          input: '/api/v1/jobs/1/files/input.pdf',
          output: '/api/v1/jobs/1/files/output.json',
          prompt: '/api/v1/jobs/1/files/prompt.md',
        },
      },
    });
  });
  let downloadRequested = false;
  await page.route('**/files/output.json', (route) => {
    downloadRequested = true;
    route.fulfill({
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ ok: true }),
    });
  });
  await page.route('**/files/prompt.md', (route) =>
    route.fulfill({ body: '# Title\ncontent' })
  );

  await page.goto('/jobs/1');
  await expect(page.getByText('Immediate')).toBeVisible();
  await expect(page.getByRole('button', { name: 'Open Hangfire' })).toHaveCount(0);
  await page.evaluate(() => fetch('/api/v1/jobs/1/files/output.json'));
  expect(downloadRequested).toBeTruthy();
});

