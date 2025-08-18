import { test, expect } from '@playwright/test';
import path from 'path';
import fs from 'fs';
import { request as __request } from '../src/generated/core/request';
import { OpenAPI } from '../src/generated';
import { fileURLToPath } from 'url';

const sample = path.join(
  path.dirname(fileURLToPath(import.meta.url)),
  'assets',
  'sample.pdf'
);

async function createJob() {
  const fileBase64 = fs.readFileSync(sample).toString('base64');
  const payload = {
    fileBase64,
    fileName: 'sample.pdf',
    prompt: 'hi',
    fields: '[]',
  };
  const res = await __request(OpenAPI, {
    method: 'POST',
    url: '/api/v1/jobs',
    body: payload,
    mediaType: 'application/json',
  });
  return typeof res === 'string' ? JSON.parse(res) : res;
}

test.beforeEach(async ({ page }) => {
  await page.addInitScript(() =>
    localStorage.setItem('apiKey', 'dev-secret-key-change-me')
  );
});

test.skip('create job and view detail', async ({ page }) => {
  OpenAPI.BASE = 'http://localhost:5214';
  OpenAPI.HEADERS = { 'X-API-Key': 'dev-secret-key-change-me' };
  const job = await createJob();
  const jobId = job.job_id as string;
  await page.goto(`/jobs/${jobId}`);
  await expect(page.getByText(`Job ${jobId}`)).toBeVisible();
});
