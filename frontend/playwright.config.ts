import { defineConfig } from '@playwright/test';

export default defineConfig({
  testDir: './tests',
  use: {
    baseURL: 'http://localhost:4173',
    headless: true,
  },
  webServer: {
    command: 'VITE_API_BASE_URL="" npm run build && VITE_API_BASE_URL="" npm run preview -- --port 4173',
    port: 4173,
    reuseExistingServer: true,
  },
});
