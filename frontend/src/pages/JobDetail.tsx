import { useEffect, useState } from 'react';
import { useParams } from 'react-router-dom';
import { JobsService, type JobDetailResponse, OpenAPI, ApiError } from '../generated';
import { request as __request } from '../generated/core/request';
import { Descriptions, Progress, Button, message, Space, Modal, Tabs, Table } from 'antd';
import {
  ReloadOutlined,
  StopOutlined,
  FileSearchOutlined,
  DownloadOutlined,
} from '@ant-design/icons';
import JobStatusTag from '../components/JobStatusTag';
import MarkdownPreview from '@uiw/react-markdown-preview';
import JsonView from '@uiw/react-json-view';

export default function JobDetail() {
  const { id } = useParams();
  const [job, setJob] = useState<JobDetailResponse | null>(null);
  const [preview, setPreview] = useState<
    | { label: string; type: 'json' | 'markdown'; content: string }
    | { label: string; type: 'file'; src: string }
    | null
  >(null);
  const [fields, setFields] = useState<
    { key: string; value: string | null; confidence?: number; page?: number; bbox?: string }[]
  >([]);

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
        const res = await fetch(job.paths.output);
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
      if (e instanceof ApiError) message.error(e.body?.errorCode);
    }
  };

  const showPreview = async (label: string, path: string) => {
    let url = path;
    if (url.startsWith('http')) {
      const u = new URL(url);
      url = u.pathname + u.search;
    }
    const lower = path.toLowerCase();
    if (
      lower.endsWith('.json') ||
      lower.endsWith('.md') ||
      lower.endsWith('.markdown') ||
      lower.endsWith('.txt')
    ) {
      try {
        const content = await __request<string>(OpenAPI, { method: 'GET', url });
        const type = lower.endsWith('.json') ? 'json' : 'markdown';
        setPreview({ label, type, content });
      } catch {
        const type = lower.endsWith('.json') ? 'json' : 'markdown';
        setPreview({ label, type, content: '' });
      }
    } else {
      setPreview({ label, type: 'file', src: path });
    }
  };

  if (!job) return <div>Loading...</div>;

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
            href={record.path}
            target="_blank"
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
        <Descriptions.Item label="Progress">
          <Progress percent={job.progress || 0} />
        </Descriptions.Item>
        <Descriptions.Item label="Attempts">{job.attempts}</Descriptions.Item>
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
          bodyStyle={{ height: '100vh', overflowY: 'auto', padding: 0 }}
          rootClassName="fullscreen-modal"
        >
          {preview.type === 'file' ? (
            <iframe src={preview.src} style={{ width: '100%', height: '100%' }} />
          ) : preview.type === 'json' ? (
            <JsonView value={JSON.parse(preview.content || '{}')} />
          ) : (
            <MarkdownPreview source={preview.content} />
          )}
        </Modal>
      )}
    </div>
  );
}
