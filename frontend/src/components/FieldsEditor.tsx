import { Button, Input, Select, Space } from 'antd';
import type { FieldSpec } from '../api';

interface FieldsEditorProps {
  fields: FieldSpec[];
  onChange: (fields: FieldSpec[]) => void;
}

export default function FieldsEditor({ fields, onChange }: FieldsEditorProps) {
  const handleFieldChange = (index: number, key: keyof FieldSpec, value: string) => {
    const newFields = fields.map((f, i) => (i === index ? { ...f, [key]: value } : f));
    onChange(newFields);
  };

  const addField = () => {
    onChange([...fields, { name: '', type: 'string' }]);
  };

  const removeField = (index: number) => {
    const newFields = fields.filter((_, i) => i !== index);
    onChange(newFields);
  };

  return (
    <div>
      {fields.map((field, index) => (
        <Space key={index} style={{ marginBottom: 8 }}>
          <Input
            placeholder="Field name"
            value={field.name}
            onChange={(e) => handleFieldChange(index, 'name', e.target.value)}
          />
          <Select
            value={field.type}
            style={{ width: 120 }}
            onChange={(value) => handleFieldChange(index, 'type', value)}
          >
            <Select.Option value="string">string</Select.Option>
            <Select.Option value="number">number</Select.Option>
          </Select>
          <Button danger onClick={() => removeField(index)}>
            Remove
          </Button>
        </Space>
      ))}
      <Button type="dashed" onClick={addField}>
        Add Field
      </Button>
    </div>
  );
}
