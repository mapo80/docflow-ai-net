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
    expect(toggle.querySelector('svg[data-icon="menu-unfold"]')).not.toBeNull();
    fireEvent.click(toggle);
    expect(localStorage.getItem('menuCollapsed')).toBe('false');
  });
});
