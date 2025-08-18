import React, { useState, useEffect } from 'react';
import { Modal, Form, Input, Select, Checkbox, Button, Space, message } from 'antd';
import type { AddModelRequest, ModelType, HostedProvider, ModelDto } from '@/services/modelsApi';
import api from '@/services/modelsApi';

interface Props {
  open: boolean;
  model?: ModelDto | null;
  onCancel: () => void;
  onSubmit: (req: AddModelRequest) => Promise<void>;
}

const typeOptions = [
  { value: 'hosted-llm' as ModelType, label: <span data-testid="type-hosted">Hosted LLM</span> },
  { value: 'local' as ModelType, label: <span data-testid="type-local">Local (Hugging Face)</span> },
];

const providerOptions = [
  { value: 'openai' as HostedProvider, label: <span data-testid="provider-openai">OpenAI</span> },
  { value: 'azure-openai' as HostedProvider, label: <span data-testid="provider-azure">Azure OpenAI</span> },
];

const ModelModal: React.FC<Props> = ({ open, model, onCancel, onSubmit }) => {
  const [form] = Form.useForm<AddModelRequest>();
  const [testing, setTesting] = useState(false);

  useEffect(() => {
    if (open) {
      if (model) {
        form.setFieldsValue({
          name: model.name,
          type: model.type,
          provider: model.provider ?? undefined,
          baseUrl: model.baseUrl ?? undefined,
          hfRepo: model.hfRepo ?? undefined,
          modelFile: model.modelFile ?? undefined,
          downloadNow: false,
        });
      } else {
        form.resetFields();
      }
    }
  }, [open, model, form]);

  const handleOk = async () => {
    try {
      const values = await form.validateFields();
      await onSubmit(values);
      form.resetFields();
      onCancel();
    } catch {
      // validation errors are handled by antd
    }
  };

  const testConnection = async () => {
    try {
      setTesting(true);
      const provider = form.getFieldValue('provider');
      const baseUrl = form.getFieldValue('baseUrl');
      const apiKey = form.getFieldValue('apiKey');
      await api.testConnection(provider, baseUrl, apiKey);
      message.success('Connection ok');
    } catch (e: any) {
      message.error(e.message || 'Connection failed');
    } finally {
      setTesting(false);
    }
  };

  return (
    <Modal
      open={open}
      onCancel={onCancel}
      onOk={handleOk}
      title="Model"
      footer={null}
      maskClosable={false}
      style={{ top: 0 }}
      width="100%"
      bodyStyle={{ height: "100vh" }}
    >
      <Form form={form} layout="vertical" onFinish={handleOk}>
        <Form.Item name="name" label="Name" rules={[{ required: true }]}>
          <Input data-testid="name" />
        </Form.Item>
        <Form.Item name="type" label="Type" rules={[{ required: true }]}>
          <Select options={typeOptions} data-testid="type" />
        </Form.Item>
        <Form.Item shouldUpdate noStyle>
          {({ getFieldValue }) => {
            const t = getFieldValue('type');
            if (t === 'hosted-llm') {
              return (
                <>
                  <Form.Item name="provider" label="Provider" rules={[{ required: true }]}>
                    <Select options={providerOptions} data-testid="provider" />
                  </Form.Item>
                  <Form.Item name="baseUrl" label="Base URL" rules={[{ required: true }]}>
                    <Input data-testid="baseUrl" />
                  </Form.Item>
                  <Form.Item name="apiKey" label="API Key" rules={[{ required: true }]}>
                    <Input.Password data-testid="apiKey" />
                  </Form.Item>
                  <Button onClick={testConnection} loading={testing}>Test Connection</Button>
                </>
              );
            }
            if (t === 'local') {
              return (
                <>
                  <Form.Item name="hfToken" label="HF Token" rules={[{ required: true }]}>
                    <Input.Password data-testid="hfToken" />
                  </Form.Item>
                  <Form.Item name="hfRepo" label="HF Repo" rules={[{ required: true }]}>
                    <Input data-testid="hfRepo" />
                  </Form.Item>
                  <Form.Item name="modelFile" label="Model File" rules={[{ required: true }]}>
                    <Input data-testid="modelFile" />
                  </Form.Item>
                  <Form.Item name="downloadNow" valuePropName="checked"> 
                    <Checkbox>Download immediately after save</Checkbox>
                  </Form.Item>
                </>
              );
            }
            return null;
          }}
        </Form.Item>
        <Form.Item>
          <Space>
            <Button onClick={onCancel}>Cancel</Button>
            <Button type="primary" htmlType="submit">Save</Button>
          </Space>
        </Form.Item>
      </Form>
    </Modal>
  );
};

export default ModelModal;
