import { render, fireEvent } from '@testing-library/react';
import FieldsTable from './FieldsTable';
import { describe, it, expect, vi } from 'vitest';

const fields = [
  { id: 'f1', name: 'buyer', value: 'Acme', page: 1, wordIds: ['w1'], conf: 0.9 },
  { id: 'f2', name: 'total', value: '10', page: 1, wordIds: ['w2'], conf: 0.8 },
];

describe('FieldsTable', () => {
  it('selects row on click', () => {
    const onSelect = vi.fn();
    const { getByText } = render(
      <FieldsTable fields={fields} selectedFieldId="f1" onFieldSelect={onSelect} />,
    );
    fireEvent.click(getByText('total'));
    expect(onSelect).toHaveBeenCalledWith('f2');
  });

  it('navigates with keyboard', () => {
    const onSelect = vi.fn();
    const { getAllByTestId } = render(
      <FieldsTable fields={fields} selectedFieldId="f1" onFieldSelect={onSelect} />,
    );
    const tables = getAllByTestId('fields-table');
    const table = tables[tables.length - 1];
    fireEvent.keyDown(table, { key: 'ArrowDown' });
    expect(onSelect).toHaveBeenCalledWith('f2');
  });
});
