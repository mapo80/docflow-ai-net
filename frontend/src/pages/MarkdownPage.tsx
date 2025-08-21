import { useState } from 'react';
import { Card, Form, Upload, Button, Tabs } from 'antd';
import InboxOutlined from '@ant-design/icons/InboxOutlined';
import MDEditor from '@uiw/react-md-editor';
import JsonView from '@uiw/react-json-view';
import { githubLightTheme } from '@uiw/react-json-view/githubLight';
import remarkGfm from 'remark-gfm';
import remarkBreaks from 'remark-breaks';
import { ApiError, OpenAPI } from '../generated';
import { request as __request } from '../generated/core/request';
import { validateFile } from './JobNew';
import { useApiError } from '../components/ApiErrorProvider';

export async function convertFile(file: File) {
  const res = await __request(OpenAPI, {
    method: 'POST',
    url: '/api/v1/markdown',
    formData: { file },
    mediaType: 'multipart/form-data',
  });
  return typeof res === 'string' ? JSON.parse(res) : res;
}

export default function MarkdownPage() {
  const [file, setFile] = useState<File | null>(null);
  const [result, setResult] = useState<any>(null);
  const [elapsed, setElapsed] = useState<number | null>(null);
  const [loading, setLoading] = useState(false);
  const { showError } = useApiError();

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
    setLoading(true);
    const start = performance.now();
    try {
      const res = await convertFile(file);
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
