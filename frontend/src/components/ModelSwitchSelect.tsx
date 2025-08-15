import { Select, InputNumber, Button, Space, Grid } from 'antd';
import { useState } from 'react';

type Props = {
  models: string[];
  onSwitch: (file: string, contextSize: number) => Promise<void>;
  disabled?: boolean;
  initialFile?: string;
};

export default function ModelSwitchSelect({ models, onSwitch, disabled, initialFile }: Props) {
  const [file, setFile] = useState<string | undefined>(initialFile);
  const [ctx, setCtx] = useState<number>(1024);
  const screens = Grid.useBreakpoint();

  const handle = async () => {
    if (!file) return;
    await onSwitch(file, ctx);
  };

  return (
    <Space
      direction={screens.xs ? 'vertical' : 'horizontal'}
      style={{ width: '100%' }}
      align={screens.xs ? 'start' : 'center'}
    >
      <Select
        style={{ width: screens.xs ? '100%' : 200 }}
        placeholder="Seleziona modello"
        value={file}
        onChange={setFile}
        options={models.map((m) => ({ value: m, label: m }))}
        disabled={disabled}
      />
      <InputNumber
        min={1024}
        max={16384}
        value={ctx}
        onChange={(v) => setCtx(v ?? 0)}
        disabled={disabled}
        style={{ width: screens.xs ? '100%' : undefined }}
      />
      <Button
        onClick={handle}
        disabled={!file || disabled}
        data-testid="switch-btn"
        block={screens.xs}
      >
        Switch
      </Button>
    </Space>
  );
}
