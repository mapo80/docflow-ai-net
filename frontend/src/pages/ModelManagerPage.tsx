import { useEffect, useState } from 'react';
import { Row, Col, Card, Descriptions, Alert, message } from 'antd';
import ModelSwitchForm from '../components/ModelSwitchForm';
import StatusCard from '../components/StatusCard';
import PresetList from '../components/PresetList';
import RetryAfterBanner from '../components/RetryAfterBanner';
import { DefaultService, type ModelInfo, type ModelSwitchRequest } from '../generated';
import { HttpError } from '../api/fetcher';
import dayjs from 'dayjs';

export default function ModelManagerPage() {
  const [info, setInfo] = useState<ModelInfo | null>(null);
  const [loadingInfo, setLoadingInfo] = useState(false);
  const [polling, setPolling] = useState(false);
  const [retryAfter, setRetryAfter] = useState<number | null>(null);

  const loadInfo = async () => {
    setLoadingInfo(true);
    try {
      const data = await DefaultService.getModelInfo();
      setInfo(data);
    } catch (e) {
      if (e instanceof HttpError && e.status === 429) {
        setRetryAfter(e.retryAfter ?? 0);
      }
    } finally {
      setLoadingInfo(false);
    }
  };

  useEffect(() => {
    loadInfo();
  }, []);

  const handleSwitch = async (req: ModelSwitchRequest) => {
    try {
      await DefaultService.switchModel({ requestBody: req });
      message.success('Switch avviato');
      setPolling(true);
    } catch (e) {
      if (e instanceof HttpError) {
        if (e.status === 429) setRetryAfter(e.retryAfter ?? 0);
        else message.error(e.data.message || e.data.errorCode);
      }
    }
  };

  const applyPreset = (p: any) => {
    localStorage.setItem('last-model', JSON.stringify(p));
    setTimeout(() => window.location.reload(), 0);
  };

  return (
    <Row gutter={16}>
      <Col span={16}>
        {retryAfter && (
          <RetryAfterBanner seconds={retryAfter} onFinish={() => setRetryAfter(null)} />
        )}
        <Card title="Modello corrente" style={{ marginBottom: 16 }}>
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
        <Card title="Cambia modello" style={{ marginBottom: 16 }}>
          <ModelSwitchForm onSubmit={handleSwitch} disabled={retryAfter !== null} />
        </Card>
        <StatusCard active={polling} />
      </Col>
      <Col span={8}>
        <PresetList onApply={applyPreset} />
      </Col>
    </Row>
  );
}
