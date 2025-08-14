import { Button, Form, Input, InputNumber, Space, message } from 'antd';
import { useEffect } from 'react';
import type { ModelSwitchRequest } from '../generated';
import { savePreset, type ModelPreset } from './presetStore';

type Props = {
  onSubmit: (req: ModelSwitchRequest) => Promise<void>;
  disabled?: boolean;
};

export default function ModelSwitchForm({ onSubmit, disabled }: Props) {
  const [form] = Form.useForm();

  const handleFinish = async () => {
    try {
      const values = await form.validateFields();
      const req: ModelSwitchRequest = {
        token: values.token,
        repo: values.repo.trim(),
        file: values.file.trim(),
        contextSize: values.contextSize,
        note: values.note?.trim(),
      };
      await onSubmit(req);
      form.resetFields(['token']);
    } catch (e) {
      // validation error
    }
  };

  const handlePreset = () => {
    const values = form.getFieldsValue();
    const name = window.prompt('Nome preset');
    if (!name) return;
    const preset: ModelPreset = {
      name,
      repo: values.repo || '',
      file: values.file || '',
      contextSize: values.contextSize || 0,
      updatedAt: new Date().toISOString(),
    };
    savePreset(preset);
    message.success('Preset salvato');
  };

  useEffect(() => {
    const preset = localStorage.getItem('last-model');
    if (preset) {
      try {
        const p = JSON.parse(preset);
        form.setFieldsValue(p);
      } catch {
        /* ignore */
      }
    }
  }, [form]);

  return (
    <Form layout="vertical" form={form} disabled={disabled}>
      <Form.Item name="token" label="HF token" rules={[{ required: true }]}> 
        <Input.Password autoComplete="new-password" />
      </Form.Item>
      <Form.Item name="repo" label="HF repo" rules={[{ required: true }]}> 
        <Input placeholder="TheOrg/the-model-repo" />
      </Form.Item>
      <Form.Item name="file" label="Model file" rules={[{ required: true }]}> 
        <Input placeholder="model.Q4_K_M.gguf" />
      </Form.Item>
      <Form.Item
        name="contextSize"
        label="Context size"
        rules={[{ required: true, type: 'number', min: 1024, max: 16384 }]}
      >
        <InputNumber style={{ width: '100%' }} />
      </Form.Item>
      <Form.Item name="note" label="Note">
        <Input.TextArea rows={3} />
      </Form.Item>
      <Space>
        <Button type="primary" onClick={handleFinish} data-testid="submit-switch">
          Avvia switch
        </Button>
        <Button onClick={() => form.resetFields()} data-testid="reset-switch">
          Reset campi
        </Button>
        <Button onClick={handlePreset} data-testid="save-preset">
          Salva preset
        </Button>
      </Space>
      <div style={{ marginTop: 16, fontSize: 12 }}>
        Il token non viene salvato.
      </div>
    </Form>
  );
}
