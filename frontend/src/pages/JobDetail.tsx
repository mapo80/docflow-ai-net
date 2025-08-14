import { useEffect, useState } from 'react';
import { useParams } from 'react-router-dom';
import { JobsService, type JobDetailResponse, OpenAPI, ApiError } from '../generated';
import { request as __request } from '../generated/core/request';
import { Descriptions, Progress, Button, Collapse, message, Space } from 'antd';
import JobStatusTag from '../components/JobStatusTag';
import { openHangfire } from '../hangfire';

function Artifact({ path, label }: { path: string; label: string }) {
  const [content, setContent] = useState<string>('');
  useEffect(() => {
    let url = path;
    if (url.startsWith('http')) {
      const u = new URL(url);
      url = u.pathname + u.search;
    }
    __request<string>(OpenAPI, { method: 'GET', url })
      .then(setContent)
      .catch(() => setContent(''));
  }, [path]);
  const isJson = path.endsWith('.json');
  return (
    <Collapse.Panel key={label} header={label} extra={<Button href={path} target="_blank">Scarica</Button>}>
      <pre>{isJson ? JSON.stringify(JSON.parse(content || '{}'), null, 2) : content}</pre>
    </Collapse.Panel>
  );
}

export default function JobDetail() {
  const { id } = useParams();
  const [job, setJob] = useState<JobDetailResponse | null>(null);

  const load = async () => {
    if (!id) return;
    try {
      const res = await JobsService.jobsGetById({ id });
      setJob(res);
    } catch (e) {
      // ignore
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
      message.success('Job cancellato');
      load();
    } catch (e) {
      if (e instanceof ApiError) message.error(e.body?.errorCode);
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
        <Descriptions.Item label="Created">{job.createdAt}</Descriptions.Item>
        <Descriptions.Item label="Updated">{job.updatedAt}</Descriptions.Item>
        {job.metrics?.durationMs != null && (
          <Descriptions.Item label="DurationMs">{job.metrics.durationMs}</Descriptions.Item>
        )}
      </Descriptions>
      <Space style={{ marginTop: 16 }}>
        <Button onClick={load}>Refresh</Button>
        <Button
          disabled={!['Queued', 'Running'].includes(job.status!)}
          onClick={handleCancel}
        >
          Cancel
        </Button>
        <Button onClick={openHangfire}>Apri Hangfire</Button>
      </Space>
      {artifacts.length > 0 && (
        <Collapse style={{ marginTop: 16 }}>
          {artifacts.map(([k, v]) => (
            <Artifact key={k} path={v} label={k} />
          ))}
        </Collapse>
      )}
    </div>
  );
}
