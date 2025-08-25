import { useEffect, useState } from 'react';
import { Table, Space, Button, Badge, Alert, List, Grid } from 'antd';
import FileAddOutlined from '@ant-design/icons/FileAddOutlined';
import EyeOutlined from '@ant-design/icons/EyeOutlined';
import StopOutlined from '@ant-design/icons/StopOutlined';
import FileTextOutlined from '@ant-design/icons/FileTextOutlined';
import FileExclamationOutlined from '@ant-design/icons/FileExclamationOutlined';
import type { ColumnsType, TablePaginationConfig } from 'antd/es/table';
import { JobsService, type JobSummary, type JobDetailResponse, ApiError } from '../generated';
import JobStatusTag from '../components/JobStatusTag';
import notify from '../components/notification';
import { Link } from 'react-router-dom';
import dayjs from 'dayjs';
import type { PaginationProps } from 'antd';
import { useApiError } from '../components/ApiErrorProvider';

const terminal = ['Succeeded', 'Failed', 'Cancelled'];

export default function JobsList() {
  type JobListItem = JobSummary & { paths?: JobDetailResponse['paths'] };
  const [jobs, setJobs] = useState<JobListItem[]>([]);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState<number>(() => Number(localStorage.getItem('pageSize') || 10));
  const [total, setTotal] = useState(0);
  const [loading, setLoading] = useState(false);
  const [retry, setRetry] = useState<number | null>(null);
  const screens = Grid.useBreakpoint();
  const isMobile = !screens.md;
  const { showError } = useApiError();

  const load = async () => {
    setLoading(true);
    try {
      const res = await JobsService.jobsList({ page, pageSize });
      const items = (res.items || []) as JobListItem[];
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

  const columns: ColumnsType<JobListItem> = [
    {
      title: 'ID',
      dataIndex: 'id',
      render: (id: string) => (
        <Link to={`/jobs/${id}`} title={id}>
          {id.slice(0, 8)}
        </Link>
      ),
    },
    {
      title: 'Status',
      dataIndex: 'status',
      render: (_: string, record: JobListItem) => (
        <JobStatusTag status={record.status!} derived={record.derivedStatus} />
      ),
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
      title: 'Template',
      dataIndex: 'templateToken',
      responsive: ['lg'],
    },
    {
      title: 'Model',
      dataIndex: 'model',
      responsive: ['xl'],
    },
    {
      title: 'Language',
      dataIndex: 'language',
      responsive: ['lg'],
    },
    {
      title: 'Attempts',
      dataIndex: 'attempts',
      render: (a?: number) => <Badge count={a || 0} showZero />,
      responsive: ['lg'],
    },
    {
      title: 'Created',
      dataIndex: 'createdAt',
      render: (v: string) => dayjs(v).format('YYYY-MM-DD HH:mm'),
      responsive: ['lg'],
    },
    {
      title: 'Updated',
      dataIndex: 'updatedAt',
      render: (v: string) => dayjs(v).format('YYYY-MM-DD HH:mm'),
      responsive: ['xl'],
    },
    {
      title: 'Actions',
      render: (_: any, record: JobListItem) => (
        <Space>
          <Link to={`/jobs/${record.id}`} title="View job">
            <Button icon={<EyeOutlined />} aria-label="View job" title="View job" />
          </Link>
          <Button
            disabled={!['Queued', 'Running'].includes(record.status!)}
            onClick={() => handleCancel(record.id!)}
            icon={<StopOutlined />}
            aria-label="Cancel job"
            title="Cancel job"
          />
          {record.paths?.output?.path && (
            <Button
              onClick={() => window.open(record.paths!.output!.path!, '_blank')}
              icon={<FileTextOutlined />}
              aria-label="View output"
              title="View output"
            />
          )}
          {record.paths?.error?.path && (
            <Button
              onClick={() => window.open(record.paths!.error!.path!, '_blank')}
              icon={<FileExclamationOutlined />}
              aria-label="View error"
              title="View error"
            />
          )}
        </Space>
      ),
      responsive: ['md'],
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
          <Button type="primary" aria-label="New Job" icon={<FileAddOutlined />}>New job</Button>
        </Link>
      </div>
      {retry !== null && <Alert banner message={`Queue full. Retry in ${retry}s`} />}
      {isMobile ? (
        <List
          dataSource={jobs}
          rowKey="id"
          loading={loading}
          pagination={pagination as PaginationProps}
          renderItem={(record) => (
            <List.Item key={record.id}>
              <Space direction="vertical" style={{ width: '100%' }}>
                <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                  <Link to={`/jobs/${record.id}`} title={record.id!}>
                    {record.id!.slice(0, 8)}
                  </Link>
                  <JobStatusTag status={record.status!} derived={record.derivedStatus} />
                </div>
                <div>Template: {record.templateToken}</div>
                <div>Model: {record.model}</div>
                <div>Language: {record.language}</div>
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                  <span>{dayjs(record.updatedAt!).format('YYYY-MM-DD HH:mm')}</span>
                  <Space>
                    <Link to={`/jobs/${record.id}`} title="View job">
                      <Button icon={<EyeOutlined />} aria-label="View job" />
                    </Link>
                    {['Queued', 'Running'].includes(record.status!) && (
                      <Button
                        onClick={() => handleCancel(record.id!)}
                        icon={<StopOutlined />}
                        aria-label="Cancel job"
                      />
                    )}
                    {record.paths?.output?.path && (
                      <Button
                        onClick={() => window.open(record.paths!.output!.path!, '_blank')}
                        icon={<FileTextOutlined />}
                        aria-label="View output"
                      />
                    )}
                    {record.paths?.error?.path && (
                      <Button
                        onClick={() => window.open(record.paths!.error!.path!, '_blank')}
                        icon={<FileExclamationOutlined />}
                        aria-label="View error"
                      />
                    )}
                  </Space>
                </div>
              </Space>
            </List.Item>
          )}
        />
      ) : (
        <Table
          columns={columns}
          dataSource={jobs}
          rowKey="id"
          pagination={pagination}
          loading={loading}
        />
      )}
    </div>
  );
}
