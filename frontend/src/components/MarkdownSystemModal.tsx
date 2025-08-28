import { useEffect, useState } from 'react';
import { Modal, Form, Input, Select, Button, Space } from 'antd';
import { MarkdownSystemsService, type CreateMarkdownSystemRequest, type MarkdownSystemDto, ApiError } from '../generated';
import { useApiError } from './ApiErrorProvider';
import notify from './notification';

interface Props {
  open: boolean;
  systemId?: string;
  onCancel: () => void;
  onSaved: () => void;
  existingNames: string[];
}

export default function MarkdownSystemModal({ open, systemId, onCancel, onSaved, existingNames }: Props) {
  const [form] = Form.useForm();
  const [saving, setSaving] = useState(false);
  const [initial, setInitial] = useState<MarkdownSystemDto | null>(null);
  const [updateKey, setUpdateKey] = useState(false);
  const { showError } = useApiError();
  const editing = !!systemId;

  useEffect(() => {
    if (open && systemId) {
      MarkdownSystemsService.markdownSystemsGet({ id: systemId })
        .then((s) => {
          setInitial(s);
          form.setFieldsValue({
            name: s.name,
            provider: s.provider,
            endpoint: s.endpoint,
          });
        })
        .catch(() => {});
    } else if (open) {
      form.resetFields();
      setInitial(null);
    }
  }, [open, systemId, form]);

  const handleSave = async () => {
    try {
      const values = await form.validateFields();
      setSaving(true);
      if (editing) {
        await MarkdownSystemsService.markdownSystemsUpdate({
          id: systemId!,
          requestBody: {
            name: values.name,
            provider: values.provider,
            endpoint: values.endpoint,
            apiKey: updateKey ? values.apiKey : undefined,
          },
        });
        notify('success', 'Markdown system updated');
      } else {
        const req: CreateMarkdownSystemRequest = {
          name: values.name,
          provider: values.provider,
          endpoint: values.endpoint,
          apiKey: values.apiKey,
        };
        await MarkdownSystemsService.markdownSystemsCreate({ requestBody: req });
        notify('success', 'Markdown system created successfully.');
      }
      onSaved();
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
      title={editing ? 'Edit Markdown System' : 'Create Markdown System'}
      onCancel={() => !saving && onCancel()}
      maskClosable={false}
      style={{ top: 0 }}
      width="100%"
      styles={{ body: { height: 'calc(100vh - 108px)', overflowY: 'auto' } }}
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
                const names = editing ? existingNames.filter((n) => n !== initial?.name) : existingNames;
                return names.includes(v) ? Promise.reject(new Error('Name exists')) : Promise.resolve();
              },
            },
          ]}
        >
          <Input />
        </Form.Item>
        <Form.Item name="provider" label="Provider" rules={[{ required: true }]}> 
          <Select
            options={[
              { value: 'docling', label: 'docling' },
              { value: 'azure-di', label: 'azure-di' },
            ]}
            disabled={editing}
          />
        </Form.Item>
        <Form.Item name="endpoint" label="Endpoint" rules={[{ required: true, type: 'url' }]}> 
          <Input />
        </Form.Item>
        {editing && initial?.hasApiKey && (
          <Form.Item>
            <Button type="link" onClick={() => setUpdateKey(!updateKey)} aria-label="Update API Key">
              {updateKey ? 'Keep existing key' : 'Update API Key'}
            </Button>
          </Form.Item>
        )}
        <Form.Item name="apiKey" label="API Key">
          <Input.Password placeholder={editing && !updateKey ? '••••••' : undefined} disabled={editing && !updateKey} />
        </Form.Item>
      </Form>
    </Modal>
  );
}
