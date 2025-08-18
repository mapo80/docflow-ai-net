import { useEffect, useState } from 'react';
import { Modal, Form, Input, Select, Button, Space, message } from 'antd';
import { ModelsService, type CreateModelRequest, ApiError } from '../generated';
import { useApiError } from './ApiErrorProvider';

interface ModelModalProps {
  open: boolean;
  onCancel: () => void;
  onCreated: () => void;
  existingNames: string[];
}

export default function ModelModal({
  open,
  onCancel,
  onCreated,
  existingNames,
}: ModelModalProps) {
  const [form] = Form.useForm();
  const [saving, setSaving] = useState(false);
  const type = Form.useWatch('type', form) || 'hosted-llm';
  const { showError } = useApiError();

  useEffect(() => {
    if (open) {
      form.resetFields();
    }
  }, [open, form]);

  const handleSave = async () => {
    try {
      const values = await form.validateFields();
      setSaving(true);
      const req: CreateModelRequest = {
        name: values.name,
        type: values.type,
        provider: values.provider,
        baseUrl: values.baseUrl,
        apiKey: values.apiKey,
        hfRepo: values.hfRepo,
        modelFile: values.modelFile,
        hfToken: values.hfToken,
      };
      await ModelsService.modelsCreate({ requestBody: req });
      message.success('Model created');
      onCreated();
      onCancel();
    } catch (e) {
      if (!(e instanceof ApiError) && e instanceof Error) showError(e.message);
    } finally {
      setSaving(false);
    }
  };

  return (
    <Modal
      open={open}
      title="Create Model"
      onCancel={() => !saving && onCancel()}
      maskClosable={false}
      style={{ top: 0 }}
      width="100%"
      bodyStyle={{ height: 'calc(100vh - 108px)', overflowY: 'auto' }}
      footer={
        <Space>
          <Button onClick={onCancel} disabled={saving}>
            Cancel
          </Button>
          <Button type="primary" loading={saving} onClick={handleSave} disabled={saving}>
            Save
          </Button>
        </Space>
      }
    >
      <Form form={form} layout="vertical">
        <Form.Item
          name="name"
          label="Name"
          rules={[
            { required: true, message: 'Name is required' },
            {
              validator: (_, v) => {
                if (!v) return Promise.resolve();
                return existingNames.includes(v)
                  ? Promise.reject(new Error('Name exists'))
                  : Promise.resolve();
              },
            },
          ]}
        >
          <Input />
        </Form.Item>
        <Form.Item name="type" label="Type" initialValue="hosted-llm" rules={[{ required: true }]}>
          <Select options={[{ value: 'hosted-llm', label: 'Hosted LLM' }, { value: 'local', label: 'Local' }]} />
        </Form.Item>
        {type === 'hosted-llm' && (
          <>
            <Form.Item name="provider" label="Provider" rules={[{ required: true }]}>
              <Select options={[{ value: 'openai', label: 'openai' }, { value: 'azure-openai', label: 'azure-openai' }]} />
            </Form.Item>
            <Form.Item name="baseUrl" label="Base URL" rules={[{ required: true, type: 'url' }]}> 
              <Input />
            </Form.Item>
            <Form.Item name="apiKey" label="API Key" rules={[{ required: true }]}> 
              <Input.Password />
            </Form.Item>
          </>
        )}
        {type === 'local' && (
          <>
            <Form.Item name="hfToken" label="HF Token" rules={[{ required: true }]}> 
              <Input.Password />
            </Form.Item>
            <Form.Item name="hfRepo" label="HF Repo" rules={[{ required: true }]}> 
              <Input />
            </Form.Item>
            <Form.Item name="modelFile" label="Model File" rules={[{ required: true }]}> 
              <Input />
            </Form.Item>
          </>
        )}
      </Form>
    </Modal>
  );
}

