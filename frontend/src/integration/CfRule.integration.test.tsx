import { describe, it, expect, beforeAll, afterAll } from 'vitest';
import { spawn, ChildProcessWithoutNullStreams } from 'child_process';
import path from 'path';

let api: ChildProcessWithoutNullStreams;

beforeAll(async () => {
  api = spawn(
    'dotnet',
    ['run', '-c', 'Release', '--project', 'src/DocflowAi.Net.Api', '--urls', 'http://localhost:5055'],
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

describe('Codice fiscale rule integration', () => {
  it('normalizes CF via real API', async () => {
    const code =
      'if (g.Has("cf")) { var value = g.Get("cf")?.ToString() ?? string.Empty; value = new string(value.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant(); g.Set("cf", value); }';

    const name = `cf-rule-${Date.now()}`;
    let resp = await fetch('http://localhost:5055/api/v1/rules', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'X-API-Key': 'dev-secret-key-change-me',
      },
      body: JSON.stringify({ name, code, readsCsv: null, writesCsv: null, enabled: true }),
    });
    const rule = (await resp.json()) as { id: string };

    resp = await fetch(`http://localhost:5055/api/v1/rules/${rule.id}/run`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'X-API-Key': 'dev-secret-key-change-me',
      },
      body: JSON.stringify({ input: { fields: { cf: { value: ' rss-mra85t10 a562 s ' } } } }),
    });
    let run = (await resp.json()) as { after: { cf: string } };
    expect(run.after.cf).toBe('RSSMRA85T10A562S');

    resp = await fetch(`http://localhost:5055/api/v1/rules/${rule.id}/run`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'X-API-Key': 'dev-secret-key-change-me',
      },
      body: JSON.stringify({ input: { fields: { cf: { value: run.after.cf } } } }),
    });
    run = (await resp.json()) as { after: { cf: string } };
    expect(run.after.cf).toBe('RSSMRA85T10A562S');
  });
});
