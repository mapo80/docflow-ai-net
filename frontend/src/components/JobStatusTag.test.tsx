import { render, screen } from '@testing-library/react';
import { test, expect } from 'vitest';
import JobStatusTag, { statusColor } from './JobStatusTag';

test('status color mapping', () => {
  for (const [status, color] of Object.entries(statusColor)) {
    render(<JobStatusTag status={status} />);
    const tag = screen.getByText(status);
    expect(tag).toHaveClass(`ant-tag-${color}`);
  }
});
