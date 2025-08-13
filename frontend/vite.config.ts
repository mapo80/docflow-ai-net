/// <reference types="vitest" />
import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

export default defineConfig({
  plugins: [react()],
  // @ts-ignore - Vitest configuration
  test: {
    environment: 'jsdom',
    setupFiles: './src/setupTests.ts',
    include: ['src/**/*.{test,spec}.tsx'],
    exclude: ['tests/**', '**/*.e2e.*', 'node_modules/**'],
  },
});
