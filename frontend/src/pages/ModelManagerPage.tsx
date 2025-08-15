import { useEffect, useState } from 'react';
import {
  Row,
  Col,
  Card,
  Descriptions,
  Alert,
  message,
  Grid,
  Progress,
  Result,
  Space,
} from 'antd';
import ModelDownloadForm from '../components/ModelDownloadForm';
import RetryAfterBanner from '../components/RetryAfterBanner';
import ModelSwitchSelect from '../components/ModelSwitchSelect';
import {
  ModelService,
  type DownloadModelRequest,
  ApiError,
  type ModelInfo,
  type ModelDownloadStatus,
} from '../generated';
import dayjs from 'dayjs';

type ModelStatus = ModelDownloadStatus & { message?: string };

export default function ModelManagerPage() {
  const [info, setInfo] = useState<ModelInfo | null>(null);
  const [retryAfter, setRetryAfter] = useState<number | null>(null);
  const [available, setAvailable] = useState<string[]>([]);
  const [polling, setPolling] = useState(false);
  const [status, setStatus] = useState<ModelStatus | null>(null);
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
      message.success('Model activated');
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
      message.success('Download started');
      setStatus(null);
      setPolling(true);
    } catch (e) {
      if (e instanceof ApiError) {
        if (e.status === 429) setRetryAfter(e.body?.retry_after_seconds ?? 0);
        else message.error(e.body?.message || e.body?.errorCode);
      }
    }
  };

  const fetchStatus = async () => {
    try {
      const s = (await ModelService.modelStatus()) as ModelStatus;
      setStatus(s);
      if (s.completed) {
        setPolling(false);
        message.success('Download completed');
        await loadAvailable();
      }
    } catch (e) {
      if (e instanceof ApiError && e.status === 429) {
        setRetryAfter(e.body?.retry_after_seconds ?? 0);
      } else {
        message.error('Status error');
        setPolling(false);
      }
    }
  };

  useEffect(() => {
    if (!polling) return;
    fetchStatus();
    const id = setInterval(fetchStatus, 2000);
    return () => clearInterval(id);
  }, [polling]);

  return (
    <Row gutter={[16, 16]}>
      <Col span={24}>
        {retryAfter && (
          <RetryAfterBanner seconds={retryAfter} onFinish={() => setRetryAfter(null)} />
        )}
        <Card
          title="Current model"
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
            <Alert type="info" message="No information available" />
          )}
        </Card>
        <Card
          title="Switch model"
          style={{ marginBottom: 16 }}
          size={screens.xs ? 'small' : undefined}
        >
          <ModelSwitchSelect
            models={available}
            onSwitch={handleSwitch}
            onReload={loadAvailable}
            disabled={retryAfter !== null}
          />
        </Card>
        <Card
          title="Download model"
          style={{ marginBottom: 16 }}
          size={screens.xs ? 'small' : undefined}
        >
          <ModelDownloadForm
            onSubmit={handleDownload}
            disabled={retryAfter !== null || polling}
          />
          {polling && status && !status.completed && (
            <Space direction="vertical" style={{ width: '100%', marginTop: 16 }}>
              <Progress percent={status.percentage} />
              {status.message}
            </Space>
          )}
          {!polling && status && status.completed && (
            <Result status="success" title="Completed" />
          )}
        </Card>
      </Col>
    </Row>
  );
}
