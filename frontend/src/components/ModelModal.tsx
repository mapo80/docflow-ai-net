import { useEffect, useState } from 'react';
import { Modal, Form, Input, Select, Button, Space, message } from 'antd';
import { ModelsService, type CreateModelRequest, type ModelDto, ApiError } from '../generated';
import { useApiError } from './ApiErrorProvider';

interface ModelModalProps {
  open: boolean;
  modelId?: string;
  onCancel: () => void;
  onSaved: (created: boolean) => void;
  existingNames: string[];
}

export default function ModelModal({
  open,
  modelId,
  onCancel,
  onSaved,
  existingNames,
}: ModelModalProps) {
  const [form] = Form.useForm();
  const [saving, setSaving] = useState(false);
  const [initial, setInitial] = useState<ModelDto | null>(null);
  const [updateKey, setUpdateKey] = useState(false);
  const type = Form.useWatch('type', form) || initial?.type || 'hosted-llm';
  const { showError } = useApiError();
  const editing = !!modelId;

  useEffect(() => {
    if (open && modelId) {
      ModelsService.modelsGet({ id: modelId })
        .then((m) => {
          setInitial(m);
          form.setFieldsValue({
            name: m.name,
            type: m.type,
            provider: m.provider,
            baseUrl: m.baseUrl,
          });
        })
        .catch(() => {});
    } else if (open) {
      form.resetFields();
      setInitial(null);
    }
  }, [open, modelId, form]);

  const handleSave = async () => {
    try {
      const values = await form.validateFields();
      setSaving(true);
      if (editing) {
        await ModelsService.modelsUpdate({
          id: modelId!,
          requestBody: {
            name: values.name,
            provider: values.provider,
            baseUrl: values.baseUrl,
            apiKey: updateKey ? values.apiKey : undefined,
          },
        });
        message.success('Model updated');
        onSaved(false);
      } else {
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
        message.success('Model created successfully.');
        onSaved(true);
      }
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
      title={editing ? 'Edit Model' : 'Create Model'}
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
                const names = editing
                  ? existingNames.filter((n) => n !== initial?.name)
                  : existingNames;
                return names.includes(v)
                  ? Promise.reject(new Error('Name exists'))
                  : Promise.resolve();
              },
            },
          ]}
        >
          <Input />
        </Form.Item>
        <Form.Item name="type" label="Type" initialValue="hosted-llm" rules={[{ required: true }]}> 
          <Select
            options={[{ value: 'hosted-llm', label: 'Hosted LLM' }, { value: 'local', label: 'Local' }]}
            disabled={editing}
          />
        </Form.Item>
        {type === 'hosted-llm' && (
          <>
            <Form.Item name="provider" label="Provider" rules={[{ required: true }]}>
              <Select options={[{ value: 'openai', label: 'openai' }, { value: 'azure-openai', label: 'azure-openai' }]} />
            </Form.Item>
            <Form.Item name="baseUrl" label="Base URL" rules={[{ required: true, type: 'url' }]}>
              <Input />
            </Form.Item>
            {editing && initial?.hasApiKey && (
              <Form.Item>
                <Space>
                  <Button type="link" onClick={() => setUpdateKey(!updateKey)} aria-label="Update API Key">
                    {updateKey ? 'Keep existing key' : 'Update API Key'}
                  </Button>
                </Space>
              </Form.Item>
            )}
            <Form.Item
              name="apiKey"
              label="API Key"
              rules={updateKey || !editing ? [{ required: true }] : []}
            >
              <Input.Password placeholder={editing ? '••••••' : undefined} disabled={editing && !updateKey} />
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

