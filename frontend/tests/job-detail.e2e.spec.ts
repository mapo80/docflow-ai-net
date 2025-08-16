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
          input: '/in.pdf',
          output: '/out.json',
          prompt: '/prompt.md',
        },
      },
    });
  });
  await page.route('**/out.json', (route) =>
    route.fulfill({ json: { ok: true } })
  );
  await page.route('**/prompt.md', (route) =>
    route.fulfill({ body: '# Title\ncontent' })
  );

  await page.goto('/jobs/1');
  await expect(page.getByText('Immediate')).toBeVisible();
  await expect(page.getByRole('button', { name: 'Open Hangfire' })).toHaveCount(0);
});

