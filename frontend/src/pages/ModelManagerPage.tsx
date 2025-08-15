import { useEffect, useState } from 'react';
import { Row, Col, Card, Descriptions, Alert, message, Grid } from 'antd';
import ModelDownloadForm from '../components/ModelDownloadForm';
import StatusCard from '../components/StatusCard';
import RetryAfterBanner from '../components/RetryAfterBanner';
import ModelSwitchSelect from '../components/ModelSwitchSelect';
import {
  ModelService,
  type DownloadModelRequest,
  ApiError,
  type ModelInfo,
} from '../generated';
import dayjs from 'dayjs';

export default function ModelManagerPage() {
  const [info, setInfo] = useState<ModelInfo | null>(null);
  const [polling, setPolling] = useState(false);
  const [retryAfter, setRetryAfter] = useState<number | null>(null);
  const [available, setAvailable] = useState<string[]>([]);
  const screens = Grid.useBreakpoint();

  const loadInfo = async () => {
    try {
      const data = await ModelService.modelCurrent();
      setInfo(data);
    } catch (e) {
      if (e instanceof ApiError && e.status === 429) {
        setRetryAfter(e.body?.retry_after_seconds ?? 0);
      }
    }
  };

  const loadAvailable = async () => {
    try {
      const data = await ModelService.modelAvailable();
      setAvailable(data);
    } catch {
      /* ignore */
    }
  };

  useEffect(() => {
    loadInfo();
    loadAvailable();
  }, []);

  const handleSwitch = async (file: string, ctx: number) => {
    try {
      await ModelService.modelSwitch({ requestBody: { modelFile: file, contextSize: ctx } });
      message.success('Modello attivato');
      await loadInfo();
    } catch (e) {
      if (e instanceof ApiError) {
        if (e.status === 429) setRetryAfter(e.body?.retry_after_seconds ?? 0);
        else message.error(e.body?.message || e.body?.errorCode);
      }
    }
  };

  const handleDownload = async (req: DownloadModelRequest) => {
    try {
      await ModelService.modelDownload({ requestBody: req });
      message.success('Download avviato');
      setPolling(true);
    } catch (e) {
      if (e instanceof ApiError) {
        if (e.status === 429) setRetryAfter(e.body?.retry_after_seconds ?? 0);
        else message.error(e.body?.message || e.body?.errorCode);
      }
    }
  };

  return (
    <Row gutter={[16, 16]}>
      <Col span={24}>
        {retryAfter && (
          <RetryAfterBanner seconds={retryAfter} onFinish={() => setRetryAfter(null)} />
        )}
        <Card
          title="Modello corrente"
          style={{ marginBottom: 16 }}
          size={screens.xs ? 'small' : undefined}
        >
          {info ? (
            <Descriptions size="small" column={1}>
              {info.name && <Descriptions.Item label="Name">{info.name}</Descriptions.Item>}
              {info.repo && <Descriptions.Item label="Repo">{info.repo}</Descriptions.Item>}
              {info.file && <Descriptions.Item label="File">{info.file}</Descriptions.Item>}
              {info.contextSize && (
                <Descriptions.Item label="Context">{info.contextSize}</Descriptions.Item>
              )}
              {info.loadedAt && (
                <Descriptions.Item label="Loaded">
                  {dayjs(info.loadedAt).format('YYYY-MM-DD HH:mm')}
                </Descriptions.Item>
              )}
            </Descriptions>
          ) : (
            <Alert type="info" message="Nessuna informazione disponibile" />
          )}
        </Card>
        <Card
          title="Cambia modello"
          style={{ marginBottom: 16 }}
          size={screens.xs ? 'small' : undefined}
        >
          <ModelSwitchSelect
            models={available}
            onSwitch={handleSwitch}
            disabled={retryAfter !== null}
          />
        </Card>
        <Card
          title="Scarica modello"
          style={{ marginBottom: 16 }}
          size={screens.xs ? 'small' : undefined}
        >
          <ModelDownloadForm onSubmit={handleDownload} disabled={retryAfter !== null} />
        </Card>
        <StatusCard active={polling} />
      </Col>
    </Row>
  );
}
