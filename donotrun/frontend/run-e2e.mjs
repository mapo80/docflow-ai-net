import { spawn } from 'node:child_process';

const child = spawn('npx', ['playwright', 'test'], {
  stdio: 'inherit',
  env: process.env,
  shell: true,
});

const interval = setInterval(() => {
  console.log('[e2e] running...');
}, 30000);

child.on('close', (code) => {
  clearInterval(interval);
  process.exit(code ?? 0);
});
