import { test, expect } from '@playwright/test';
import { promises as fs } from 'node:fs';
import path from 'node:path';

test.skip('switch and download model', async ({ page }) => {
  const modelsDir = path.resolve('models');
  const baseModel = 'm1.gguf';
  await fs.mkdir(modelsDir, { recursive: true });
  await fs.writeFile(path.join(modelsDir, baseModel), '');

  await page.goto('/');
  await page.evaluate(() =>
    localStorage.setItem('apiKey', 'dev-secret-key-change-me')
  );

  await page.route('**/model/available', async (route) => {
    if (route.request().method() === 'OPTIONS') {
      await route.fulfill({ status: 200 });
      return;
    }
    const files = await fs.readdir(modelsDir);
    await route.fulfill({ status: 200, body: JSON.stringify(files) });
  });

  await page.route('**/model/switch', async (route) => {
    if (route.request().method() === 'OPTIONS') {
      await route.fulfill({ status: 200 });
      return;
    }
    await route.fulfill({ status: 200, body: '{}' });
  });

  await page.route('**/model/download', async (route) => {
    if (route.request().method() === 'OPTIONS') {
      await route.fulfill({ status: 200 });
      return;
    }
    const body = route.request().postDataJSON() as { modelFile: string };
    await fs.copyFile(
      path.join(modelsDir, baseModel),
      path.join(modelsDir, body.modelFile)
    );
    await route.fulfill({ status: 200, body: '{}' });
  });

  let statuses = [
    { completed: false, percentage: 10, message: 'downloading' },
    { completed: true, percentage: 100, message: 'done' },
  ];
  await page.route('**/model/status', async (route) => {
    if (route.request().method() === 'OPTIONS') {
      await route.fulfill({ status: 200 });
      return;
    }
    const s = statuses.shift() ?? { completed: true, percentage: 100 };
    await route.fulfill({ status: 200, body: JSON.stringify(s) });
  });

  await page.route('**/api/v1/model', async (route) => {
    if (route.request().method() === 'OPTIONS') {
      await route.fulfill({ status: 200 });
      return;
    }
    if (route.request().url().endsWith('/model')) {
      await route.fulfill({
        status: 200,
        body: JSON.stringify({ file: baseModel, contextSize: 1024 }),
      });
    } else {
      route.fallback();
    }
  });

  const downloaded = 'm2.gguf';

  await page.goto('/model');
  await page.locator('input[type="password"]').fill('t');
  await page.locator('input[placeholder="TheOrg/the-model-repo"]').fill('r');
  await page.locator('input[placeholder="model.Q4_K_M.gguf"]').fill(downloaded);
  await page.getByTestId('submit-download').click();
  await expect(page.getByText('downloading')).toBeVisible();
  await expect(page.getByText('done')).toBeVisible();

  await page.getByTestId('reload-models').click();
  await page.waitForResponse((res) =>
    res.url().includes('/model/available') && res.request().method() === 'GET',
  );

  await page.getByTestId('model-select').click();
  await expect(page.getByText(downloaded)).toBeVisible();
});
