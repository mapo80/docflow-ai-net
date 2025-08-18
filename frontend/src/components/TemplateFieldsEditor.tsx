import { useState, useEffect } from 'react';
import { Tabs, Input, Button, Space, Grid } from 'antd';
import FieldsEditor, {
  fieldsToJson,
  jsonToFields,
  type FieldItem,
} from './FieldsEditor';

interface Props {
  value: FieldItem[];
  onChange: (fields: FieldItem[]) => void;
  onJsonError?: (msg: string | null) => void;
}

export default function TemplateFieldsEditor({ value, onChange, onJsonError }: Props) {
  const [mode, setMode] = useState<'visual' | 'json'>('visual');
  const [visual, setVisual] = useState<FieldItem[]>(value);
  const [json, setJson] = useState(fieldsToJson(value));
  const screens = Grid.useBreakpoint();
  const isMobile = !screens.md;

  useEffect(() => {
    setVisual(value);
    setJson(fieldsToJson(value));
  }, [value]);

  const updateVisual = (fields: FieldItem[]) => {
    setVisual(fields);
    setJson(fieldsToJson(fields));
    onJsonError?.(null);
    onChange(fields);
  };

  const handleJsonChange = (text: string) => {
    setJson(text);
    try {
      const fields = jsonToFields(text);
      setVisual(fields);
      onChange(fields);
      onJsonError?.(null);
    } catch (e: any) {
      onJsonError?.(e.message);
    }
  };

  return (
    <Tabs
      activeKey={mode}
      onChange={(k) => setMode(k as 'visual' | 'json')}
      items={[
        {
          key: 'visual',
          label: 'Visual',
          children: <FieldsEditor value={visual} onChange={updateVisual} />,
        },
        {
          key: 'json',
          label: 'JSON',
          children: (
            <>
              <label htmlFor="fields-json">Fields JSON</label>
              <Input.TextArea
                id="fields-json"
                rows={isMobile ? 6 : 4}
                value={json}
                onChange={(e) => handleJsonChange(e.target.value)}
              />
              <Space style={{ marginTop: 8 }}>
                <Button onClick={() => setJson(fieldsToJson(visual))}>Import from Visual</Button>
                <Button
                  onClick={() => {
                    try {
                      updateVisual(jsonToFields(json));
                    } catch {
                      onJsonError?.('Invalid JSON');
                    }
                  }}
                >
                  Export to Visual
                </Button>
              </Space>
            </>
          ),
        },
      ]}
    />
  );
}
