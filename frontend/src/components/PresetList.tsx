import { List, Button, Space, Tag } from 'antd';
import { loadPresets, deletePreset, renamePreset, type ModelPreset } from './presetStore';
import { useState } from 'react';

type Props = {
  onApply: (preset: ModelPreset) => void;
};

export default function PresetList({ onApply }: Props) {
  const [presets, setPresets] = useState<ModelPreset[]>(loadPresets());

  const handleDelete = (name: string) => {
    deletePreset(name);
    setPresets(loadPresets());
  };

  const handleRename = (p: ModelPreset) => {
    const name = window.prompt('Nuovo nome', p.name);
    if (!name) return;
    renamePreset(p.name, name);
    setPresets(loadPresets());
  };

  return (
    <List
      header="Preset"
      dataSource={presets}
      renderItem={(p) => (
        <List.Item
          actions={[
            <Button onClick={() => onApply(p)}>Applica</Button>,
            <Button onClick={() => handleRename(p)}>Rinomina</Button>,
            <Button danger onClick={() => handleDelete(p.name)}>Elimina</Button>,
          ]}
        >
          <Space>
            <Tag>{p.name}</Tag>
            {p.repo}/{p.file} ({p.contextSize})
          </Space>
        </List.Item>
      )}
    />
  );
}
