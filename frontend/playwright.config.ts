import { defineConfig } from '@playwright/test';

export default defineConfig({
  testDir: './tests',
  use: {
    baseURL: 'http://localhost:5000',
    headless: true,
  },
  webServer: {
    command: 'npm run build && rm -rf ../src/DocflowAi.Net.Api/wwwroot/* && cp -r dist/* ../src/DocflowAi.Net.Api/wwwroot/ && dotnet run --project ../src/DocflowAi.Net.Api --urls http://localhost:5000',
    port: 5000,
    reuseExistingServer: true,
  },
});
