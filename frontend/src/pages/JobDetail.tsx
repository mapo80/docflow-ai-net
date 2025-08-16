import { useEffect, useState } from 'react';
import { useParams } from 'react-router-dom';
import { JobsService, type JobDetailResponse, OpenAPI, ApiError } from '../generated';
import { request as __request } from '../generated/core/request';
import { Descriptions, Progress, Button, message, Space, List, Modal } from 'antd';
import JobStatusTag from '../components/JobStatusTag';
import MarkdownPreview from '@uiw/react-markdown-preview';

export default function JobDetail() {
  const { id } = useParams();
  const [job, setJob] = useState<JobDetailResponse | null>(null);
  const [preview, setPreview] = useState<{ label: string; type: 'json' | 'markdown'; content: string } | null>(null);

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
    try {
      const content = await __request<string>(OpenAPI, { method: 'GET', url });
      const lower = path.toLowerCase();
      const type = lower.endsWith('.json') ? 'json' : 'markdown';
      setPreview({ label, type, content });
    } catch {
      const lower = path.toLowerCase();
      const type = lower.endsWith('.json') ? 'json' : 'markdown';
      setPreview({ label, type, content: '' });
    }
  };

  if (!job) return <div>Loading...</div>;

  const artifacts = Object.entries(job.paths || {}).filter(([, v]) => v) as [string, string][];

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
        <Button onClick={load}>Refresh</Button>
        <Button disabled={!['Queued', 'Running'].includes(job.status!)} onClick={handleCancel}>
          Cancel
        </Button>
      </Space>
      {artifacts.length > 0 && (
        <List
          style={{ marginTop: 16 }}
          bordered
          dataSource={artifacts}
          renderItem={([k, v]) => {
            const lower = v.toLowerCase();
            const canPreview = k !== 'input' && (lower.endsWith('.json') || lower.endsWith('.md') || lower.endsWith('.markdown'));
            return (
              <List.Item
                actions={[
                  canPreview && <Button onClick={() => showPreview(k, v)}>Preview</Button>,
                  <Button href={v} target="_blank">
                    Download
                  </Button>,
                ].filter(Boolean)}
              >
                {k}
              </List.Item>
            );
          }}
        />
      )}
      {preview && (
        <Modal open title={preview.label} footer={null} onCancel={() => setPreview(null)} width="80%">
          {preview.type === 'json' ? (
            <pre>{JSON.stringify(JSON.parse(preview.content || '{}'), null, 2)}</pre>
          ) : (
            <MarkdownPreview source={preview.content} />
          )}
        </Modal>
      )}
    </div>
  );
}
