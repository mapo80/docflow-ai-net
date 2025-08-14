import { useState, useEffect } from 'react';
import { Button, Input, Select, Space } from 'antd';
import { MinusCircleOutlined, PlusOutlined } from '@ant-design/icons';

export interface FieldItem {
  name: string;
  type: string;
}

export function fieldsToJson(fields: FieldItem[]): string {
  return JSON.stringify({ fields }, null, 2);
}

export function jsonToFields(json: string): FieldItem[] {
  const parsed = JSON.parse(json);
  if (!parsed || !Array.isArray(parsed.fields)) return [];
  return parsed.fields as FieldItem[];
}

interface Props {
  value?: FieldItem[];
  onChange?: (fields: FieldItem[]) => void;
}

export default function FieldsEditor({ value = [], onChange }: Props) {
  const [fields, setFields] = useState<FieldItem[]>(value);

  useEffect(() => {
    setFields(value);
  }, [value]);

  const update = (next: FieldItem[]) => {
    setFields(next);
    onChange?.(next);
  };

  const add = () => update([...fields, { name: '', type: 'string' }]);

  const remove = (index: number) => {
    const next = fields.filter((_, i) => i !== index);
    update(next);
  };

  const change = (index: number, field: Partial<FieldItem>) => {
    const next = fields.map((f, i) => (i === index ? { ...f, ...field } : f));
    update(next);
  };

  return (
    <>
      {fields.map((field, idx) => (
        <Space key={idx} align="baseline" style={{ display: 'flex', marginBottom: 8 }}>
          <Input
            placeholder="Nome"
            value={field.name}
            onChange={(e) => change(idx, { name: e.target.value })}
          />
          <Select
            placeholder="Tipo"
            style={{ width: 120 }}
            value={field.type}
            onChange={(v) => change(idx, { type: v })}
            options={[
              { value: 'string' },
              { value: 'number' },
              { value: 'boolean' },
              { value: 'date' },
            ]}
          />
          <MinusCircleOutlined onClick={() => remove(idx)} />
        </Space>
      ))}
      <Button type="dashed" onClick={add} block icon={<PlusOutlined />}>Aggiungi campo</Button>
    </>
  );
}
