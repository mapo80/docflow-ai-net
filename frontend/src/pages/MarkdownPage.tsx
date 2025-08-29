import { useState, useEffect } from 'react';
import { Card, Form, Upload, Button, Tabs, Select } from 'antd';
import InboxOutlined from '@ant-design/icons/InboxOutlined';
import MDEditor from '@uiw/react-md-editor';
import JsonView from '@uiw/react-json-view';
import { githubLightTheme } from '@uiw/react-json-view/githubLight';
import remarkGfm from 'remark-gfm';
import remarkBreaks from 'remark-breaks';
import { ApiError } from '../generated';
import { MarkdownService } from '../generated/services/MarkdownService';
import { MarkdownSystemsService } from '../generated/services/MarkdownSystemsService';
import { validateFile } from './JobNew';
import { useApiError } from '../components/ApiErrorProvider';

export async function convertFile(file: File, language: string, markdownSystemId: string) {
  return await MarkdownService.markdownConvert({
    language,
    markdownSystemId,
    formData: { file },
  });
}

export default function MarkdownPage() {
  const [file, setFile] = useState<File | null>(null);
  const [result, setResult] = useState<any>(null);
  const [elapsed, setElapsed] = useState<number | null>(null);
  const [loading, setLoading] = useState(false);
  const [language, setLanguage] = useState('ita');
  const [markdownSystems, setMarkdownSystems] = useState<any[]>([]);
  const [markdownSystemId, setMarkdownSystemId] = useState('');
  const { showError } = useApiError();

  useEffect(() => {
    (async () => {
      try {
        const ms = await MarkdownSystemsService.markdownSystemsList();
        setMarkdownSystems(ms);
      } catch {
        /* ignore */
      }
    })();
  }, []);

  const handleConvert = async () => {
    if (!file) {
      showError('File is required');
      return;
    }
    const err = validateFile(file);
    if (err) {
      showError(err);
      return;
    }
    if (!markdownSystemId) {
      showError('Markdown system is required');
      return;
    }
    setLoading(true);
    const start = performance.now();
    try {
      const res = await convertFile(file, language, markdownSystemId);
      setResult(res);
      setElapsed(performance.now() - start);
    } catch (e) {
      if (e instanceof ApiError) {
        // handled by ApiErrorProvider
      } else if (e instanceof Error) showError(e.message);
      else showError(String(e));
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
        <Form.Item label="OCR language" required>
          <Select
            value={language}
            onChange={setLanguage}
            options={[
              { label: 'Italian', value: 'ita' },
              { label: 'English', value: 'eng' },
              { label: 'Latin', value: 'lat' },
            ]}
          />
        </Form.Item>
        <Form.Item label="Markdown system" required>
          <Select
            value={markdownSystemId || undefined}
            onChange={setMarkdownSystemId}
            options={markdownSystems.map((m) => ({ value: m.id, label: m.name }))}
          />
        </Form.Item>
        <Form.Item>
          <Button type="primary" onClick={handleConvert} loading={loading}>
            Convert
          </Button>
        </Form.Item>
      </Form>
      {result && (
        <>
          {elapsed !== null && (
            <p>Execution time: {elapsed.toFixed(0)} ms</p>
          )}
          <Tabs
            items={[
              {
                key: 'md',
                label: 'Markdown',
                children: (
                  <div style={{ maxHeight: 400, overflow: 'auto' }} data-color-mode="light">
                    <MDEditor.Markdown
                      source={result.markdown}
                      remarkPlugins={[remarkGfm, remarkBreaks]}
                    />
                  </div>
                ),
              },
              {
                key: 'json',
                label: 'JSON',
                children: (
                  <div style={{ maxHeight: 400, overflow: 'auto' }}>
                    <JsonView value={result} style={githubLightTheme} />
                  </div>
                ),
              },
            ]}
          />
        </>
      )}
    </Card>
  );
}
