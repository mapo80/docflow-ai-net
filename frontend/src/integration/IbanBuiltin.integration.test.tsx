import { describe, it, expect, beforeAll, afterAll } from 'vitest';
import { spawn, ChildProcessWithoutNullStreams } from 'child_process';
import path from 'path';

let api: ChildProcessWithoutNullStreams;

beforeAll(async () => {
  api = spawn(
    'dotnet',
    ['run', '-c', 'Release', '--project', 'src/DocflowAi.Net.Api', '--urls', 'http://localhost:5056'],
    { cwd: path.resolve(__dirname, '../../..'), stdio: ['ignore', 'pipe', 'pipe'] },
  );
  await new Promise<void>((resolve, reject) => {
    const timer = setTimeout(() => reject(new Error('timeout')), 60000);
    api.stdout.on('data', (d) => {
      if (d.toString().includes('Now listening on')) {
        clearTimeout(timer);
        resolve();
      }
    });
    api.on('error', reject);
  });
}, 60000);

afterAll(() => {
  api.kill();
});

describe('Builtin IBAN rule integration', () => {
  it('normalizes and validates via real API', async () => {
    let resp = await fetch(
      'http://localhost:5056/api/v1/rules?search=Builtins.Iban.NormalizeAndValidate',
      {
        headers: { 'X-API-Key': 'dev-secret-key-change-me' },
      },
    );
    const list = (await resp.json()) as { items: Array<{ id: string }> };
    const ruleId = list.items[0].id;

    resp = await fetch(`http://localhost:5056/api/v1/rules/${ruleId}/run`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'X-API-Key': 'dev-secret-key-change-me',
      },
      body: JSON.stringify({
        input: {
          fields: {
            ibanRaw: { value: 'IT60 X054 2811 1010 0000 0123 456' },
          },
        },
      }),
    });
    const run = (await resp.json()) as { after: { iban: string } };
    expect(run.after.iban).toBe('IT60X0542811101000000123456');

    resp = await fetch(`http://localhost:5056/api/v1/rules/${ruleId}/tests/run`, {
      method: 'POST',
      headers: { 'X-API-Key': 'dev-secret-key-change-me' },
    });
    const tests = (await resp.json()) as Array<{ passed: boolean }>;
    expect(tests[0].passed).toBe(true);
  });
});
