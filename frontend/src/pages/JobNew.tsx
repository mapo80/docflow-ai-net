import { useState, useEffect } from 'react';
import {
  Alert,
  Button,
  Card,
  Checkbox,
  Form,
  Input,
  Upload,
  Tabs,
  Space,
  notification,
  Drawer,
} from 'antd';
import MDEditor from '@uiw/react-md-editor';
import '@uiw/react-md-editor/markdown-editor.css';
import '@uiw/react-markdown-preview/markdown.css';
import InboxOutlined from '@ant-design/icons/InboxOutlined';
import FieldsEditor, {
  fieldsToJson,
  jsonToFields,
  type FieldItem,
} from '../components/FieldsEditor';
import { ApiError, OpenAPI } from '../generated';
import { request as __request } from '../generated/core/request';
import { useNavigate } from 'react-router-dom';
import { useApiError } from '../components/ApiErrorProvider';

export function isValidJson(str: string): boolean {
  try {
    JSON.parse(str);
    return true;
  } catch {
    return false;
  }
}

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
  prompt: string,
  fields: string
) {
  return {
    fileBase64: await fileToBase64(file),
    fileName: file.name,
    prompt,
    fields,
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
  const [prompt, setPrompt] = useState('');
  const [fieldsMode, setFieldsMode] = useState<'visual' | 'json'>('visual');
  const [visualFields, setVisualFields] = useState<FieldItem[]>([]);
  const [jsonFields, setJsonFields] = useState(fieldsToJson([]));
  const [immediate, setImmediate] = useState(false);
  const [idempotencyKey, setIdempotencyKey] = useState('');
  const [loading, setLoading] = useState(false);
  const [result, setResult] = useState<any | null>(null);
  const [retryAfter, setRetryAfter] = useState<number | null>(null);
  const navigate = useNavigate();
  const { showError } = useApiError();

  useEffect(() => {
    const preset = localStorage.getItem('jobPreset');
    if (preset) {
      try {
        const p = JSON.parse(preset);
        setPrompt(p.prompt || p.promptText || '');
        setVisualFields(p.visualFields || []);
        setJsonFields(p.jsonFields || fieldsToJson([]));
      } catch {
        /* ignore */
      }
    }
  }, []);

  const savePreset = () => {
    localStorage.setItem(
      'jobPreset',
      JSON.stringify({ prompt, visualFields, jsonFields })
    );
  };

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
    if (fieldsMode === 'json' && !isValidJson(jsonFields)) {
      showError('Invalid fields JSON');
      return;
    }
    const payload = await buildPayload(
      file,
      prompt,
      fieldsMode === 'json' ? jsonFields : fieldsToJson(visualFields)
    );
    savePreset();
    setLoading(true);
    try {
      const data = await submitPayload(
        payload,
        immediate,
        idempotencyKey || undefined
      );
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

  useEffect(() => {
    if (retryAfter === null) return;
    if (retryAfter <= 0) {
      setRetryAfter(null);
      return;
    }
    const id = setTimeout(() => setRetryAfter((r) => (r ?? 1) - 1), 1000);
    return () => clearTimeout(id);
  }, [retryAfter]);

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
            <p className="ant-upload-text">
              Drag or click to upload
            </p>
            {file && (
              <p>
                {file.name} ({(file.size / 1024).toFixed(1)} KB)
              </p>
            )}
          </Upload.Dragger>
        </Form.Item>
        <Form.Item label="Prompt">
          <MDEditor value={prompt} onChange={(v) => setPrompt(v ?? '')} />
        </Form.Item>
        <Form.Item label="Fields">
          <Tabs
            activeKey={fieldsMode}
            onChange={(k) => setFieldsMode(k as 'visual' | 'json')}
            items={[
              {
                key: 'visual',
                label: 'Visual',
                children: (
                  <FieldsEditor
                    value={visualFields}
                    onChange={(f) => {
                      setVisualFields(f);
                      setJsonFields(fieldsToJson(f));
                    }}
                  />
                ),
              },
              {
                key: 'json',
                label: 'JSON',
                children: (
                  <>
                    <Input.TextArea
                      rows={4}
                      value={jsonFields}
                      onChange={(e) => setJsonFields(e.target.value)}
                    />
                    <Space style={{ marginTop: 8 }}>
                      <Button onClick={() => setJsonFields(fieldsToJson(visualFields))}>
                        Import from Visual
                      </Button>
                      <Button
                        onClick={() => {
                          try {
                            setVisualFields(jsonToFields(jsonFields));
                          } catch {
                            showError('Invalid JSON');
                          }
                        }}
                      >
                        Export to Visual
                      </Button>
                    </Space>
                  </>
                ),
              },
            ]}
          />
        </Form.Item>
        <Form.Item>
          <Space direction="vertical" style={{ width: '100%' }}>
            <Checkbox
              checked={immediate}
              onChange={(e) => setImmediate(e.target.checked)}
            >
              Run immediately
            </Checkbox>
            <Input
              placeholder="Idempotency-Key"
              value={idempotencyKey}
              onChange={(e) => setIdempotencyKey(e.target.value)}
            />
            <Button
              type="primary"
              onClick={handleSubmit}
              loading={loading}
              disabled={!file}
            >
              Submit
            </Button>
          </Space>
        </Form.Item>
      </Form>
      <Drawer
        open={!!result}
        onClose={() => setResult(null)}
        title="Immediate result"
        width={480}
      >
        {result && (
          <>
            <p>Status: {result.status}</p>
            {result.output && (
              <pre>{JSON.stringify(result.output, null, 2)}</pre>
            )}
            {result.error && <pre>{result.error}</pre>}
            <Button onClick={() => navigate(`/jobs/${result.id}`)}>
              Go to detail
            </Button>
          </>
        )}
      </Drawer>
    </Card>
  );
}
