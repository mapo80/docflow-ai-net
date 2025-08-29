import { render, screen, act } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import { GlobalLoaderProvider, globalLoader, withLoader } from './GlobalLoader';

describe('GlobalLoader', () => {
  it('shows loader when toggled', () => {
    render(
      <GlobalLoaderProvider>
        <div>content</div>
      </GlobalLoaderProvider>,
    );
    expect(screen.queryByLabelText('loading')).toBeNull();
    act(() => {
      globalLoader.show();
    });
    expect(screen.getByLabelText('loading')).toBeInTheDocument();
    act(() => {
      globalLoader.hide();
    });
    expect(screen.queryByLabelText('loading')).toBeNull();
  });

  it('wraps imports with loader visibility', async () => {
    const factory = vi.fn(async () => ({ default: {} }));
    const show = vi.spyOn(globalLoader, 'show');
    const hide = vi.spyOn(globalLoader, 'hide');
    const wrapped = withLoader(factory);
    await wrapped();
    expect(factory).toHaveBeenCalled();
    expect(show).toHaveBeenCalled();
    expect(hide).toHaveBeenCalled();
  });
});
