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
  message,
  notification,
  Drawer,
} from 'antd';
import { InboxOutlined } from '@ant-design/icons';
import FieldsEditor, {
  fieldsToJson,
  jsonToFields,
  type FieldItem,
} from '../components/FieldsEditor';
import { HttpError } from '../api/fetcher';
import { OpenAPI } from '../generated';
import { request as __request } from '../generated/core/request';
import { useNavigate } from 'react-router-dom';

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
  allowed = ['pdf', 'txt']
): string | undefined {
  const ext = file.name.split('.').pop()?.toLowerCase();
  if (!ext || !allowed.includes(ext)) return 'Estensione non valida';
  if (file.size > maxSize) return 'File troppo grande';
  return undefined;
}

export function buildFormData(
  file: File,
  prompt: string,
  fields: string
): FormData {
  const form = new FormData();
  form.append('file', file);
  form.append('prompt', prompt);
  form.append('fields', fields);
  return form;
}

export async function submitFormData(
  form: FormData,
  immediate: boolean,
  idempotencyKey?: string
) {
  return await __request(OpenAPI, {
    method: 'POST',
    url: '/jobs',
    query: { mode: immediate ? 'immediate' : undefined },
    body: form,
    mediaType: 'multipart/form-data',
    headers: idempotencyKey ? { 'Idempotency-Key': idempotencyKey } : undefined,
  });
}

export default function JobNew() {
  const [file, setFile] = useState<File | null>(null);
  const [promptMode, setPromptMode] = useState<'text' | 'json'>('text');
  const [promptText, setPromptText] = useState('');
  const [promptJson, setPromptJson] = useState('{}');
  const [fieldsMode, setFieldsMode] = useState<'visual' | 'json'>('visual');
  const [visualFields, setVisualFields] = useState<FieldItem[]>([]);
  const [jsonFields, setJsonFields] = useState(fieldsToJson([]));
  const [immediate, setImmediate] = useState(false);
  const [idempotencyKey, setIdempotencyKey] = useState('');
  const [loading, setLoading] = useState(false);
  const [result, setResult] = useState<any | null>(null);
  const [retryAfter, setRetryAfter] = useState<number | null>(null);
  const navigate = useNavigate();

  useEffect(() => {
    const preset = localStorage.getItem('jobPreset');
    if (preset) {
      try {
        const p = JSON.parse(preset);
        setPromptText(p.promptText || '');
        setPromptJson(p.promptJson || '{}');
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
      JSON.stringify({ promptText, promptJson, visualFields, jsonFields })
    );
  };

  const handleSubmit = async () => {
    if (!file) {
      message.error('File obbligatorio');
      return;
    }
    const err = validateFile(file);
    if (err) {
      message.error(err);
      return;
    }
    if (promptMode === 'json' && !isValidJson(promptJson)) {
      message.error('Prompt JSON non valido');
      return;
    }
    if (fieldsMode === 'json' && !isValidJson(jsonFields)) {
      message.error('Fields JSON non valido');
      return;
    }
    const form = buildFormData(
      file,
      promptMode === 'json' ? promptJson : promptText,
      fieldsMode === 'json' ? jsonFields : fieldsToJson(visualFields)
    );
    savePreset();
    setLoading(true);
    try {
      const data = await submitFormData(
        form,
        immediate,
        idempotencyKey || undefined
      );
      if (data.status === 'Succeeded') {
        setResult(data);
      } else {
        notification.success({
          message: 'Job creato',
          description: data.id,
        });
        navigate(`/jobs/${data.id}`);
      }
    } catch (e) {
      if (e instanceof HttpError) {
        if (e.status === 429 && e.data.errorCode === 'immediate_capacity') {
          setRetryAfter(e.retryAfter ?? 0);
        } else if (e.status === 429) {
          notification.warning({ message: 'queue_full' });
        } else if (e.status === 413) {
          message.error('File troppo grande');
        } else if (e.status === 507) {
          notification.error({ message: 'spazio insufficiente' });
        } else {
          notification.error({ message: e.data.errorCode });
        }
      } else {
        notification.error({ message: 'Errore' });
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
          message={`Capacità immediata esaurita. Riprova tra ${retryAfter}s`}
        />
      )}
      <Form layout="vertical">
        <Form.Item label="File" required>
          <Upload.Dragger
            maxCount={1}
            beforeUpload={(f) => {
              const e = validateFile(f);
              if (e) {
                message.error(e);
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
              Trascina o clicca per caricare
            </p>
            {file && (
              <p>
                {file.name} ({(file.size / 1024).toFixed(1)} KB)
              </p>
            )}
          </Upload.Dragger>
        </Form.Item>
        <Form.Item label="Prompt">
          <Tabs
            activeKey={promptMode}
            onChange={(k) => setPromptMode(k as 'text' | 'json')}
            items={[
              {
                key: 'text',
                label: 'Testo',
                children: (
                  <Input.TextArea
                    rows={4}
                    value={promptText}
                    onChange={(e) => setPromptText(e.target.value)}
                  />
                ),
              },
              {
                key: 'json',
                label: 'JSON',
                children: (
                  <Input.TextArea
                    rows={4}
                    value={promptJson}
                    onChange={(e) => setPromptJson(e.target.value)}
                  />
                ),
              },
            ]}
          />
        </Form.Item>
        <Form.Item label="Fields">
          <Tabs
            activeKey={fieldsMode}
            onChange={(k) => setFieldsMode(k as 'visual' | 'json')}
            items={[
              {
                key: 'visual',
                label: 'Visuale',
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
                        Importa da Visuale
                      </Button>
                      <Button
                        onClick={() => {
                          try {
                            setVisualFields(jsonToFields(jsonFields));
                          } catch {
                            message.error('JSON non valido');
                          }
                        }}
                      >
                        Esporta in Visuale
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
              Esegui immediatamente
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
        title="Risultato immediato"
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
              Vai al dettaglio
            </Button>
          </>
        )}
      </Drawer>
    </Card>
  );
}
