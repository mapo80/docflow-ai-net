import { Button, Form, Input, Space, Grid } from 'antd';
import type { DownloadModelRequest } from '../generated';

type Props = {
  onSubmit: (req: DownloadModelRequest) => Promise<void>;
  disabled?: boolean;
};

export default function ModelDownloadForm({ onSubmit, disabled }: Props) {
  const [form] = Form.useForm();
  const screens = Grid.useBreakpoint();

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
      <Form.Item name="token" label="HF token" rules={[{ required: true }]}> 
        <Input.Password autoComplete="new-password" />
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
        direction={screens.xs ? 'vertical' : 'horizontal'}
        style={{ width: '100%' }}
      >
        <Button
          type="primary"
          onClick={handleFinish}
          data-testid="submit-download"
          block={screens.xs}
        >
          Download
        </Button>
        <Button
          onClick={() => form.resetFields()}
          data-testid="reset-download"
          block={screens.xs}
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
