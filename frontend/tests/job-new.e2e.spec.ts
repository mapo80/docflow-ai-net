import { test, expect } from '@playwright/test';
import path from 'path';
import fs from 'fs';

const uploadFile = path.join(process.cwd(), 'tmp.pdf');
fs.writeFileSync(uploadFile, 'hello');

test.beforeEach(async ({ page }) => {
  await page.addInitScript(() =>
    localStorage.setItem('apiKey', 'dev-secret-key-change-me')
  );
});

async function fillBasic(page) {
  await page.getByRole('menuitem', { name: 'New Job' }).click();
  await page.setInputFiles('input[type="file"]', uploadFile);
  await page.getByRole('textbox').first().fill('hi');
}

test.skip('immediate success', async ({ page }) => {
  await page.route('**/jobs?mode=immediate', (route) => {
    if (route.request().method() === 'OPTIONS') {
      route.fulfill({ status: 200 });
      return;
    }
    route.fulfill({
      status: 200,
      body: JSON.stringify({ id: '1', status: 'Succeeded', output: { ok: true } }),
    });
  });
  await page.goto('/');
  await page.evaluate(() =>
    localStorage.setItem('apiKey', 'dev-secret-key-change-me')
  );
  await page.reload();
  await fillBasic(page);
  await page.getByRole('checkbox', { name: /Run immediately/ }).check();
  await page.getByRole('button', { name: 'Submit' }).click();
  await expect(page.getByText('Status: Succeeded')).toBeVisible();
  await page.getByRole('button', { name: 'Go to detail' }).click();
  await expect(page).toHaveURL(/\/jobs\/1$/);
});

test.skip('immediate capacity 429', async ({ page }) => {
  await page.route('**/jobs?mode=immediate', (route) => {
    if (route.request().method() === 'OPTIONS') {
      route.fulfill({ status: 200 });
      return;
    }
    route.fulfill({
      status: 429,
      headers: { 'Retry-After': '2' },
      body: JSON.stringify({ errorCode: 'immediate_capacity' }),
    });
  });
  await page.goto('/');
  await fillBasic(page);
  await page.getByRole('checkbox', { name: /Run immediately/ }).check();
  await page.getByRole('button', { name: 'Submit' }).click();
  await expect(page.getByText(/Retry in/)).toBeVisible();
});

test.skip('queued job completes and shows detail', async ({ page }) => {
  await page.route('**/jobs', (route) => {
    if (route.request().method() === 'OPTIONS') {
      route.fulfill({ status: 200 });
      return;
    }
    if (route.request().method() !== 'POST') {
      route.fallback();
      return;
    }
    route.fulfill({
      status: 202,
      body: JSON.stringify({ id: '42' }),
    });
  });

  await page.route('**/jobs/42', (route) => {
    if (route.request().method() === 'OPTIONS') {
      route.fulfill({ status: 200 });
      return;
    }
    route.fulfill({
      status: 200,
      headers: { 'Access-Control-Allow-Origin': '*', 'Content-Type': 'application/json' },
      body: JSON.stringify({
        id: '42',
        status: 'Succeeded',
        attempts: 1,
        createdAt: '',
        updatedAt: '',
        paths: {
          output: '/api/v1/jobs/42/files/output.json',
          fields: '/api/v1/jobs/42/files/fields.json',
        },
      }),
    });
  });

  await page.route('**/jobs/42/files/output.json', (route) => {
    route.fulfill({
      status: 200,
      headers: { 'Access-Control-Allow-Origin': '*', 'Content-Type': 'application/json' },
      body: JSON.stringify({ ok: true }),
    });
  });
  await page.route('**/jobs/42/files/fields.json', (route) => {
    route.fulfill({
      status: 200,
      headers: { 'Access-Control-Allow-Origin': '*', 'Content-Type': 'application/json' },
      body: JSON.stringify({ fields: [{ name: 'a', value: '1' }] }),
    });
  });

  await page.goto('/jobs/new');
  await page.setInputFiles('input[type="file"]', uploadFile);
  await page.getByRole('button', { name: 'Submit' }).click();
  await expect(page).toHaveURL(/\/jobs\/42$/);

  await page.locator('text=output').first().waitFor();
  await expect(page.locator('text=output')).toBeVisible();
  await expect(page.locator('text=fields')).toBeVisible();
});

test.skip('413 file too large', async ({ page }) => {
  await page.route('**/jobs', (route) => {
    if (route.request().method() === 'OPTIONS') {
      route.fulfill({ status: 200 });
      return;
    }
    route.fulfill({ status: 413, body: JSON.stringify({ errorCode: 'payload_too_large' }) });
  });
  await page.goto('/');
  await fillBasic(page);
  await page.getByRole('button', { name: 'Submit' }).click();
  await expect(page.getByText('File too large')).toBeVisible();
});

test.skip('fields round trip', async ({ page }) => {
  await page.goto('/jobs/new');
  await page.getByRole('tab', { name: 'Visual' }).click();
  await page.getByText('Aggiungi campo').click();
  await page.getByPlaceholder('Nome').fill('a');
  await page.getByRole('tab', { name: 'JSON' }).nth(1).click();
  const val = await page.getByRole('textbox').nth(1).inputValue();
  await page.getByRole('tab', { name: 'Visual' }).click();
  await expect(page.getByPlaceholder('Nome')).toHaveValue('a');
  await page.getByRole('tab', { name: 'JSON' }).nth(1).click();
  await page.getByRole('textbox').nth(1).fill(val.replace('a', 'b'));
  await page.getByRole('button', { name: 'Export to Visual' }).click();
  await page.getByRole('tab', { name: 'Visual' }).click();
  await expect(page.getByPlaceholder('Nome')).toHaveValue('b');
});

test.skip('idempotency key', async ({ page }) => {
  let called = 0;
  await page.route('**/jobs', (route) => {
    if (route.request().method() === 'OPTIONS') {
      route.fulfill({ status: 200 });
      return;
    }
    called++;
    route.fulfill({ status: 202, body: JSON.stringify({ id: '55' }) });
  });
  await page.goto('/jobs/new');
  await page.setInputFiles('input[type="file"]', uploadFile);
  await page.getByPlaceholder('Idempotency-Key').fill('key1');
  await page.getByRole('button', { name: 'Submit' }).click();
  await page.goto('/jobs/new');
  await page.setInputFiles('input[type="file"]', uploadFile);
  await page.getByPlaceholder('Idempotency-Key').fill('key1');
  await page.getByRole('button', { name: 'Submit' }).click();
  expect(called).toBe(2);
});
