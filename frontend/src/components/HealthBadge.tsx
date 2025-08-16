import { useEffect, useState } from 'react';
import { Badge, Tooltip, Space } from 'antd';
import CheckCircleOutlined from '@ant-design/icons/CheckCircleOutlined';
import CloseCircleOutlined from '@ant-design/icons/CloseCircleOutlined';
import ExclamationCircleOutlined from '@ant-design/icons/ExclamationCircleOutlined';
import LoadingOutlined from '@ant-design/icons/LoadingOutlined';

type HealthResponse = { status?: string; reasons?: string[] };

type Status = 'loading' | 'ok' | 'unhealthy' | 'backpressure';

const colorMap: Record<Status, string> = {
  loading: '#d9d9d9',
  ok: 'green',
  unhealthy: 'red',
  backpressure: 'orange',
};

function statusIcon(status: Status) {
  const props = { 'aria-label': `health-${status}` } as const;
  switch (status) {
    case 'ok':
      return <CheckCircleOutlined {...props} style={{ color: 'green' }} />;
    case 'unhealthy':
      return <CloseCircleOutlined {...props} style={{ color: 'red' }} />;
    case 'backpressure':
      return <ExclamationCircleOutlined {...props} style={{ color: 'orange' }} />;
    default:
      return <LoadingOutlined {...props} style={{ color: '#d9d9d9' }} />;
  }
}

export default function HealthBadge() {
  const [health, setHealth] = useState<HealthResponse | null>(null);
  const status: Status = health
    ? ((health.status === 'healthy' ? 'ok' : (health.status as Status)))
    : 'loading';

  useEffect(() => {
    let cancelled = false;
    const load = async () => {
      try {
        const api = (import.meta.env.VITE_API_BASE_URL || '').replace(/\/$/, '');
        const base = api.replace(/\/api\/v1$/, '');
        const res = await fetch(`${base}/health/ready`);
        const data = (await res.json()) as HealthResponse;
        if (!cancelled) setHealth(data);
      } catch {
        if (!cancelled) setHealth({ status: 'unhealthy', reasons: ['fetch_failed'] });
      }
    };
    load();
    const id = setInterval(load, 30000);
    return () => {
      cancelled = true;
      clearInterval(id);
    };
  }, []);

  return (
    <Tooltip title={health?.reasons?.join(', ') || ''}>
      <Badge
        color={colorMap[status]}
        text={
          <Space align="center" size="small">
            {statusIcon(status)}
            Health
          </Space>
        }
      />
    </Tooltip>
  );
}
