import { render, fireEvent } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { describe, it, expect } from 'vitest';
import Shell from './Shell';

describe('Shell', () => {
  it('loads collapsed state from storage and toggles', () => {
    localStorage.setItem('menuCollapsed', 'true');
    const { getByLabelText } = render(
      <MemoryRouter initialEntries={['/jobs']}>
        <Shell />
      </MemoryRouter>,
    );
    const toggle = getByLabelText('toggle-menu');
    expect(toggle.querySelector('svg[data-icon="menu"]')).not.toBeNull();
    fireEvent.click(toggle);
    expect(localStorage.getItem('menuCollapsed')).toBe('false');
  });

  it('shows app icon and title', () => {
    localStorage.setItem('menuCollapsed', 'true');
    const { getAllByText, container } = render(
      <MemoryRouter initialEntries={['/jobs']}>
        <Shell />
      </MemoryRouter>,
    );
    expect(getAllByText('DocFlow AI').length).toBeGreaterThan(0);
    expect(container.querySelector('svg[data-icon="robot"]')).not.toBeNull();
  });

  it('renders drawer on mobile screens', () => {
    localStorage.setItem('menuCollapsed', 'false');
    render(
      <MemoryRouter initialEntries={['/jobs']}>
        <Shell />
      </MemoryRouter>,
    );
    // jsdom lacks matchMedia so useBreakpoint returns mobile
    expect(document.body.querySelector('.ant-drawer-open')).not.toBeNull();
  });
});
