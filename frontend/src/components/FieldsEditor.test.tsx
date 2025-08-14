import { render, screen, fireEvent } from '@testing-library/react';
import FieldsEditor, { fieldsToJson, jsonToFields, FieldItem } from './FieldsEditor';
import { test, expect, vi } from 'vitest';

test('fieldsToJson and jsonToFields round trip', () => {
  const fields: FieldItem[] = [
    { name: 'age', type: 'number' },
    { name: 'name', type: 'string' },
  ];
  const json = fieldsToJson(fields);
  expect(jsonToFields(json)).toEqual(fields);
});

test('add and remove fields', () => {
  const handle = vi.fn();
  render(<FieldsEditor value={[]} onChange={handle} />);
  fireEvent.click(screen.getByText('Aggiungi campo'));
  fireEvent.change(screen.getByPlaceholderText('Nome'), { target: { value: 'id' } });
  expect(handle).toHaveBeenLastCalledWith([{ name: 'id', type: 'string' }]);
  fireEvent.click(screen.getByRole('img', { name: 'minus-circle' }));
  expect(handle).toHaveBeenLastCalledWith([]);
});
