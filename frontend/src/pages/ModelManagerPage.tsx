import { useEffect, useState } from 'react';
import { Row, Col, Card, Descriptions, Alert, message } from 'antd';
import ModelSwitchForm from '../components/ModelSwitchForm';
import StatusCard from '../components/StatusCard';
import PresetList from '../components/PresetList';
import RetryAfterBanner from '../components/RetryAfterBanner';
import { ModelService, type SwitchModelRequest, ApiError, OpenAPI } from '../generated';
import { request as __request } from '../generated/core/request';
import dayjs from 'dayjs';

type ModelInfo = {
  name?: string | null;
  repo?: string | null;
  file?: string | null;
  contextSize?: number | null;
  loadedAt?: string | null;
};

export default function ModelManagerPage() {
  const [info, setInfo] = useState<ModelInfo | null>(null);
  const [polling, setPolling] = useState(false);
  const [retryAfter, setRetryAfter] = useState<number | null>(null);

  const loadInfo = async () => {
    try {
      const data = await __request<ModelInfo>(OpenAPI, {
        method: 'GET',
        url: '/api/v1/model',
      });
      setInfo(data);
    } catch (e) {
      if (e instanceof ApiError && e.status === 429) {
        setRetryAfter(e.body?.retry_after_seconds ?? 0);
      }
    }
  };

  useEffect(() => {
    loadInfo();
  }, []);

  const handleSwitch = async (req: SwitchModelRequest) => {
    try {
      await ModelService.modelSwitch({ requestBody: req });
      message.success('Switch avviato');
      setPolling(true);
    } catch (e) {
      if (e instanceof ApiError) {
        if (e.status === 429) setRetryAfter(e.body?.retry_after_seconds ?? 0);
        else message.error(e.body?.message || e.body?.errorCode);
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
