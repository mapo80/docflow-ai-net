import { useState, useEffect } from 'react';
import { Tabs, Input, Button, Grid } from 'antd';
import JSONView from '@uiw/react-json-view';
import type { SimpleField } from '../templates/fieldsConversion';
import { simpleToJsonText, jsonTextToSimple, simpleToObject } from '../templates/fieldsConversion';

interface Props {
  value: SimpleField[];
  onChange: (fields: SimpleField[]) => void;
  onJsonError?: (msg: string | null) => void;
}

export default function TemplateFieldsEditor({ value, onChange, onJsonError }: Props) {
  const [mode, setMode] = useState<'simple' | 'json'>('simple');
  const [simple, setSimple] = useState<SimpleField[]>(value);
  const [jsonText, setJsonText] = useState(simpleToJsonText(value));
  const [dragIndex, setDragIndex] = useState<number | null>(null);
  const screens = Grid.useBreakpoint();
  const isMobile = !screens.md;

  useEffect(() => {
    setSimple(value);
    setJsonText(simpleToJsonText(value));
  }, [value]);

  const updateSimple = (fields: SimpleField[]) => {
    setSimple(fields);
    setJsonText(simpleToJsonText(fields));
    onJsonError?.(null);
    onChange(fields);
  };

  const addField = () => updateSimple([...simple, { key: '', value: '' }]);
  const removeField = (i: number) => updateSimple(simple.filter((_, idx) => idx !== i));
  const changeField = (i: number, field: Partial<SimpleField>) =>
    updateSimple(simple.map((f, idx) => (idx === i ? { ...f, ...field } : f)));

  const handleJsonChange = (text: string) => {
    setJsonText(text);
    try {
      const fields = jsonTextToSimple(text);
      onJsonError?.(null);
      setSimple(fields);
      onChange(fields);
    } catch (e: any) {
      onJsonError?.(e.message);
    }
  };

  const onDragStart = (i: number) => () => setDragIndex(i);
  const onDragOver = (i: number) => (e: React.DragEvent) => {
    e.preventDefault();
    if (dragIndex === null || dragIndex === i) return;
    const next = [...simple];
    const [moved] = next.splice(dragIndex, 1);
    next.splice(i, 0, moved);
    setDragIndex(i);
    updateSimple(next);
  };

  return (
    <Tabs
      activeKey={mode}
      onChange={(k) => setMode(k as 'simple' | 'json')}
      items={[
        {
          key: 'simple',
          label: 'Simple',
          children: (
            <div
              style={{
                display: 'flex',
                gap: 16,
                flexDirection: isMobile ? 'column' : 'row',
              }}
            >
              <div style={{ flex: 1 }}>
                {simple.map((f, i) => (
                  <div
                    key={i}
                    style={{ display: 'flex', gap: 8, marginBottom: 8 }}
                    draggable
                    onDragStart={onDragStart(i)}
                    onDragOver={onDragOver(i)}
                  >
                    <Input
                      placeholder="Key"
                      value={f.key}
                      onChange={(e) => changeField(i, { key: e.target.value })}
                    />
                    <Input
                      placeholder="Value"
                      value={f.value}
                      onChange={(e) => changeField(i, { value: e.target.value })}
                    />
                    <Button aria-label="delete" onClick={() => removeField(i)}>
                      Delete
                    </Button>
                  </div>
                ))}
                <Button onClick={addField}>Add field</Button>
              </div>
              <div style={{ flex: 1, maxHeight: 200, overflow: 'auto' }}>
                <JSONView value={simpleToObject(simple)} />
              </div>
            </div>
          ),
        },
        {
          key: 'json',
          label: 'JSON',
          children: (
            <div
              style={{
                display: 'flex',
                gap: 16,
                flexDirection: isMobile ? 'column' : 'row',
              }}
            >
              <div style={{ flex: 1 }}>
                <Input.TextArea
                  value={jsonText}
                  onChange={(e) => handleJsonChange(e.target.value)}
                  rows={10}
                />
              </div>
              <div style={{ flex: 1, maxHeight: 200, overflow: 'auto' }}>
                {simple.map((f, i) => (
                  <div key={i}>
                    {f.key}: {f.value}
                  </div>
                ))}
              </div>
            </div>
          ),
        },
      ]}
    />
  );
}
