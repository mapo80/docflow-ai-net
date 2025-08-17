import { defineConfig } from 'vitest/config'
import react from '@vitejs/plugin-react'
export default defineConfig({
  plugins: [react()],
  test: { environment: 'jsdom', setupFiles: ['./vitest.setup.ts'], coverage: { reporter: ['text','html'], provider: 'istanbul', reportsDirectory: './coverage', all: true, include: ['src/**/*.{ts,tsx}'], exclude: ['src/main.tsx', 'src/vite-env.d.ts'] , thresholds: { statements: 0.8, branches: 0.8, functions: 0.8, lines: 0.8 } } }
})
