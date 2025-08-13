import { useEffect, useState } from 'react';
import { Button, Form, Input, InputNumber, Typography, Progress, message, Space } from 'antd';
import { getModelStatus, switchModel } from '../api';
import type { ModelStatus } from '../api';

interface Props {
  apiKey: string;
  onLogout: () => void;
}

export default function ModelManager({ apiKey, onLogout }: Props) {
  const [currentModel, setCurrentModel] = useState<string | null>(
    localStorage.getItem('currentModel')
  );
  const [status, setStatus] = useState<ModelStatus>({ completed: true, percentage: 100 });
  const [polling, setPolling] = useState(false);

  useEffect(() => {
    getModelStatus(apiKey)
      .then(setStatus)
      .catch(() => message.error('Unable to fetch status'));
  }, [apiKey]);

  useEffect(() => {
    if (!polling) return;
    const id = setInterval(async () => {
      try {
        const s = await getModelStatus(apiKey);
        setStatus(s);
        if (s.completed) {
          setPolling(false);
        }
      } catch (e) {
        message.error('Status check failed');
        setPolling(false);
      }
    }, 2000);
    return () => clearInterval(id);
  }, [polling, apiKey]);

  const onFinish = async (values: {
    hfToken: string;
    hfRepo: string;
    modelFile: string;
    contextSize: number;
  }) => {
    try {
      const res = await switchModel(apiKey, values);
      if (!res.ok) {
        const text = await res.text();
        message.error(text || 'Model switch failed');
        return;
      }
      message.success('Model download started');
      localStorage.setItem('currentModel', values.modelFile);
      setCurrentModel(values.modelFile);
      setPolling(true);
    } catch (e) {
      message.error((e as Error).message);
    }
  };

  return (
    <div style={{ maxWidth: 600, margin: '20px auto' }}>
      <Space direction="vertical" style={{ width: '100%' }}>
        <Typography.Title level={3}>Model Manager</Typography.Title>
        <Typography.Text>
          Current model: {currentModel ?? 'unknown'}
        </Typography.Text>
        <Form layout="vertical" onFinish={onFinish}>
          <Form.Item
            label="HF Token"
            name="hfToken"
            rules={[{ required: true, message: 'Required' }]}
          >
            <Input.Password />
          </Form.Item>
          <Form.Item
            label="HF Repo"
            name="hfRepo"
            rules={[{ required: true, message: 'Required' }]}
          >
            <Input />
          </Form.Item>
          <Form.Item
            label="GGUF Model Name"
            name="modelFile"
            rules={[{ required: true, message: 'Required' }]}
          >
            <Input />
          </Form.Item>
          <Form.Item
            label="Context Length"
            name="contextSize"
            initialValue={4096}
            rules={[{ required: true, type: 'number', min: 1 }]}
          >
            <InputNumber style={{ width: '100%' }} />
          </Form.Item>
          <Form.Item>
            <Space>
              <Button type="primary" htmlType="submit" loading={polling}>
                Switch Model
              </Button>
              <Button onClick={onLogout}>Logout</Button>
            </Space>
          </Form.Item>
        </Form>
        {polling || !status.completed ? (
          <Progress percent={Math.round(status.percentage)} />
        ) : null}
      </Space>
    </div>
  );
}
