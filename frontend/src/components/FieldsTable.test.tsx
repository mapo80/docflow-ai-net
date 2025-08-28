import { render, fireEvent, cleanup } from '@testing-library/react';
import FieldsTable from './FieldsTable';
import { describe, it, expect, vi, afterEach } from 'vitest';

const fields = [
  {
    id: 'f1',
    name: 'buyer',
    value: 'Acme',
    page: 1,
    wordIds: ['w1'],
    hasBbox: true,
    conf: 0.9,
  },
  {
    id: 'f2',
    name: 'total',
    value: '10',
    page: 1,
    wordIds: [],
    hasBbox: false,
    conf: 0.8,
  },
];

describe('FieldsTable', () => {
  afterEach(() => cleanup());
  it('selects row on click', () => {
    const onSelect = vi.fn();
    const { getByText } = render(
      <FieldsTable
        docType="pdf"
        fields={fields}
        selectedFieldId="f1"
        onFieldSelect={onSelect}
      />,
    );
    fireEvent.click(getByText('total'));
    expect(onSelect).toHaveBeenCalledWith('f2');
  });

  it('navigates with keyboard', () => {
    const onSelect = vi.fn();
    const { getAllByTestId } = render(
      <FieldsTable
        docType="pdf"
        fields={fields}
        selectedFieldId="f1"
        onFieldSelect={onSelect}
      />,
    );
    const tables = getAllByTestId('fields-table');
    const table = tables[tables.length - 1];
    fireEvent.keyDown(table, { key: 'ArrowDown' });
    expect(onSelect).toHaveBeenCalledWith('f2');
  });

  it('sets horizontal scroll', () => {
    const { container } = render(
      <FieldsTable
        docType="pdf"
        fields={fields}
        selectedFieldId="f1"
        onFieldSelect={() => {}}
      />,
    );
    const body = container.querySelector(
      '.ant-table-content',
    ) as HTMLElement;
    expect(body.style.overflowX).toBe('auto');
  });

  it('shows page dash and bbox icons for images', () => {
    const { getAllByText } = render(
      <FieldsTable
        docType="image"
        fields={fields}
        selectedFieldId="f1"
        onFieldSelect={() => {}}
      />,
    );
    expect(getAllByText('-').length).toBeGreaterThan(0);
    expect(getAllByText('✅').length).toBeGreaterThan(0);
    expect(getAllByText('❌').length).toBeGreaterThan(0);
  });
});
