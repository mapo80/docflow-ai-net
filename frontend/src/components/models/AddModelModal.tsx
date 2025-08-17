import React from 'react';
import { Modal, Form, Input, Select } from 'antd';
import type { AddModelRequest, ModelSourceType } from '@/services/modelsApi';

interface Props {
  open: boolean;
  onCancel: () => void;
  onSubmit: (req: AddModelRequest) => Promise<void>;
}

const AddModelModal: React.FC<Props> = ({ open, onCancel, onSubmit }) => {
  const [form] = Form.useForm<AddModelRequest>();

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

  const sourceOptions = [
    { value: 'Url' as ModelSourceType, label: 'URL' },
    { value: 'Local' as ModelSourceType, label: 'Local' },
  ];

  return (
    <Modal open={open} onCancel={onCancel} onOk={handleOk} title="Add GGUF Model">
      <Form form={form} layout="vertical">
        <Form.Item name="name" label="Name" rules={[{ required: true }]}> 
          <Input />
        </Form.Item>
        <Form.Item name="sourceType" label="Source Type" rules={[{ required: true }]}> 
          <Select options={sourceOptions} />
        </Form.Item>
        <Form.Item shouldUpdate noStyle>
          {({ getFieldValue }) =>
            getFieldValue('sourceType') === 'Url' ? (
              <Form.Item name="url" label="URL" rules={[{ required: true }]}> 
                <Input />
              </Form.Item>
            ) : getFieldValue('sourceType') === 'Local' ? (
              <Form.Item name="localPath" label="Path" rules={[{ required: true }]}> 
                <Input />
              </Form.Item>
            ) : null
          }
        </Form.Item>
      </Form>
    </Modal>
  );
};

export default AddModelModal;
