import { test, expect } from '@playwright/test';

test.beforeEach(async ({ page }) => {
  await page.addInitScript(() =>
    localStorage.setItem('apiKey', 'dev-secret-key-change-me')
  );
});

test.skip('lista paginata', async ({ page }) => {
  await page.route('**/jobs*', (route) => {
    const url = new URL(route.request().url());
    const pageParam = url.searchParams.get('page');
    if (pageParam === '1') {
      route.fulfill({
        json: { items: [{ id: '1', status: 'Queued', createdAt: '', updatedAt: '' }], page: 1, pageSize: 10, total: 20 },
      });
    } else if (pageParam === '2') {
      route.fulfill({
        json: { items: [{ id: '2', status: 'Succeeded', createdAt: '', updatedAt: '' }], page: 2, pageSize: 10, total: 20 },
      });
    }
  });
  await page.goto('/jobs');
  await expect(page.getByText('1')).toBeVisible();
  await page.getByRole('button', { name: '2' }).click();
  await expect(page.getByText('2')).toBeVisible();
});

test.skip('cancel action', async ({ page }) => {
  await page.route('**/jobs*', (route) => {
    route.fulfill({
      json: {
        items: [
          { id: 'r1', status: 'Running', createdAt: '', updatedAt: '' },
          { id: 's1', status: 'Succeeded', createdAt: '', updatedAt: '' },
        ],
        page: 1,
        pageSize: 10,
        total: 2,
      },
    });
  });
  await page.route('**/jobs/r1', (route) => {
    if (route.request().method() === 'OPTIONS') {
      route.fulfill({ status: 200 });
    } else if (route.request().method() === 'DELETE') {
      route.fulfill({ status: 202 });
    } else {
      route.continue();
    }
  });
  await page.route('**/jobs/s1', (route) => {
    if (route.request().method() === 'OPTIONS') {
      route.fulfill({ status: 200 });
    } else if (route.request().method() === 'DELETE')
      route.fulfill({ status: 409, body: JSON.stringify({ errorCode: 'conflict' }) });
    else route.continue();
  });
  await page.goto('/jobs');
  await page.getByRole('row', { name: /r1/ }).getByRole('button', { name: 'Cancel' }).click();
  await expect(page.getByText('Job cancellato')).toBeVisible();
  await page.getByRole('row', { name: /s1/ }).getByRole('button', { name: 'Cancel' }).click();
  await expect(page.getByText('conflict')).toBeVisible();
});

test.skip('dettaglio polling', async ({ page }) => {
  let called = 0;
  await page.route('**/jobs/1', (route) => {
    called++;
    if (called === 1)
      route.fulfill({ json: { id: '1', status: 'Running', createdAt: '', updatedAt: '', paths: { output: '/out.json' } } });
    else
      route.fulfill({ json: { id: '1', status: 'Succeeded', createdAt: '', updatedAt: '', paths: { output: '/out.json' } } });
  });
  await page.route('**/out.json', (route) => route.fulfill({ json: { ok: true } }));
  await page.goto('/jobs/1');
  await expect(page.getByText('Running')).toBeVisible();
  await expect(page.getByText('Succeeded')).toBeVisible({ timeout: 7000 });
  await page.getByText('output').click();
  await expect(page.getByText(/"ok"/)).toBeVisible();
});

test.skip('429 queue_full', async ({ page }) => {
  let called = 0;
  await page.route('**/jobs*', (route) => {
    called++;
    if (called === 1)
      route.fulfill({ status: 429, headers: { 'Retry-After': '1' }, body: JSON.stringify({ errorCode: 'queue_full' }) });
    else
      route.fulfill({ json: { items: [{ id: '1', status: 'Queued', createdAt: '', updatedAt: '' }], page: 1, pageSize: 10, total: 1 } });
  });
  await page.goto('/jobs');
  await expect(page.getByText(/Coda piena/)).toBeVisible();
  await expect(page.getByText('1')).toBeVisible({ timeout: 3000 });
});

test('hangfire button opens new window', async ({ page, context }) => {
  await page.goto('/jobs');
  const [newPage] = await Promise.all([
    context.waitForEvent('page'),
    page.getByRole('button', { name: 'Apri Hangfire' }).click(),
  ]);
  await newPage.waitForLoadState();
  expect(newPage.url()).toContain('api_key=dev-secret-key-change-me');
});
