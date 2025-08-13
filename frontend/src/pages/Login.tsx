import { Form, Input, Button, Typography } from 'antd';

interface Props {
  onLogin: (apiKey: string) => void;
}

export default function Login({ onLogin }: Props) {
  const onFinish = (values: { apiKey: string }) => {
    onLogin(values.apiKey);
  };

  return (
    <div style={{ maxWidth: 300, margin: '100px auto' }}>
      <Typography.Title level={2}>Login</Typography.Title>
      <Form onFinish={onFinish} layout="vertical">
        <Form.Item
          label="API Key"
          name="apiKey"
          rules={[{ required: true, message: 'Please input API key' }]}
        >
          <Input.Password placeholder="API Key" />
        </Form.Item>
        <Form.Item>
          <Button type="primary" htmlType="submit" block>
            Save
          </Button>
        </Form.Item>
      </Form>
    </div>
  );
}
