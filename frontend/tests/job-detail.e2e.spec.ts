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
        paths: {
          input: '/api/v1/jobs/1/files/input.pdf',
          output: '/api/v1/jobs/1/files/output.json',
          error: '/api/v1/jobs/1/files/error.txt',
        },
      },
    });
  });
  await page.route('**/files/output.json', (route) =>
    route.fulfill({
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify([
        {
          FieldName: 'company_name',
          Value: 'ACME',
          Confidence: 0.9,
          Spans: [{ Page: 0, BBox: { X: 0, Y: 0, W: 1, H: 1 } }],
        },
      ]),
    })
  );

  await page.goto('/jobs/1');
  await expect(page.getByText('company_name')).toBeVisible();
  await page.getByRole('tab', { name: 'Files' }).click();
  await expect(page.getByRole('cell', { name: 'input' })).toBeVisible();
  await expect(page.getByRole('cell', { name: 'output' })).toBeVisible();
  await expect(page.getByRole('cell', { name: 'error' })).toHaveCount(0);
});

