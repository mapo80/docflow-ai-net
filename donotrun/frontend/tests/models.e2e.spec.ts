import { test, expect } from '@playwright/test';

test.beforeEach(async ({ page }) => {
  await page.addInitScript(() =>
    localStorage.setItem('apiKey', 'dev-secret-key-change-me')
  );
});

test('create hosted model', async ({ page }) => {
  const models: any[] = [];
  await page.route('**/api/models/test-connection', (route) => route.fulfill({ status: 200 }));
  await page.route('**/api/models**', (route) => {
    const url = new URL(route.request().url());
    if (url.pathname.endsWith('/test-connection')) {
      route.fallback();
      return;
    }
    const method = route.request().method();
    if (method === 'GET') {
      route.fulfill({ json: models });
    } else if (method === 'POST') {
      const body = route.request().postDataJSON();
      const m = {
        id: String(models.length + 1),
        name: body.name,
        type: body.type,
        provider: body.provider,
        baseUrl: body.baseUrl,
        downloaded: null,
        downloadStatus: null,
      };
      models.push(m);
      route.fulfill({ json: m });
    } else {
      route.fallback();
    }
  });

  await page.goto('/models');
  await page.getByRole('button', { name: 'Add' }).click();
  await page.getByTestId('name').fill('h1');
  await page.getByTestId('type').click();
  await page.getByTestId('type-hosted').click();
  await page.getByTestId('provider').click();
  await page.getByTestId('provider-openai').click();
  await page.getByTestId('baseUrl').fill('https://api');
  await page.getByTestId('apiKey').fill('k');
  await page.getByRole('button', { name: 'Test Connection' }).click();
  await page.getByRole('button', { name: 'Save' }).click();
  expect(models).toHaveLength(1);
  expect(models[0].provider).toBe('openai');
});

test('create local model', async ({ page }) => {
  const models: any[] = [];
  await page.route('**/api/models**', (route) => {
    const url = new URL(route.request().url());
    if (url.pathname.endsWith('/test-connection')) {
      route.fallback();
      return;
    }
    const method = route.request().method();
    if (method === 'GET') {
      route.fulfill({ json: models });
    } else if (method === 'POST') {
      const body = route.request().postDataJSON();
      const m = {
        id: String(models.length + 1),
        name: body.name,
        type: body.type,
        hfRepo: body.hfRepo,
        modelFile: body.modelFile,
        downloaded: false,
        downloadStatus: 'pending',
      };
      models.push(m);
      route.fulfill({ json: m });
    } else {
      route.fallback();
    }
  });

  await page.goto('/models');
  await page.getByRole('button', { name: 'Add' }).click();
  await page.getByTestId('name').fill('l1');
  await page.getByTestId('type').click();
  await page.getByTestId('type-local').click();
  await page.getByTestId('hfToken').fill('t');
  await page.getByTestId('hfRepo').fill('r');
  await page.getByTestId('modelFile').fill('f.gguf');
  await page.getByRole('button', { name: 'Save' }).click();
  expect(models).toHaveLength(1);
  expect(models[0].hfRepo).toBe('r');
});

test.skip('update model', async ({ page }) => {
  const models: any[] = [
    { id: '1', name: 'm1', type: 'hosted-llm', provider: 'openai', baseUrl: 'u', downloaded: null },
  ];
  let patched = false;
  await page.route('**/api/models**', (route) => {
    const url = new URL(route.request().url());
    if (url.pathname.endsWith('/test-connection')) {
      route.fallback();
      return;
    }
    const method = route.request().method();
    if (method === 'GET') {
      route.fulfill({ json: models });
    } else if (method === 'PATCH') {
      patched = true;
      const id = url.pathname.split('/').pop()!;
      const body = route.request().postDataJSON();
      const idx = models.findIndex((m) => m.id === id);
      models[idx] = { ...models[idx], ...body };
      route.fulfill({ json: models[idx] });
    } else {
      route.fallback();
    }
  });

  await page.goto('/models');
  await page.getByRole('row', { name: /m1/ }).getByRole('button', { name: 'Edit' }).click();
  await page.getByTestId('name').fill('m2');
  await page.getByTestId('provider').click();
  await page.getByTestId('provider-azure').click();
  await page.getByTestId('baseUrl').fill('u');
  await page.getByRole('button', { name: 'Save' }).click();
  await expect.poll(() => patched).toBe(true);
  expect(models[0].name).toBe('m2');
  expect(models[0].provider).toBe('azure-openai');
});

test.skip('delete model', async ({ page }) => {
  const models: any[] = [
    { id: '1', name: 'd1', type: 'hosted-llm', provider: 'openai', baseUrl: 'u', downloaded: null },
  ];
  let removed = false;
  await page.route('**/api/models**', (route) => {
    const url = new URL(route.request().url());
    if (url.pathname.endsWith('/test-connection')) {
      route.fallback();
      return;
    }
    const method = route.request().method();
    if (method === 'GET') {
      route.fulfill({ json: models });
    } else if (method === 'DELETE') {
      removed = true;
      const id = url.pathname.split('/').pop()!;
      const idx = models.findIndex((m) => m.id === id);
      models.splice(idx, 1);
      route.fulfill({ status: 200 });
    } else {
      route.fallback();
    }
  });

  await page.goto('/models');
  await page.getByRole('row', { name: /d1/ }).waitFor();
  await page.getByRole('row', { name: /d1/ }).getByRole('button', { name: 'Delete' }).click();
  await expect.poll(() => removed).toBe(true);
  expect(models).toHaveLength(0);
});
