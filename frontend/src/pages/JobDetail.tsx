import { useEffect, useState } from 'react';
import { useParams } from 'react-router-dom';
import { JobsService, type JobDetailResponse, OpenAPI, ApiError, ModelsService, TemplatesService } from '../generated';
import { Descriptions, Button, Space, Modal, Tabs, Table, Alert, Popover, Tooltip } from 'antd';
import ReloadOutlined from '@ant-design/icons/ReloadOutlined';
import StopOutlined from '@ant-design/icons/StopOutlined';
import FileSearchOutlined from '@ant-design/icons/FileSearchOutlined';
import DownloadOutlined from '@ant-design/icons/DownloadOutlined';
import EyeOutlined from '@ant-design/icons/EyeOutlined';
import InfoCircleOutlined from '@ant-design/icons/InfoCircleOutlined';
import JobDetailPage from './JobDetailPage';
import JobStatusTag from '../components/JobStatusTag';
import notify from '../components/notification';
import MarkdownPreview from '@uiw/react-markdown-preview';
import JsonView from '@uiw/react-json-view';
import { githubLightTheme } from '@uiw/react-json-view/githubLight';
import Loader from '../components/Loader';
import { useApiError } from '../components/ApiErrorProvider';

export default function JobDetail() {
  const { id } = useParams();
  const [job, setJob] = useState<JobDetailResponse | null>(null);
  const [modelInfo, setModelInfo] = useState<any | null>(null);
  const [templateInfo, setTemplateInfo] = useState<any | null>(null);
  const [preview, setPreview] = useState<
    | { label: string; type: 'json'; content: any }
    | { label: string; type: 'markdown'; content: string }
    | { label: string; type: 'file'; src: string }
    | null
  >(null);
  const [fields, setFields] = useState<
    { key: string; value: string | null; confidence?: number; page?: number; hasBbox: boolean }[]
  >([]);
  const [files, setFiles] = useState<
    {
      key: string;
      label: string;
      path: string;
      createdAt?: string | null;
      info: string;
    }[]
  >([]);
  const [error, setError] = useState<string | null>(null);
  const { showError } = useApiError();
  const [viewerOpen, setViewerOpen] = useState(false);

  const load = async () => {
    if (!id) return;
    try {
      const res = await JobsService.jobsGetById({ id });
      setJob(res);
      setError(null);
    } catch (e) {
      setJob(null);
      if (e instanceof ApiError && e.status === 404) {
        const msg = 'Job not found';
        setError(msg);
        // handled by ApiErrorProvider
      } else if (e instanceof ApiError) {
        setError(e.message);
        // handled by ApiErrorProvider
      } else if (e instanceof Error) {
        setError(e.message);
        showError(e.message);
      }
    }
  };

  useEffect(() => {
    load();
  }, [id]);

  useEffect(() => {
    if (!job || ['Succeeded', 'Failed', 'Cancelled'].includes(job.status!)) return;
    const t = setInterval(load, 3000);
    return () => clearInterval(t);
  }, [job]);

  useEffect(() => {
    if (!job) return;
    (async () => {
      try {
        const ms = await ModelsService.modelsList();
        setModelInfo(ms.find((m: any) => m.name === job.model) || null);
      } catch {
        setModelInfo(null);
      }
      try {
        const ts = await TemplatesService.templatesList({ pageSize: 100 });
        setTemplateInfo(
          ts.items?.find((t: any) => t.token === job.templateToken) || null,
        );
      } catch {
        setTemplateInfo(null);
      }
    })();
  }, [job]);

  useEffect(() => {
    const fetchFields = async () => {
      const direct: any = (job as any)?.fields;
      if (Array.isArray(direct)) {
        setFields(direct);
        return;
      }
      if (!job?.paths?.output?.path || job.status === 'Running') {
        setFields([]);
        return;
      }
      try {
        const outUrl = job.paths.output.path.startsWith('http')
          ? job.paths.output.path
          : `${OpenAPI.BASE}${job.paths.output.path}`;
        const res = await fetch(outUrl, {
          headers: OpenAPI.HEADERS as Record<string, string> | undefined,
        });
        const json = await res.json();
        const rows: {
          key: string;
          value: string | null;
          confidence?: number;
          page?: number;
          hasBbox: boolean;
        }[] = [];
        if (Array.isArray(json)) {
          json.forEach((f: any, i: number) => {
            const span = f.Spans?.[0];
            const box = span?.BBox;
            rows.push({
              key: f.FieldName ?? `f${i}`,
              value: f.Value ?? null,
              confidence: f.Confidence,
              page: span?.Page,
              hasBbox: !!box,
            });
          });
        } else if (json.fields) {
          json.fields.forEach((f: any, i: number) => {
            const span =
              (Array.isArray(f.spans) ? f.spans[0] : undefined) ||
              (Array.isArray(f.evidence) ? f.evidence[0] : undefined);
            rows.push({
              key: f.key ?? `f${i}`,
              value: f.value ?? null,
              confidence: f.confidence,
              page: span?.page,
              hasBbox: !!span,
            });
          });
        }
        setFields(rows);
      } catch {
        setFields([]);
      }
    };
    fetchFields();
  }, [job]);

  useEffect(() => {
    if (!job) return;
    const mapping: Record<string, { label: string; info: string }> = {
      input: {
        label: 'Input',
        info: 'File submitted with the job request',
      },
      prompt: {
        label: 'Prompt',
        info: 'Prompt sent to the LLM for extraction',
      },
      markdown: {
        label: 'Markdown',
        info: 'Markdown content extracted from the input file',
      },
      markdownJson: {
        label: 'Layout',
        info: 'JSON response from the markdown conversion service',
      },
      output: { label: 'Output', info: 'LLM response' },
      error: {
        label: 'Error',
        info: 'Error message produced during job processing',
      },
    };
    const order = ['input', 'prompt', 'markdown', 'markdownJson', 'output', 'error'];
    const entries = Object.entries(job.paths || {}).filter(([k, v]: any) => {
      if (!v || !v.path) return false;
      if (k === 'error' && job.status === 'Succeeded') return false;
      if (k === 'output' && job.status !== 'Succeeded') return false;
      return true;
    });
    const sorted = order
      .map((k) => entries.find((e) => e[0] === k))
      .filter(Boolean) as any[];
    setFiles(
      sorted.map(([k, v]: any) => ({
        key: k,
        label: mapping[k].label,
        info: mapping[k].info,
        path: v.path as string,
        createdAt: v.createdAt as string | null,
      })),
    );
  }, [job]);


  const handleCancel = async () => {
    if (!id) return;
    try {
      await JobsService.jobsDelete({ id });
      notify('success', 'Job canceled');
      load();
    } catch (e) {
      if (e instanceof ApiError) {
        // handled by interceptor
      } else if (e instanceof Error) {
        showError(e.message);
      }
    }
  };

  const showPreview = async (label: string, path: string) => {
    let url = path;
    if (!url.startsWith('http')) url = `${OpenAPI.BASE}${path}`;
    try {
      const resp = await fetch(url, {
        headers: OpenAPI.HEADERS as Record<string, string> | undefined,
      });
      const ct = resp.headers.get('content-type')?.toLowerCase() ?? '';
      if (ct.includes('application/json') || ct.includes('text/json')) {
        let json: unknown = {};
        try {
          json = await resp.json();
        } catch {
          try {
            json = JSON.parse(await resp.text());
          } catch {
            json = {};
          }
        }
        setPreview({ label, type: 'json', content: json });
      } else if (
        ct.includes('text/markdown') ||
        ct.includes('text/plain')
      ) {
        const text = await resp.text();
        setPreview({ label, type: 'markdown', content: text });
      } else {
        setPreview({ label, type: 'file', src: url });
      }
    } catch {
      setPreview({ label, type: 'file', src: path });
    }
  };

  const handleDownload = async (label: string, path: string) => {
    let url = path;
    if (!url.startsWith('http')) url = `${OpenAPI.BASE}${path}`;
    try {
      const resp = await fetch(url, {
        headers: OpenAPI.HEADERS as Record<string, string> | undefined,
      });
      if (!resp.ok) return;
      const blob = await resp.blob();
      const blobUrl = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = blobUrl;
      a.download = path.split('/').pop() || label;
      document.body.appendChild(a);
      a.click();
      a.remove();
      URL.revokeObjectURL(blobUrl);
    } catch {
      /* ignore */
    }
  };

  if (!job) {
    return error ? <Alert type="error" message={error} /> : <Loader />;
  }

  const docType = job.paths?.input?.path?.toLowerCase().endsWith('.pdf') ? 'pdf' : 'image';

  const fieldColumns = [
    { title: 'Key', dataIndex: 'key' },
    { title: 'Value', dataIndex: 'value' },
    {
      title: 'Page',
      dataIndex: 'page',
      render: (value: number | undefined) =>
        docType === 'pdf' ? value ?? '-' : '-',
    },
    {
      title: 'BBox',
      dataIndex: 'hasBbox',
      render: (v: boolean) => (
        <Tooltip title={v ? 'Bounding box available' : 'Bounding box unavailable'}>
          {v ? '✅' : '❌'}
        </Tooltip>
      ),
    },
    { title: 'Confidence', dataIndex: 'confidence' },
  ];

  const fileColumns = [
    {
      title: 'Name',
      dataIndex: 'label',
      render: (_: any, record: any) => (
        <Space>
          <Popover content={record.info} trigger="click">
            <InfoCircleOutlined aria-label="Info" />
          </Popover>
          {record.label}
        </Space>
      ),
    },
    { title: 'Created', dataIndex: 'createdAt' },
    {
      title: 'Actions',
      render: (_: any, record: { key: string; label: string; path: string }) => (
        <Space>
          {record.key === 'input' ? (
            <Button
              onClick={() => setViewerOpen(true)}
              icon={<EyeOutlined />}
              aria-label="Document preview"
              title="Document preview"
            />
          ) : (
            <Button
              onClick={() => showPreview(record.label, record.path)}
              icon={<FileSearchOutlined />}
              aria-label="Preview"
              title="Preview"
            />
          )}
          <Button
            onClick={() => handleDownload(record.label, record.path)}
            icon={<DownloadOutlined />}
            aria-label="Download"
            title="Download"
          />
        </Space>
      ),
    },
  ];

  return (
    <div>
      <Descriptions title={`Job ${job.id}`} bordered column={1} size="small">
        <Descriptions.Item label="Status">
          <JobStatusTag status={job.status!} derived={job.derivedStatus} />
        </Descriptions.Item>
        {job.status === 'Failed' && job.errorMessage && (
          <Descriptions.Item label="Error">
            <span style={{ color: '#ff4d4f' }}>{job.errorMessage}</span>
          </Descriptions.Item>
        )}
        <Descriptions.Item label="Attempts">{job.attempts}</Descriptions.Item>
        <Descriptions.Item label="Model">
          {modelInfo?.name || job.model}
        </Descriptions.Item>
        <Descriptions.Item label="Markdown system">{job.markdownSystem}</Descriptions.Item>
        <Descriptions.Item label="Template">
          {templateInfo?.name || job.templateToken}
        </Descriptions.Item>
        <Descriptions.Item label="Created">{job.createdAt}</Descriptions.Item>
        <Descriptions.Item label="Updated">{job.updatedAt}</Descriptions.Item>
        {job.metrics?.durationMs != null && (
          <Descriptions.Item label="Duration">
            {(job.metrics.durationMs / 1000).toFixed(2)} sec
          </Descriptions.Item>
        )}
      </Descriptions>
      <Space style={{ marginTop: 16 }}>
        <Button
          onClick={() => setViewerOpen(true)}
          icon={<FileSearchOutlined />}
          aria-label="Document preview"
          title="Document preview"
          data-testid="open-preview"
        >
          Document preview
        </Button>
        {job.status === 'Running' && (
          <>
            <Button
              onClick={load}
              icon={<ReloadOutlined />}
              aria-label="Reload"
              title="Reload"
            >
              Reload
            </Button>
            <Button
              onClick={handleCancel}
              icon={<StopOutlined />}
              aria-label="Cancel job"
              title="Cancel job"
            >
              Cancel
            </Button>
          </>
        )}
      </Space>
      <Tabs
        style={{ marginTop: 16 }}
        items={[
          {
            key: 'fields',
            label: 'Fields',
            children: (
              <Table
                dataSource={fields}
                columns={fieldColumns}
                size="small"
                pagination={false}
                rowKey="key"
                scroll={{ x: true }}
              />
            ),
          },
          {
            key: 'files',
            label: 'Files',
            children: (
              <Table
                dataSource={files}
                columns={fileColumns}
                size="small"
                pagination={false}
                rowKey="key"
                scroll={{ x: true }}
              />
            ),
          },
        ]}
      />
      <Modal
        open={viewerOpen}
        footer={null}
        title="Document preview"
        onCancel={() => setViewerOpen(false)}
        width="100%"
        style={{ top: 0 }}
        styles={{
          body: {
            height: 'calc(100vh - 64px)',
            overflow: 'hidden',
            padding: 0,
            backgroundColor: '#fff',
          },
        }}
        rootClassName="fullscreen-modal"
        destroyOnClose
        data-testid="viewer-modal"
      >
        {job && <JobDetailPage jobId={job.id} />}
      </Modal>
      {preview && (
        <Modal
          open
          title={preview.label}
          footer={null}
          onCancel={() => setPreview(null)}
          width="100%"
          style={{ top: 0 }}
          styles={{
            body: {
              height: '100vh',
              overflow: 'auto',
              padding: 0,
              backgroundColor: '#fff',
            },
          }}
          rootClassName="fullscreen-modal"
        >
          {preview.type === 'file' ? (
            <iframe src={preview.src} style={{ width: '100%', height: '100%' }} />
          ) : preview.type === 'json' ? (
            <div style={{ padding: 16, overflowX: 'auto' }}>
              <JsonView
                value={preview.content}
                style={{ ...githubLightTheme, padding: 16 }}
                collapsed={false}
                displayObjectSize={false}
                displayDataTypes={false}
              />
            </div>
          ) : (
            <MarkdownPreview
              source={preview.content}
              wrapperElement={{ 'data-color-mode': 'light' }}
              style={{ padding: 16 }}
            />
          )}
        </Modal>
      )}
    </div>
  );
}
