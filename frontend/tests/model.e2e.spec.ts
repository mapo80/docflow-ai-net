import { test, expect } from '@playwright/test';

test('switch and download model', async ({ page }) => {
  await page.addInitScript(() =>
    localStorage.setItem('apiKey', 'dev-secret-key-change-me')
  );

  await page.route('**/model/available', (route) => {
    if (route.request().method() === 'OPTIONS') {
      route.fulfill({ status: 200 });
      return;
    }
    route.fulfill({ status: 200, body: JSON.stringify(['m1.gguf']) });
  });

  await page.route('**/model/switch', (route) => {
    if (route.request().method() === 'OPTIONS') {
      route.fulfill({ status: 200 });
      return;
    }
    route.fulfill({ status: 200, body: '{}' });
  });

  await page.route('**/model/download', (route) => {
    if (route.request().method() === 'OPTIONS') {
      route.fulfill({ status: 200 });
      return;
    }
    route.fulfill({ status: 200, body: '{}' });
  });

  let statuses = [
    { completed: false, percentage: 10, message: 'downloading' },
    { completed: true, percentage: 100, message: 'done' },
  ];
  await page.route('**/model/status', (route) => {
    if (route.request().method() === 'OPTIONS') {
      route.fulfill({ status: 200 });
      return;
    }
    const s = statuses.shift() ?? { completed: true, percentage: 100 };
    route.fulfill({ status: 200, body: JSON.stringify(s) });
  });

  await page.route('**/model', (route) => {
    if (route.request().method() === 'OPTIONS') {
      route.fulfill({ status: 200 });
      return;
    }
    if (route.request().url().endsWith('/model')) {
      route.fulfill({ status: 200, body: JSON.stringify({ file: 'm1.gguf', contextSize: 1024 }) });
    } else {
      route.fallback();
    }
  });

  await page.goto('/model');
  await page.getByRole('combobox').click();
  await page.getByText('m1.gguf').click();
  await page.getByTestId('switch-btn').click();

  await page.getByLabel('HF token').fill('t');
  await page.getByLabel('HF repo').fill('r');
  await page.getByLabel('Model file').fill('m1.gguf');
  await page.getByTestId('submit-download').click();
  await expect(page.getByText('downloading')).toBeVisible();
});
