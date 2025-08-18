import { useEffect, useState } from 'react';
import { Alert, Button, Card, Checkbox, Form, Input, Upload, notification, Select } from 'antd';
import InboxOutlined from '@ant-design/icons/InboxOutlined';
import { ApiError, OpenAPI, ModelsService, TemplatesService } from '../generated';
import { request as __request } from '../generated/core/request';
import { useNavigate } from 'react-router-dom';
import { useApiError } from '../components/ApiErrorProvider';

export function validateFile(
  file: File,
  maxSize = 10 * 1024 * 1024,
  allowed = ['pdf', 'png', 'jpg', 'jpeg']
): string | undefined {
  const ext = file.name.split('.').pop()?.toLowerCase();
  if (!ext || !allowed.includes(ext)) return 'Invalid extension';
  if (file.size > maxSize) return 'File too large';
  return undefined;
}

export async function fileToBase64(file: File): Promise<string> {
  const buffer = await (file.arrayBuffer
    ? file.arrayBuffer()
    : new Response(file).arrayBuffer());
  let binary = '';
  const bytes = new Uint8Array(buffer);
  for (let i = 0; i < bytes.byteLength; i++) {
    binary += String.fromCharCode(bytes[i]);
  }
  return btoa(binary);
}

export async function buildPayload(
  file: File,
  model: string,
  templateToken: string
) {
  return {
    fileBase64: await fileToBase64(file),
    fileName: file.name,
    model,
    templateToken,
  };
}

export async function submitPayload(
  payload: Awaited<ReturnType<typeof buildPayload>>,
  immediate: boolean,
  idempotencyKey?: string
): Promise<any> {
  const res = await __request(OpenAPI, {
    method: 'POST',
    url: '/api/v1/jobs',
    query: { mode: immediate ? 'immediate' : undefined },
    body: payload,
    mediaType: 'application/json',
    headers: idempotencyKey ? { 'Idempotency-Key': idempotencyKey } : undefined,
  });
  return typeof res === 'string' ? JSON.parse(res) : res;
}

export default function JobNew() {
  const [file, setFile] = useState<File | null>(null);
  const [models, setModels] = useState<any[]>([]);
  const [templates, setTemplates] = useState<any[]>([]);
  const [model, setModel] = useState('');
  const [templateToken, setTemplateToken] = useState('');
  const [immediate, setImmediate] = useState(false);
  const [idempotencyKey, setIdempotencyKey] = useState('');
  const [loading, setLoading] = useState(false);
  const [result, setResult] = useState<any | null>(null);
  const [retryAfter, setRetryAfter] = useState<number | null>(null);
  const navigate = useNavigate();
  const { showError } = useApiError();

  useEffect(() => {
    (async () => {
      try {
        const ms = await ModelsService.modelsList();
        setModels(ms);
      } catch {
        /* ignore */
      }
      try {
        const ts = await TemplatesService.templatesList({ pageSize: 100 });
        setTemplates(ts.items || []);
      } catch {
        /* ignore */
      }
    })();
  }, []);

  const handleSubmit = async () => {
    if (!file) {
      showError('File is required');
      return;
    }
    const err = validateFile(file);
    if (err) {
      showError(err);
      return;
    }
    if (!model || !templateToken) {
      showError('Model and template are required');
      return;
    }
    const payload = await buildPayload(file, model, templateToken);
    setLoading(true);
    try {
      const data = await submitPayload(payload, immediate, idempotencyKey || undefined);
      if (data.status === 'Succeeded') {
        setResult(data);
      } else {
        notification.success({
          message: 'Job created',
          description: data.job_id,
        });
        navigate(`/jobs/${data.job_id}`);
      }
    } catch (e) {
      if (e instanceof ApiError) {
        if (e.status === 429 && e.body?.errorCode === 'immediate_capacity') {
          setRetryAfter(e.body.retry_after_seconds ?? 0);
        } else if (e.status === 429) {
          notification.warning({ message: 'queue_full' });
        }
      } else {
        showError('Error');
      }
    } finally {
      setLoading(false);
    }
  };

  return (
    <Card>
      {retryAfter !== null && (
        <Alert
          banner
          type="warning"
          message={`Immediate capacity exhausted. Retry in ${retryAfter}s`}
        />
      )}
      <Form layout="vertical">
        <Form.Item label="File" required>
          <Upload.Dragger
            maxCount={1}
            beforeUpload={(f) => {
              const e = validateFile(f);
              if (e) {
                showError(e);
                return Upload.LIST_IGNORE;
              }
              setFile(f);
              return false;
            }}
          >
            <p className="ant-upload-drag-icon">
              <InboxOutlined />
            </p>
            <p className="ant-upload-text">Drag or click to upload</p>
            {file && (
              <p>
                {file.name} ({(file.size / 1024).toFixed(1)} KB)
              </p>
            )}
          </Upload.Dragger>
        </Form.Item>
        <Form.Item label="Model" required>
          <Select
            showSearch
            placeholder="Select model"
            options={models.map((m) => ({ value: m.name, label: m.name }))}
            value={model || undefined}
            onChange={(v) => setModel(v)}
          />
        </Form.Item>
        <Form.Item label="Template" required>
          <Select
            showSearch
            placeholder="Select template"
            options={templates.map((t) => ({ value: t.token, label: t.name }))}
            value={templateToken || undefined}
            onChange={(v) => setTemplateToken(v)}
          />
        </Form.Item>
        <Form.Item>
          <Checkbox
            checked={immediate}
            onChange={(e) => setImmediate(e.target.checked)}
          >
            Immediate
          </Checkbox>
        </Form.Item>
        <Form.Item label="Idempotency Key">
          <Input
            value={idempotencyKey}
            onChange={(e) => setIdempotencyKey(e.target.value)}
          />
        </Form.Item>
        <Form.Item>
          <Button type="primary" onClick={handleSubmit} loading={loading}>
            Submit
          </Button>
        </Form.Item>
        {result && (
          <Alert
            type="success"
            message="Job completed"
            description={`Status: ${result.status}`}
          />
        )}
      </Form>
    </Card>
  );
}
