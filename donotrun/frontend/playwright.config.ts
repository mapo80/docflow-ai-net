import { defineConfig } from '@playwright/test';

export default defineConfig({
  testDir: './tests',
  use: {
    baseURL: 'http://localhost:4173',
    headless: true,
  },
  webServer: [
    {
      command:
        'dotnet run --no-build --configuration Release --urls http://localhost:5214 --project ../src/DocflowAi.Net.Api',
      port: 5214,
      reuseExistingServer: true,
    },
    {
      command: 'npm run build && npm run preview -- --port 4173',
      port: 4173,
      reuseExistingServer: true,
      env: { VITE_API_BASE_URL: 'http://localhost:5214' },
    },
  ],
});
