import { useEffect, useState } from 'react';
import { Button } from 'antd';
import HealthBadge from '../components/HealthBadge';

type HealthResponse = { status?: string; reasons?: string[] };

export default function HealthPage() {
  const [ready, setReady] = useState<HealthResponse | null>(null);
  const [live, setLive] = useState<HealthResponse | null>(null);

  const load = async () => {
    const api = (import.meta.env.VITE_API_BASE_URL || '').replace(/\/$/, '');
    const base = api.replace(/\/api\/v1$/, '');
    try {
      const res = await fetch(`${base}/health/ready`);
      setReady(await res.json());
    } catch {
      setReady(null);
    }
    try {
      const res = await fetch(`${base}/health/live`);
      setLive(await res.json());
    } catch {
      setLive(null);
    }
  };

  useEffect(() => {
    load();
  }, []);

  const readyReasons = ready?.reasons ?? [];
  const liveReasons = live?.reasons ?? [];

  return (
    <div>
      <h2>Health</h2>
      <HealthBadge />
      <Button onClick={load}>Retry</Button>
      <h3>Ready: {ready?.status}</h3>
      <ul>{readyReasons.map((r) => (<li key={r}>{r}</li>))}</ul>
      <h3>Live: {live?.status}</h3>
      <ul>{liveReasons.map((r) => (<li key={r}>{r}</li>))}</ul>
    </div>
  );
}
