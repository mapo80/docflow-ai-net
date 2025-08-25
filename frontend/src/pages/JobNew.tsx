import { useEffect, useState } from 'react';
import { Button, Card, Form, Input, Upload, Select } from 'antd';
import InboxOutlined from '@ant-design/icons/InboxOutlined';
import { ApiError, OpenAPI, ModelsService, TemplatesService } from '../generated';
import { request as __request } from '../generated/core/request';
import { useNavigate } from 'react-router-dom';
import { useApiError } from '../components/ApiErrorProvider';
import notify from '../components/notification';

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
  templateToken: string,
  language: string,
  engine: string,
) {
  return {
    fileBase64: await fileToBase64(file),
    fileName: file.name,
    model,
    templateToken,
    language,
    engine,
  };
}

export async function submitPayload(
  payload: Awaited<ReturnType<typeof buildPayload>>,
  idempotencyKey?: string
): Promise<any> {
  const res = await __request(OpenAPI, {
    method: 'POST',
    url: '/api/v1/jobs',
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
  const [language, setLanguage] = useState('');
  const [engine, setEngine] = useState('');
  const [idempotencyKey, setIdempotencyKey] = useState('');
  const [loading, setLoading] = useState(false);
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
    if (!model || !templateToken || !language || !engine) {
      showError('Model, template, language and engine are required');
      return;
    }
    const payload = await buildPayload(file, model, templateToken, language, engine);
    setLoading(true);
    try {
      const data = await submitPayload(payload, idempotencyKey || undefined);
      notify('success', 'Job created successfully.', data.job_id);
      navigate(`/jobs/${data.job_id}`);
    } catch (e) {
      if (e instanceof ApiError && e.status === 429) {
        notify('warning', 'queue_full');
      } else if (e instanceof ApiError) {
        // handled by ApiErrorProvider
      } else if (e instanceof Error) {
        showError(e.message);
      } else {
        showError(String(e));
      }
    } finally {
      setLoading(false);
    }
  };

  return (
    <Card>
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
        <Form.Item label="OCR language" required>
          <Select
            placeholder="Select language"
            options={[
              { value: 'eng', label: 'English' },
              { value: 'ita', label: 'Italian' },
            ]}
            value={language || undefined}
            onChange={(v) => setLanguage(v)}
          />
        </Form.Item>
        <Form.Item label="OCR engine" required>
          <Select
            placeholder="Select engine"
            options={[
              { value: 'tesseract', label: 'Tesseract' },
              { value: 'rapidocr', label: 'RapidOCR' },
            ]}
            value={engine || undefined}
            onChange={(v) => setEngine(v)}
          />
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
      </Form>
    </Card>
  );
}
