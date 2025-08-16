import { Button, Form, Input, Space } from 'antd';
import type { DownloadModelRequest } from '../generated';

type Props = {
  onSubmit: (req: DownloadModelRequest) => Promise<void>;
  disabled?: boolean;
};

export default function ModelDownloadForm({ onSubmit, disabled }: Props) {
  const [form] = Form.useForm();
  const isMobile =
    typeof window !== 'undefined' &&
    typeof window.matchMedia === 'function' &&
    window.matchMedia('(max-width: 575px)').matches;
  const inputWidth = isMobile ? '100%' : 200;

  const handleFinish = async () => {
    try {
      const values = await form.validateFields();
      const req: DownloadModelRequest = {
        hfKey: values.token,
        modelRepo: values.repo.trim(),
        modelFile: values.file.trim(),
      };
      await onSubmit(req);
      form.resetFields(['token']);
    } catch (e) {
      // validation error
    }
  };

  

  return (
    <Form layout="vertical" form={form} disabled={disabled}>
      <Form.Item
        name="token"
        label="HF token"
        rules={[{ required: true }]}
        style={{ width: inputWidth }}
        data-testid="token-input-wrapper"
      >
        <Input.Password autoComplete="new-password" style={{ width: '100%' }} />
      </Form.Item>
      <Form.Item name="repo" label="HF repo" rules={[{ required: true }]}> 
        <Input placeholder="TheOrg/the-model-repo" />
      </Form.Item>
      <Form.Item name="file" label="Model file" rules={[{ required: true }]}>
        <Input placeholder="model.Q4_K_M.gguf" />
      </Form.Item>
      <Form.Item name="note" label="Note">
        <Input.TextArea rows={3} />
      </Form.Item>
      <Space
        direction={isMobile ? 'vertical' : 'horizontal'}
        style={{ width: '100%' }}
      >
        <Button
          type="primary"
          onClick={handleFinish}
          data-testid="submit-download"
          block={isMobile}
        >
          Download
        </Button>
        <Button
          onClick={() => form.resetFields()}
          data-testid="reset-download"
          block={isMobile}
        >
          Reset fields
        </Button>
      </Space>
      <div style={{ marginTop: 16, fontSize: 12 }}>
        The token is not saved.
      </div>
    </Form>
  );
}
