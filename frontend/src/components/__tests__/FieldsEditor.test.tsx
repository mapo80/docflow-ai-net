import { fireEvent, render, screen } from '@testing-library/react';
import { useState } from 'react';
import { expect, test } from 'vitest';
import FieldsEditor from '../FieldsEditor';
import type { FieldSpec } from '../../api';

test('add and remove fields', () => {
  function Wrapper() {
    const [fields, setFields] = useState<FieldSpec[]>([]);
    return <FieldsEditor fields={fields} onChange={setFields} />;
  }

  render(<Wrapper />);
  fireEvent.click(screen.getByText('Add Field'));
  expect(screen.getAllByPlaceholderText('Field name')).toHaveLength(1);
  fireEvent.click(screen.getByText('Remove'));
  expect(screen.queryByPlaceholderText('Field name')).toBeNull();
});
