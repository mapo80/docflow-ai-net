import '@testing-library/jest-dom/vitest';
import { vi } from 'vitest';

// polyfills for Ant Design components in jsdom
Object.defineProperty(window, 'matchMedia', {
  writable: true,
  value: vi.fn().mockImplementation((query: string) => ({
    matches: false,
    media: query,
    onchange: null,
    addListener: vi.fn(),
    removeListener: vi.fn(),
    addEventListener: vi.fn(),
    removeEventListener: vi.fn(),
    dispatchEvent: vi.fn(),
  })),
});

// jsdom doesn't implement this fully
window.getComputedStyle = vi.fn(() => ({
  getPropertyValue: () => '',
}) as any);
