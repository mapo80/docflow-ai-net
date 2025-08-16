import { test, expect } from '@playwright/test';

test.beforeEach(async ({ page }) => {
  await page.addInitScript(() =>
    localStorage.setItem('apiKey', 'dev-secret-key-change-me')
  );
});

test('shows fields and file list without error file', async ({ page }) => {
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
        fields: [{ key: 'company_name', value: 'ACME' }],
        paths: {
          input: '/api/v1/jobs/1/files/input.pdf',
          output: '/api/v1/jobs/1/files/output.json',
          error: '/api/v1/jobs/1/files/error.txt',
        },
      },
    });
  });
  await page.route('**/files/output.json', (route) => route.fulfill({ json: [] }));

  await page.goto('/jobs/1');
  await expect(page.getByText('company_name')).toBeVisible();
});

