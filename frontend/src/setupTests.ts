import '@testing-library/jest-dom/vitest';
import { vi } from 'vitest';

// polyfills for Ant Design components in jsdom
const matchMediaMock = vi.fn().mockImplementation((query: string) => ({
  matches: false,
  media: query,
  onchange: null,
  addListener: vi.fn(),
  removeListener: vi.fn(),
  addEventListener: vi.fn(),
  removeEventListener: vi.fn(),
  dispatchEvent: vi.fn(),
}));

Object.defineProperty(window, 'matchMedia', {
  writable: true,
  value: matchMediaMock,
});
Object.defineProperty(globalThis, 'matchMedia', {
  writable: true,
  value: matchMediaMock,
});

// jsdom doesn't implement this fully
window.getComputedStyle = vi.fn(() => ({
  getPropertyValue: () => '',
}) as any);

// polyfill ResizeObserver for libraries like Recharts
class ResizeObserver {
  observe() {}
  unobserve() {}
  disconnect() {}
}
(window as any).ResizeObserver = ResizeObserver;
(globalThis as any).ResizeObserver = ResizeObserver;
