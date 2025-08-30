import { defineConfig } from 'vitest/config';
import react from '@vitejs/plugin-react';

export default defineConfig({
  plugins: [react()],
  test: {
    environment: 'jsdom',
    setupFiles: './src/setupTests.ts',
    include: ['src/integration/**/*.test.tsx'],
    coverage: {
      provider: 'v8',
      include: ['src/pages/RulesPage.tsx'],
      thresholds: { lines: 0.9, functions: 0.9, branches: 0.9, statements: 0.9 },
    },
  },
});
