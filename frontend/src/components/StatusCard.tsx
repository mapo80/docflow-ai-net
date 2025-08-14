import { useEffect, useState } from 'react';
import { Card, Progress, Result, Space, Button } from 'antd';
import { ModelService, type ModelDownloadStatus, ApiError } from '../generated';
import RetryAfterBanner from './RetryAfterBanner';

type ModelStatus = ModelDownloadStatus & { message?: string };

type Props = {
  active: boolean;
};

export default function StatusCard({ active }: Props) {
  const [status, setStatus] = useState<ModelStatus | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [retryAfter, setRetryAfter] = useState<number | null>(null);

  const fetchStatus = async () => {
    try {
      const s = await ModelService.modelStatus();
      setStatus(s);
      setError(null);
    } catch (e) {
      if (e instanceof ApiError && e.status === 429) {
        setRetryAfter(e.body?.retry_after_seconds ?? 0);
      } else {
        setError('Errore');
      }
    }
  };

  useEffect(() => {
    if (!active) return;
    fetchStatus();
    const id = setInterval(fetchStatus, 2000);
    return () => clearInterval(id);
  }, [active]);

  if (retryAfter) {
    return <RetryAfterBanner seconds={retryAfter} onFinish={() => setRetryAfter(null)} />;
  }

  return (
    <Card title="Stato caricamento" extra={<Button onClick={fetchStatus}>Aggiorna stato</Button>}>
      {error && <Result status="error" title={error} />}
      {!error && status && !status.completed && (
        <Space direction="vertical" style={{ width: '100%' }}>
          <Progress percent={status.percentage} />
          {status.message}
        </Space>
      )}
      {!error && status && status.completed && (
        <Result status="success" title="Completato" />
      )}
    </Card>
  );
}
