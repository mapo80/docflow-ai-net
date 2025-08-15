import { useEffect, useState } from 'react';
import { Table, Space, Button, Progress, Badge, Alert, message } from 'antd';
import { FileAddOutlined } from '@ant-design/icons';
import type { ColumnsType, TablePaginationConfig } from 'antd/es/table';
import { JobsService, type JobDetailResponse, ApiError } from '../generated';
import JobStatusTag from '../components/JobStatusTag';
import { Link } from 'react-router-dom';
import dayjs from 'dayjs';
import { openHangfire } from '../hangfire';

const terminal = ['Succeeded', 'Failed', 'Cancelled'];

export default function JobsList() {
  const [jobs, setJobs] = useState<JobDetailResponse[]>([]);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState<number>(() => Number(localStorage.getItem('pageSize') || 10));
  const [total, setTotal] = useState(0);
  const [loading, setLoading] = useState(false);
  const [retry, setRetry] = useState<number | null>(null);

  const load = async () => {
    setLoading(true);
    try {
      const res = await JobsService.jobsList({ page, pageSize });
      const items = (res.items || []) as JobDetailResponse[];
      setJobs(items);
      setTotal(res.total || 0);
      if (items.some((j) => !terminal.includes(j.status!))) {
        // polling
      }
    } catch (e) {
      if (e instanceof ApiError && e.status === 429 && e.body?.retry_after_seconds) {
        setRetry(e.body.retry_after_seconds);
      }
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    load();
  }, [page, pageSize]);

  useEffect(() => {
    if (retry === null) return;
    if (retry <= 0) {
      setRetry(null);
      load();
      return;
    }
    const id = setTimeout(() => setRetry((r) => (r ? r - 1 : null)), 1000);
    return () => clearTimeout(id);
  }, [retry]);

  useEffect(() => {
    const id = setInterval(() => {
      if (jobs.some((j) => !terminal.includes(j.status!))) {
        load();
      }
    }, 5000);
    return () => clearInterval(id);
  }, [jobs, page, pageSize]);

  const handleCancel = async (id: string) => {
    try {
      await JobsService.jobsDelete({ id });
      message.success('Job canceled');
      load();
    } catch (e) {
      if (e instanceof ApiError) {
        message.error(e.body?.errorCode);
      }
    }
  };

  const columns: ColumnsType<JobDetailResponse> = [
    {
      title: 'ID',
      dataIndex: 'id',
      render: (id: string) => <Link to={`/jobs/${id}`}>{id}</Link>,
    },
    {
      title: 'Status',
      dataIndex: 'status',
      render: (_: string, record: JobDetailResponse) => <JobStatusTag status={record.status!} derived={record.derivedStatus} />,
      filters: [
        { text: 'Queued', value: 'Queued' },
        { text: 'Running', value: 'Running' },
        { text: 'Succeeded', value: 'Succeeded' },
        { text: 'Failed', value: 'Failed' },
        { text: 'Cancelled', value: 'Cancelled' },
      ],
      onFilter: (value, record) => record.status === value,
    },
    {
      title: 'Progress',
      dataIndex: 'progress',
      render: (p?: number) => <Progress percent={p || 0} size="small" />,
    },
    {
      title: 'Attempts',
      dataIndex: 'attempts',
      render: (a?: number) => <Badge count={a || 0} showZero />,
    },
    {
      title: 'Created',
      dataIndex: 'createdAt',
      render: (v: string) => dayjs(v).format('YYYY-MM-DD HH:mm'),
    },
    {
      title: 'Updated',
      dataIndex: 'updatedAt',
      render: (v: string) => dayjs(v).format('YYYY-MM-DD HH:mm'),
    },
    {
      title: 'Actions',
      render: (_: any, record: JobDetailResponse) => (
        <Space>
          <Link to={`/jobs/${record.id}`}>View</Link>
          <Button disabled={!['Queued', 'Running'].includes(record.status!)} onClick={() => handleCancel(record.id!)}>
            Cancel
          </Button>
          {record.paths?.output && (
            <Button onClick={() => window.open(record.paths!.output!, '_blank')}>Output</Button>
          )}
          {record.paths?.error && (
            <Button onClick={() => window.open(record.paths!.error!, '_blank')}>Error</Button>
          )}
        </Space>
      ),
    },
  ];

  const pagination: TablePaginationConfig = {
    current: page,
    pageSize,
    total,
    showSizeChanger: true,
    pageSizeOptions: ['10', '20', '50', '100'],
    onChange: (p, ps) => {
      setPage(p);
      setPageSize(ps);
      localStorage.setItem('pageSize', String(ps));
    },
  };

  return (
    <div>
      <div
        style={{
          display: 'flex',
          justifyContent: 'flex-end',
          marginBottom: 16,
          gap: 8,
        }}
      >
        <Link to="/jobs/new">
          <Button aria-label="New Job" icon={<FileAddOutlined />} />
        </Link>
        <Button onClick={openHangfire}>Open Hangfire</Button>
      </div>
      {retry !== null && <Alert banner message={`Queue full. Retry in ${retry}s`} />}
      <Table columns={columns} dataSource={jobs} rowKey="id" pagination={pagination} loading={loading} />
    </div>
  );
}
