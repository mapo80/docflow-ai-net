/// <reference types="vitest" />
import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import path from 'path';
import { fileURLToPath } from 'url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));

export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
    },
  },
  build: {
    chunkSizeWarningLimit: 500,
    rollupOptions: {
      output: {
        manualChunks(id) {
          if (id.includes('node_modules')) {
            const parts = id.split('node_modules/')[1].split('/');
            const pkg = parts[0].startsWith('@') ? `${parts[0]}/${parts[1]}` : parts[0];
            if (['react', 'react-dom', 'react-router-dom'].includes(pkg)) {
              return 'react';
            }
            if (pkg === 'refractor') {
              const langIndex = parts.indexOf('lang');
              if (langIndex > -1 && parts.length > langIndex + 1) {
                return `refractor-${parts[langIndex + 1]}`;
              }
              return 'refractor';
            }
            if (pkg === 'antd' || pkg.startsWith('@ant-design')) {
              return undefined;
            }
            return pkg;
          }
        },
      },
    },
  },
  // @ts-ignore - Vitest configuration
  test: {
    environment: 'jsdom',
    setupFiles: './src/setupTests.ts',
    include: ['src/**/*.{test,spec}.tsx'],
    exclude: ['tests/**', '**/*.e2e.*', 'node_modules/**'],
  },
});
