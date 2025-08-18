import { useEffect, useState } from 'react';
import {
  Table,
  List,
  Button,
  Input,
  Select,
  Space,
  message,
  Grid,
} from 'antd';
import type { Breakpoint } from 'antd/es/_util/responsiveObserver';
import PlusOutlined from '@ant-design/icons/PlusOutlined';
import DownloadOutlined from '@ant-design/icons/DownloadOutlined';
import FileTextOutlined from '@ant-design/icons/FileTextOutlined';
import ModelModal from '../components/ModelModal';
import ModelLogModal from '../components/ModelLogModal';
import { ModelsService, type ModelDto } from '../generated';
import dayjs from 'dayjs';

export default function ModelsPage() {
  const [models, setModels] = useState<ModelDto[]>([]);
  const [filtered, setFiltered] = useState<ModelDto[]>([]);
  const [search, setSearch] = useState('');
  const [typeFilter, setTypeFilter] = useState<'all' | 'hosted-llm' | 'local'>('all');
  const [modalOpen, setModalOpen] = useState(false);
  const [loading, setLoading] = useState(false);
  const screens = Grid.useBreakpoint();
  const isMobile = !screens.md;
  const [logModel, setLogModel] = useState<string | null>(null);

  const load = async () => {
    setLoading(true);
    try {
      const data = await ModelsService.modelsList();
      setModels(data);
    } catch {
      /* ignore */
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    load();
  }, []);

  useEffect(() => {
    let data = models;
    if (search) data = data.filter((m) => (m.name || '').toLowerCase().includes(search.toLowerCase()));
    if (typeFilter !== 'all') data = data.filter((m) => m.type === typeFilter);
    setFiltered(data);
  }, [models, search, typeFilter]);

  const columns = [
    { title: 'Name', dataIndex: 'name' },
    {
      title: 'Type',
      dataIndex: 'type',
      render: (t: string) => (t === 'hosted-llm' ? 'Hosted LLM' : 'Local'),
    },
    {
      title: 'Provider / HF Repo+File',
      render: (_: unknown, r: ModelDto) =>
        r.type === 'hosted-llm' ? r.provider : `${r.hfRepo}/${r.modelFile}`,
    },
    {
      title: 'Downloaded',
      render: (_: unknown, r: ModelDto) =>
        r.type === 'hosted-llm' ? '–' : r.downloaded ? 'Yes' : 'No',
    },
    {
      title: 'Download Status',
      render: (_: unknown, r: ModelDto) =>
        r.type === 'hosted-llm' ? '–' : r.downloadStatus || 'NotRequested',
    },
    {
      title: 'Last Used',
      dataIndex: 'lastUsedAt',
      render: (v?: string) => (v ? dayjs(v).format('YYYY-MM-DD HH:mm') : '–'),
    },
    {
      title: 'Created',
      dataIndex: 'createdAt',
      responsive: ['md'] as Breakpoint[],
      render: (v: string) => dayjs(v).format('YYYY-MM-DD HH:mm'),
    },
    {
      title: 'Updated',
      dataIndex: 'updatedAt',
      responsive: ['md'] as Breakpoint[],
      render: (v: string) => dayjs(v).format('YYYY-MM-DD HH:mm'),
    },
    {
      title: 'Actions',
      render: (_: unknown, record: ModelDto) => (
        <Space>
          {record.type === 'local' && (
            <Button
              icon={<DownloadOutlined />}
              aria-label="Start download"
              onClick={async () => {
                await ModelsService.modelsStartDownload({ id: record.id! });
                message.success('Download started');
                load();
              }}
            />
          )}
          {record.type === 'local' && (
            <Button
              icon={<FileTextOutlined />}
              aria-label="View log"
              onClick={() => setLogModel(record.id!)}
            />
          )}
        </Space>
      ),
    },
  ];

  return (
    <div>
      <Space
        direction={isMobile ? 'vertical' : 'horizontal'}
        style={{ width: '100%', marginBottom: 16 }}
        wrap
      >
        <Button
          type="primary"
          icon={<PlusOutlined />}
          aria-label="Create Model"
          onClick={() => {
            setModalOpen(true);
          }}
        >
          Create Model
        </Button>
        <Input.Search
          placeholder="Search by name"
          allowClear
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          style={{ flex: 1, minWidth: 200 }}
        />
        <Select
          value={typeFilter}
          onChange={(v) => setTypeFilter(v)}
          style={{ width: 160 }}
          options={[
            { value: 'all', label: 'All' },
            { value: 'hosted-llm', label: 'Hosted LLM' },
            { value: 'local', label: 'Local' },
          ]}
        />
      </Space>
      {isMobile ? (
        <List
          dataSource={filtered}
          loading={loading}
          renderItem={(r) => (
            <List.Item
              actions={
                r.type === 'local'
                  ? [
                      <Button
                        key="download"
                        icon={<DownloadOutlined />}
                        aria-label="Start download"
                        onClick={async () => {
                          await ModelsService.modelsStartDownload({ id: r.id! });
                          message.success('Download started');
                          load();
                        }}
                      />, 
                      <Button
                        key="log"
                        icon={<FileTextOutlined />}
                        aria-label="View log"
                        onClick={() => setLogModel(r.id!)}
                      />,
                    ]
                  : undefined
              }
            >
              <List.Item.Meta
                title={r.name}
                description={
                  <div>
                    <div>Type: {r.type === 'hosted-llm' ? 'Hosted LLM' : 'Local'}</div>
                    <div>
                      {r.type === 'hosted-llm'
                        ? `Provider: ${r.provider}`
                        : `HF: ${r.hfRepo}/${r.modelFile}`}
                    </div>
                    <div>
                      Downloaded:{' '}
                      {r.type === 'hosted-llm' ? '–' : r.downloaded ? 'Yes' : 'No'}
                    </div>
                    <div>
                      Status:{' '}
                      {r.type === 'hosted-llm' ? '–' : r.downloadStatus || 'NotRequested'}
                    </div>
                    <div>Created: {dayjs(r.createdAt).format('YYYY-MM-DD HH:mm')}</div>
                    <div>Updated: {dayjs(r.updatedAt).format('YYYY-MM-DD HH:mm')}</div>
                    <div>
                      Last Used:{' '}
                      {r.lastUsedAt ? dayjs(r.lastUsedAt).format('YYYY-MM-DD HH:mm') : '–'}
                    </div>
                  </div>
                }
              />
            </List.Item>
          )}
        />
      ) : (
        <Table<ModelDto>
          rowKey="id"
          columns={columns}
          dataSource={filtered}
          loading={loading}
          pagination={false}
        />
      )}
      {modalOpen && (
        <ModelModal
          open={modalOpen}
          onCancel={() => setModalOpen(false)}
          onCreated={load}
          existingNames={models.map((m) => m.name!).filter(Boolean) as string[]}
        />
      )}
      {logModel && (
        <ModelLogModal
          open={!!logModel}
          modelId={logModel}
          onClose={() => setLogModel(null)}
        />
      )}
    </div>
  );
}

