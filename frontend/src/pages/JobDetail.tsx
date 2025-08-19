import { useEffect, useState } from 'react';
import { useLocation, useParams } from 'react-router-dom';
import { JobsService, type JobDetailResponse, OpenAPI, ApiError, ModelsService, TemplatesService } from '../generated';
import { Badge, Descriptions, Progress, Button, message, Space, Modal, Tabs, Table } from 'antd';
import ReloadOutlined from '@ant-design/icons/ReloadOutlined';
import StopOutlined from '@ant-design/icons/StopOutlined';
import FileSearchOutlined from '@ant-design/icons/FileSearchOutlined';
import DownloadOutlined from '@ant-design/icons/DownloadOutlined';
import JobStatusTag from '../components/JobStatusTag';
import MarkdownPreview from '@uiw/react-markdown-preview';
import JsonView from '@uiw/react-json-view';
import { githubLightTheme } from '@uiw/react-json-view/githubLight';
import Loader from '../components/Loader';
import { useApiError } from '../components/ApiErrorProvider';

export default function JobDetail() {
  const { id } = useParams();
  const location = useLocation();
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
    { key: string; value: string | null; confidence?: number; page?: number; bbox?: string }[]
  >([]);
  const { showError } = useApiError();

  const load = async () => {
    if (!id) return;
    try {
      const res = await JobsService.jobsGetById({ id });
      setJob(res);
    } catch {
      /* ignore */
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
      if (!job?.paths?.output) {
        setFields([]);
        return;
      }
      try {
        const outUrl = job.paths.output.startsWith('http')
          ? job.paths.output
          : `${OpenAPI.BASE}${job.paths.output}`;
        const res = await fetch(outUrl, {
          headers: OpenAPI.HEADERS as Record<string, string> | undefined,
        });
        const json = await res.json();
        const rows: {
          key: string;
          value: string | null;
          confidence?: number;
          page?: number;
          bbox?: string;
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
              bbox: box ? `${box.X},${box.Y},${box.W},${box.H}` : undefined,
            });
          });
        } else if (json.fields) {
          json.fields.forEach((f: any, i: number) => {
            const span = f.evidence?.[0];
            const box = span?.bbox;
            rows.push({
              key: f.key ?? `f${i}`,
              value: f.value ?? null,
              confidence: f.confidence,
              page: span?.page,
              bbox: box ? `${box.x},${box.y},${box.w},${box.h}` : undefined,
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

  const handleCancel = async () => {
    if (!id) return;
    try {
      await JobsService.jobsDelete({ id });
      message.success('Job canceled');
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

  if (!job) return <Loader />;

  const artifacts = Object.entries(job.paths || {})
    .filter(([k, v]) => v && (k !== 'error' || job.status !== 'Succeeded'))
    .map(([k, v]) => ({ key: k, label: k, path: v as string }));

  const fieldColumns = [
    { title: 'Key', dataIndex: 'key' },
    { title: 'Value', dataIndex: 'value' },
    { title: 'Page', dataIndex: 'page' },
    { title: 'BBox', dataIndex: 'bbox' },
    { title: 'Confidence', dataIndex: 'confidence' },
  ];

  const fileColumns = [
    { title: 'Name', dataIndex: 'label' },
    {
      title: 'Actions',
      render: (_: any, record: { label: string; path: string }) => (
        <Space>
          {record.label !== 'input' && (
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
      {(location.state as any)?.newJob && (
        <div style={{ marginBottom: 16 }}>
          <Badge status="success" text="Job created successfully." />
        </div>
      )}
      <Descriptions title={`Job ${job.id}`} bordered column={1} size="small">
        <Descriptions.Item label="Status">
          <JobStatusTag status={job.status!} derived={job.derivedStatus} />
        </Descriptions.Item>
        <Descriptions.Item label="Progress">
          <Progress percent={job.progress || 0} />
        </Descriptions.Item>
        <Descriptions.Item label="Attempts">{job.attempts}</Descriptions.Item>
        <Descriptions.Item label="Model">
          {modelInfo?.name || job.model}
        </Descriptions.Item>
        <Descriptions.Item label="Template">
          {templateInfo?.name || job.templateToken}
        </Descriptions.Item>
        <Descriptions.Item label="Immediate">{job.immediate ? 'Yes' : 'No'}</Descriptions.Item>
        <Descriptions.Item label="Created">{job.createdAt}</Descriptions.Item>
        <Descriptions.Item label="Updated">{job.updatedAt}</Descriptions.Item>
        {job.metrics?.durationMs != null && (
          <Descriptions.Item label="DurationMs">{job.metrics.durationMs}</Descriptions.Item>
        )}
      </Descriptions>
      <Space style={{ marginTop: 16 }}>
        <Button
          onClick={load}
          icon={<ReloadOutlined />}
          aria-label="Refresh"
          title="Refresh"
        />
        <Button
          disabled={!['Queued', 'Running'].includes(job.status!)}
          onClick={handleCancel}
          icon={<StopOutlined />}
          aria-label="Cancel job"
          title="Cancel job"
        />
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
                dataSource={artifacts}
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
      {preview && (
        <Modal
          open
          title={preview.label}
          footer={null}
          onCancel={() => setPreview(null)}
          width="100%"
          style={{ top: 0 }}
          bodyStyle={{
            height: '100vh',
            overflow: 'auto',
            padding: 0,
            backgroundColor: '#fff',
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
