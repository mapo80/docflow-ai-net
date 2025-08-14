import { useEffect, useState } from 'react';
import { Button } from 'antd';
import HealthBadge from '../components/HealthBadge';
import { DefaultService, type HealthResponse } from '../generated';
import { fetcher } from '../api/fetcher';

export default function HealthPage() {
  const [ready, setReady] = useState<HealthResponse | null>(null);
  const [live, setLive] = useState<HealthResponse | null>(null);

  const load = async () => {
    try {
      setReady(await DefaultService.getHealth());
    } catch {
      setReady(null);
    }
    try {
      const res = await fetcher('/health/live', { method: 'GET' });
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
      <Button onClick={load}>Riprova</Button>
      <h3>Ready: {ready?.status}</h3>
      <ul>{readyReasons.map((r) => (<li key={r}>{r}</li>))}</ul>
      <h3>Live: {live?.status}</h3>
      <ul>{liveReasons.map((r) => (<li key={r}>{r}</li>))}</ul>
    </div>
  );
}
