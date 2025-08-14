import { useEffect, useState } from 'react';
import { Table, Space, Button, Progress, Badge, Alert, message } from 'antd';
import type { ColumnsType, TablePaginationConfig } from 'antd/es/table';
import { DefaultService, type Job } from '../generated';
import JobStatusTag from '../components/JobStatusTag';
import { Link } from 'react-router-dom';
import dayjs from 'dayjs';
import { HttpError } from '../api/fetcher';
import { openHangfire } from '../hangfire';

const terminal = ['Succeeded', 'Failed', 'Cancelled'];

export default function JobsList() {
  const [jobs, setJobs] = useState<Job[]>([]);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState<number>(() => Number(localStorage.getItem('pageSize') || 10));
  const [total, setTotal] = useState(0);
  const [loading, setLoading] = useState(false);
  const [retry, setRetry] = useState<number | null>(null);

  const load = async () => {
    setLoading(true);
    try {
      const res = await DefaultService.getJobs({ page, pageSize });
      setJobs(res.items);
      setTotal(res.total);
      if (res.items.some((j) => !terminal.includes(j.status))) {
        // polling
      }
    } catch (e) {
      if (e instanceof HttpError && e.status === 429 && e.retryAfter) {
        setRetry(e.retryAfter);
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
      if (jobs.some((j) => !terminal.includes(j.status))) {
        load();
      }
    }, 5000);
    return () => clearInterval(id);
  }, [jobs, page, pageSize]);

  const handleCancel = async (id: string) => {
    try {
      await DefaultService.cancelJob({ id });
      message.success('Job cancellato');
      load();
    } catch (e) {
      if (e instanceof HttpError) {
        message.error(e.data.errorCode);
      }
    }
  };

  const columns: ColumnsType<Job> = [
    {
      title: 'ID',
      dataIndex: 'id',
      render: (id: string) => <Link to={`/jobs/${id}`}>{id}</Link>,
    },
    {
      title: 'Status',
      dataIndex: 'status',
      render: (_: string, record: Job) => <JobStatusTag status={record.status} derived={record.derivedStatus} />,
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
      title: 'Azioni',
      render: (_: any, record: Job) => (
        <Space>
          <Link to={`/jobs/${record.id}`}>View</Link>
          <Button disabled={!['Queued', 'Running'].includes(record.status)} onClick={() => handleCancel(record.id)}>
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
      <div style={{ display: 'flex', justifyContent: 'flex-end', marginBottom: 16 }}>
        <Button onClick={openHangfire}>Apri Hangfire</Button>
      </div>
      {retry !== null && <Alert banner message={`Coda piena. Riprova tra ${retry}s`} />}
      <Table columns={columns} dataSource={jobs} rowKey="id" pagination={pagination} loading={loading} />
    </div>
  );
}
