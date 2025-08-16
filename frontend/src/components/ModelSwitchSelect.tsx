import { Select, InputNumber, Button, Space, Grid } from 'antd';
import ReloadOutlined from '@ant-design/icons/ReloadOutlined';
import { useState } from 'react';

type Props = {
  models: string[];
  onSwitch: (file: string, contextSize: number) => Promise<void>;
  onReload?: () => Promise<void> | void;
  disabled?: boolean;
  initialFile?: string;
};

export default function ModelSwitchSelect({
  models,
  onSwitch,
  onReload,
  disabled,
  initialFile,
}: Props) {
  const [file, setFile] = useState<string | undefined>(initialFile);
  const [ctx, setCtx] = useState<number>(1024);
  const [refreshing, setRefreshing] = useState(false);
  const screens = Grid.useBreakpoint();

  const handle = async () => {
    if (!file) return;
    await onSwitch(file, ctx);
  };

  const reload = async () => {
    if (!onReload) return;
    setRefreshing(true);
    await onReload();
    setRefreshing(false);
  };

  const inputWidth = screens.xs ? '100%' : 200;

  return (
    <Space
      direction={screens.xs ? 'vertical' : 'horizontal'}
      style={{ width: '100%' }}
      align={screens.xs ? 'start' : 'center'}
    >
      <div style={{ width: inputWidth }}>
        <label htmlFor="model-select">Model file</label>
        <Space.Compact style={{ width: '100%' }}>
          <Select
            id="model-select"
            style={{ width: '100%' }}
            placeholder="Select model"
            value={file}
            onChange={setFile}
            options={models.map((m) => ({ value: m, label: m }))}
            disabled={disabled}
            data-testid="model-select"
          />
          <Button
            icon={<ReloadOutlined />}
            onClick={reload}
            disabled={disabled}
            loading={refreshing}
            data-testid="reload-models"
          />
        </Space.Compact>
      </div>
      <div style={{ width: inputWidth }}>
        <label htmlFor="context-size">Context size</label>
        <InputNumber
          id="context-size"
          min={1024}
          max={16384}
          value={ctx}
          onChange={(v) => setCtx(v ?? 0)}
          disabled={disabled}
          style={{ width: '100%' }}
        />
      </div>
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
