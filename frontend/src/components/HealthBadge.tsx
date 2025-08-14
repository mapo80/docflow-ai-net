import { useEffect, useState } from 'react';
import { Badge, Tooltip } from 'antd';
import { DefaultService, type HealthResponse } from '../generated';

type Status = 'loading' | 'ok' | 'unhealthy' | 'backpressure';

const colorMap: Record<Status, string> = {
  loading: 'default',
  ok: 'green',
  unhealthy: 'red',
  backpressure: 'orange',
};

export default function HealthBadge() {
  const [health, setHealth] = useState<HealthResponse | null>(null);
  const status: Status = health ? (health.status as Status) : 'loading';

  useEffect(() => {
    let cancelled = false;
    const load = async () => {
      try {
        const res = await DefaultService.getHealth();
        if (!cancelled) setHealth(res);
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
      <Badge color={colorMap[status]} text="Health" />
    </Tooltip>
  );
}
