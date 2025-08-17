import { defineConfig } from '@playwright/test'
export default defineConfig({
  timeout: 60_000,
  retries: 1,
  use: { baseURL: 'http://localhost:5173', trace: 'on-first-retry', screenshot: 'only-on-failure', video: 'retain-on-failure' },
  webServer: {
    command: 'npm run dev',
    port: 5173,
    reuseExistingServer: !process.env.CI,
    timeout: 60_000,
  },
})
