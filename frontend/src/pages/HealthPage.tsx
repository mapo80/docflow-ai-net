import { useEffect, useState } from 'react';
import { Button, Card, Col, Row, Space, Typography, List } from 'antd';
import { CheckCircleOutlined, CloseCircleOutlined, ExclamationCircleOutlined, LoadingOutlined } from '@ant-design/icons';
import HealthBadge from '../components/HealthBadge';

type HealthResponse = { status?: string; reasons?: string[] };
type Status = 'ok' | 'unhealthy' | 'backpressure' | 'loading';

function statusIcon(status: string | undefined, prefix: string) {
  const s: Status = status
    ? ((status === 'healthy' ? 'ok' : (status as Status)))
    : 'loading';
  const props = { 'aria-label': `${prefix}-${s}` } as const;
  switch (s) {
    case 'ok':
      return <CheckCircleOutlined {...props} style={{ color: 'green' }} />;
    case 'unhealthy':
      return <CloseCircleOutlined {...props} style={{ color: 'red' }} />;
    case 'backpressure':
      return <ExclamationCircleOutlined {...props} style={{ color: 'orange' }} />;
    default:
      return <LoadingOutlined {...props} />;
  }
}

function StatusCard({ label, data, prefix }: { label: string; data: HealthResponse | null; prefix: string }) {
  const reasons = data?.reasons ?? [];
  return (
    <Card>
      <Space direction="vertical" size="small">
        <Space align="center">
          {statusIcon(data?.status, prefix)}
          <Typography.Text strong>{label}:</Typography.Text>
          <Typography.Text>{data?.status === 'healthy' ? 'ok' : data?.status ?? 'unknown'}</Typography.Text>
        </Space>
        {reasons.length > 0 && (
          <List size="small" dataSource={reasons} renderItem={(item) => <List.Item>{item}</List.Item>} />
        )}
      </Space>
    </Card>
  );
}

export default function HealthPage() {
  const [ready, setReady] = useState<HealthResponse | null>(null);
  const [live, setLive] = useState<HealthResponse | null>(null);
  const [loading, setLoading] = useState(false);

  const load = async () => {
    setLoading(true);
    setReady(null);
    setLive(null);
    const api = (import.meta.env.VITE_API_BASE_URL || '').replace(/\/$/, '');
    const base = api.replace(/\/api\/v1$/, '');
    try {
      const res = await fetch(`${base}/health/ready`);
      setReady(await res.json());
    } catch {
      setReady({ reasons: ['fetch_failed'] });
    }
    try {
      const res = await fetch(`${base}/health/live`);
      setLive(await res.json());
    } catch {
      setLive({ reasons: ['fetch_failed'] });
    }
    setLoading(false);
  };

  useEffect(() => {
    load();
  }, []);

  return (
    <Space direction="vertical" style={{ width: '100%' }} size="large">
      <Typography.Title level={2}>Health</Typography.Title>
      <HealthBadge />
      <Button onClick={load} loading={loading}>
        Retry
      </Button>
      <Row gutter={[16, 16]}>
        <Col xs={24} md={12}>
          <StatusCard label="Ready" data={ready} prefix="ready" />
        </Col>
        <Col xs={24} md={12}>
          <StatusCard label="Live" data={live} prefix="live" />
        </Col>
      </Row>
    </Space>
  );
}
