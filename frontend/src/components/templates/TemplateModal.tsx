import React, { useState } from "react";
import { Modal, Form, Input } from "antd";
import type { TemplateUpsertRequest } from "@/services/templatesApi";

type Props = {
  open: boolean;
  initial?: TemplateUpsertRequest & { id?: string };
  onCancel: () => void;
  onSubmit: (data: TemplateUpsertRequest) => Promise<void>;
  title?: string;
};

const TemplateModal: React.FC<Props> = ({ open, onCancel, onSubmit, initial, title }) => {
  const [form] = Form.useForm<TemplateUpsertRequest>();
  const [loading, setLoading] = useState(false);

  const ok = async () => {
    const v = await form.validateFields();
    setLoading(true);
    try {
      await onSubmit(v);
      onCancel();
      form.resetFields();
    } finally {
      setLoading(false);
    }
  };

  return (
    <Modal open={open} onCancel={onCancel} onOk={ok} confirmLoading={loading} title={title ?? "Template"} okText="Save" destroyOnClose>
      <Form layout="vertical" form={form} initialValues={initial ?? { documentType: "generic", language: "auto", fieldsJson: "[]" }}>
        <Form.Item name="name" label="Name" rules={[{ required: true }]}>
          <Input />
        </Form.Item>
        <Form.Item name="documentType" label="Document Type">
          <Input placeholder="generic / invoice / ..." />
        </Form.Item>
        <Form.Item name="language" label="Language">
          <Input placeholder="auto / it / en ..." />
        </Form.Item>
        <Form.Item name="fieldsJson" label="Fields (JSON array)">
          <Input.TextArea autoSize={{ minRows: 6 }} placeholder='[{"key":"total","description":"...","type":"number","required":true}]' />
        </Form.Item>
        <Form.Item name="notes" label="Notes">
          <Input.TextArea autoSize />
        </Form.Item>
      </Form>
    </Modal>
  );
};

export default TemplateModal;
