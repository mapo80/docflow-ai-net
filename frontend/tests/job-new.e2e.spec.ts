import { test, expect } from '@playwright/test';
import path from 'path';
import fs from 'fs';

const uploadFile = path.join(process.cwd(), 'tmp.txt');
fs.writeFileSync(uploadFile, 'hello');

test.beforeEach(async ({ page }) => {
  await page.addInitScript(() => localStorage.setItem('apiKey', 'test'));
});

async function fillBasic(page) {
  await page.getByRole('menuitem', { name: 'Nuovo Job' }).click();
  await page.setInputFiles('input[type="file"]', uploadFile);
  await page.getByRole('textbox').first().fill('hi');
}

test('immediate success', async ({ page }) => {
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
  await page.evaluate(() => localStorage.setItem('apiKey', 'test'));
  await page.reload();
  await fillBasic(page);
  await page.getByRole('checkbox', { name: /Esegui immediatamente/ }).check();
  await page.getByRole('button', { name: 'Submit' }).click();
  await expect(page.getByText('Status: Succeeded')).toBeVisible();
  await page.getByRole('button', { name: 'Vai al dettaglio' }).click();
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
  await page.getByRole('checkbox', { name: /Esegui immediatamente/ }).check();
  await page.getByRole('button', { name: 'Submit' }).click();
  await expect(page.getByText(/Riprova tra/)).toBeVisible();
});

test('queued 202', async ({ page }) => {
  await page.route('**/jobs', (route) => {
    if (route.request().method() === 'OPTIONS') {
      route.fulfill({ status: 200 });
      return;
    }
    if (route.request().url().includes('mode=immediate')) {
      route.fallback();
      return;
    }
    route.fulfill({
      status: 202,
      body: JSON.stringify({ id: '42' }),
    });
  });
  await page.goto('/jobs/new');
  await page.setInputFiles('input[type="file"]', uploadFile);
  await page.getByRole('button', { name: 'Submit' }).click();
  await expect(page).toHaveURL(/\/jobs\/42$/);
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
  await expect(page.getByText('File troppo grande')).toBeVisible();
});

test.skip('fields round trip', async ({ page }) => {
  await page.goto('/jobs/new');
  await page.getByRole('tab', { name: 'Visuale' }).click();
  await page.getByText('Aggiungi campo').click();
  await page.getByPlaceholder('Nome').fill('a');
  await page.getByRole('tab', { name: 'JSON' }).nth(1).click();
  const val = await page.getByRole('textbox').nth(1).inputValue();
  await page.getByRole('tab', { name: 'Visuale' }).click();
  await expect(page.getByPlaceholder('Nome')).toHaveValue('a');
  await page.getByRole('tab', { name: 'JSON' }).nth(1).click();
  await page.getByRole('textbox').nth(1).fill(val.replace('a', 'b'));
  await page.getByRole('button', { name: 'Esporta in Visuale' }).click();
  await page.getByRole('tab', { name: 'Visuale' }).click();
  await expect(page.getByPlaceholder('Nome')).toHaveValue('b');
});

test('idempotency key', async ({ page }) => {
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
