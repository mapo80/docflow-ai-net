import React, { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { Table, Space, Button, Progress, Tag, Typography, Input, Select, DatePicker, Grid, Tooltip, Popconfirm, message, Badge, Segmented, Switch } from 'antd';
import { PlusOutlined, ReloadOutlined, SearchOutlined, EyeOutlined, StopOutlined, DeleteOutlined, FileTextOutlined } from '@ant-design/icons';
import dayjs, { Dayjs } from 'dayjs';
import { Link, useNavigate } from 'react-router-dom';
import { listJobs, cancelJob } from '@/services/jobsApi';

const { Title, Text } = Typography;
const { RangePicker } = DatePicker;
const useBreakpoint = Grid.useBreakpoint;

type JobRow = {
  id: string;
  status: string;
  progress?: number;
  attempts?: number;
  fileName?: string;
  createdAt?: string;
  updatedAt?: string;
  immediate?: boolean;
  modelName?: string;
  templateName?: string;
};

const STATUS_COLORS: Record<string, string> = {
  Queued: 'default',
  Running: 'processing',
  Succeeded: 'success',
  Failed: 'error',
  Cancelled: 'warning',
};

const TERMINAL = new Set(['Succeeded', 'Failed', 'Cancelled']);

const JobsList: React.FC = () => {
  const screens = useBreakpoint();
  const navigate = useNavigate();

  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(20);
  const [total, setTotal] = useState(0);
  const [rows, setRows] = useState<JobRow[]>([]);
  const [loading, setLoading] = useState(false);

  // Filters
  const [q, setQ] = useState('');
  const [statusFilter, setStatusFilter] = useState<string[]>([]);
  const [dateRange, setDateRange] = useState<[Dayjs, Dayjs] | null>(null);
  const [autoRefresh, setAutoRefresh] = useState(true);

  const load = useCallback(async (p = page, ps = pageSize) => {
    setLoading(true);
    try {
      const resp: any = await listJobs({ page: p, pageSize: ps, q, status: statusFilter, from: dateRange ? dateRange[0].toISOString() : undefined, to: dateRange ? dateRange[1].toISOString() : undefined });
      // Expect shape: { page, pageSize, total, items: [{ id, status, progress, attempts, fileName, createdAt, updatedAt, immediate }] }
      setTotal(resp.total ?? 0);
      setRows((resp.items ?? []).map((x: any) => ({
        id: x.id ?? x.Id,
        status: x.status ?? x.Status,
        progress: x.progress ?? x.Progress,
        attempts: x.attempts ?? x.Attempts,
        fileName: x.fileName ?? x.FileName ?? x.inputFileName,
        createdAt: x.createdAt ?? x.CreatedAt,
        updatedAt: x.updatedAt ?? x.UpdatedAt,
        immediate: x.immediate ?? x.Immediate,
        modelName: x.modelName ?? x.ModelName ?? x.meta?.modelName ?? x.output?.modelName,
        templateName: x.templateName ?? x.TemplateName ?? x.meta?.templateName ?? x.output?.templateName,
      })));
    } catch (e: any) {
      message.error(e?.message ?? 'Load failed');
    } finally {
      setLoading(false);
    }
  }, [page, pageSize]);

  useEffect(() => { load(page, pageSize); }, [page, pageSize, load]);

  // Auto refresh
  useEffect(() => {
    if (!autoRefresh) return;
    const t = setInterval(() => load(page, pageSize), 2000);
    return () => clearInterval(t);
  }, [autoRefresh, page, pageSize, load]);

  const filtered = useMemo(() => {
    let data = rows;
    if (q.trim()) {
      const s = q.trim().toLowerCase();
      data = data.filter(r =>
        (r.id && r.id.toLowerCase().includes(s)) ||
        (r.fileName && r.fileName.toLowerCase().includes(s)) ||
        (r.modelName && r.modelName.toLowerCase().includes(s)) ||
        (r.templateName && r.templateName.toLowerCase().includes(s))
      );
    }
    if (statusFilter.length) {
      data = data.filter(r => statusFilter.includes(r.status));
    }
    if (dateRange) {
      const [from, to] = dateRange;
      data = data.filter(r => {
        const d = r.createdAt ? dayjs(r.createdAt) : null;
        return d && d.isAfter(from) && d.isBefore(to);
      });
    }
    return data;
  }, [rows, q, statusFilter, dateRange]);

  const cancelJob = async (id: string) => {
    try {
      await cancelJob(id);
      message.success('Job cancellato');
      load(page, pageSize);
    } catch (e: any) {
      message.error(e?.message ?? 'Cancel failed');
    }
  };

  const columns = useMemo(() => {
    return [
      {
        title: 'ID',
        dataIndex: 'id',
        width: 260,
        render: (v: string) => <Link to={`/jobs/${v}`}>{v}</Link>,
        ellipsis: true,
        fixed: screens.xl ? 'left' : undefined,
      },
      {
        title: 'File',
        dataIndex: 'fileName',
        ellipsis: true,
        render: (v?: string) => v ?? '—',
      },
      {
        title: 'Status',
        dataIndex: 'status',
        filters: Object.keys(STATUS_COLORS).map(s => ({ text: s, value: s })),
        onFilter: (value: any, record: JobRow) => record.status === value,
        render: (s: string) => <Tag color={STATUS_COLORS[s] ?? 'default'}>{s}</Tag>,
        width: 140,
      },
      {
        title: 'Progress',
        dataIndex: 'progress',
        width: 160,
        render: (p?: number, r: JobRow) => p != null ? <Progress percent={p} size="small" status={r.status === 'Failed' ? 'exception' : r.status === 'Succeeded' ? 'success' : 'active'} /> : '—',
        responsive: ['sm'],
      },
      {
        title: 'Attempts',
        dataIndex: 'attempts',
        width: 100,
        render: (a?: number) => <Badge count={a || 0} showZero />,
        responsive: ['lg'],
      },
      {
        title: 'Model',
        dataIndex: 'modelName',
        ellipsis: true,
        render: (v?: string) => v ? <Tag>{v}</Tag> : '—',
        responsive: ['lg'],
      },
      {
        title: 'Template',
        dataIndex: 'templateName',
        ellipsis: true,
        render: (v?: string) => v ? <Tag color="geekblue">{v}</Tag> : '—',
        responsive: ['lg'],
      },
      {
        title: 'Created',
        dataIndex: 'createdAt',
        width: 180,
        render: (v?: string) => v ? dayjs(v).format('YYYY-MM-DD HH:mm') : '—',
        responsive: ['md'],
        sorter: (a: JobRow, b: JobRow) => dayjs(a.createdAt).valueOf() - dayjs(b.createdAt).valueOf(),
        defaultSortOrder: 'descend' as const,
      },
      {
        title: 'Updated',
        dataIndex: 'updatedAt',
        width: 180,
        render: (v?: string) => v ? dayjs(v).format('YYYY-MM-DD HH:mm') : '—',
        responsive: ['xl'],
      },
      {
        title: 'Actions',
        key: 'actions',
        fixed: screens.xl ? 'right' : undefined,
        width: 220,
        render: (_: any, r: JobRow) => (
          <Space>
            <Tooltip title="Vedi">
              <Button icon={<EyeOutlined />} onClick={() => navigate(`/jobs/${r.id}`)} />
            </Tooltip>
            <Tooltip title="File">
              <Button icon={<FileTextOutlined />} href={`/api/v1/jobs/${r.id}/file`} target="_blank" />
            </Tooltip>
            <Tooltip title={TERMINAL.has(r.status) ? "Non annullabile" : "Annulla"}>
              <Popconfirm title="Annullare il job?" onConfirm={() => cancelJob(r.id)} okText="Sì" cancelText="No" disabled={TERMINAL.has(r.status)}>
                <Button icon={<StopOutlined />} danger disabled={TERMINAL.has(r.status)} />
              </Popconfirm>
            </Tooltip>
          </Space>
        ),
      },
    ];
  }, [navigate, screens.xl]);

  return (
    <div style={{ padding: 16 }}>
      <Space direction="vertical" size="large" style={{ width: '100%' }}>
        <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: 12, flexWrap: 'wrap' }}>
          <Title level={3} style={{ margin: 0 }}>Jobs</Title>
          <Space wrap>
            <Tooltip title="Aggiorna">
              <Button icon={<ReloadOutlined />} onClick={() => load(page, pageSize)} />
            </Tooltip>
            <Button type="primary" icon={<PlusOutlined />} onClick={() => navigate('/jobs/new')}>
              Nuovo Job
            </Button>
          </Space>
        </div>

        <Space wrap style={{ width: '100%' }}>
          <Input
            allowClear
            style={{ minWidth: 240 }}
            placeholder="Cerca per ID, file, modello, template"
            prefix={<SearchOutlined />}
            value={q}
            onChange={(e) => setQ(e.target.value)}
          />
          <Select
            mode="multiple"
            allowClear
            placeholder="Status"
            style={{ minWidth: 200 }}
            value={statusFilter}
            onChange={setStatusFilter}
            options={Object.keys(STATUS_COLORS).map(s => ({ label: s, value: s }))}
          />
          <RangePicker
            onChange={(v) => setDateRange(v as any)}
            showTime
            allowEmpty={[true, true]}
          />
          <Space>
            <Text>Auto refresh</Text>
            <Switch checked={autoRefresh} onChange={setAutoRefresh} />
          </Space>
        </Space>

        <Table<JobRow>
          rowKey="id"
          size="middle"
          bordered
          sticky
          loading={loading}
          dataSource={filtered}
          columns={columns as any}
          pagination={{
            current: page,
            pageSize,
            total,
            showSizeChanger: true,
            onChange: (p, ps) => { setPage(p); setPageSize(ps); },
          }}
          scroll={{ x: 1200 }}
        />
      </Space>
    </div>
  );
};

export default JobsList;
