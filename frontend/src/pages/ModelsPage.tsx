import { useEffect, useState } from 'react';
import {
  Table,
  List,
  Button,
  Input,
  Select,
  Space,
  Grid,
  Popconfirm,
  Form,
} from 'antd';
import type { Breakpoint } from 'antd/es/_util/responsiveObserver';
import PlusOutlined from '@ant-design/icons/PlusOutlined';
import DownloadOutlined from '@ant-design/icons/DownloadOutlined';
import FileTextOutlined from '@ant-design/icons/FileTextOutlined';
import EditOutlined from '@ant-design/icons/EditOutlined';
import DeleteOutlined from '@ant-design/icons/DeleteOutlined';
import ModelModal from '../components/ModelModal';
import ModelLogModal from '../components/ModelLogModal';
import { ModelsService, type ModelDto } from '../generated';
import dayjs from 'dayjs';
import notify from '../components/notification';

export default function ModelsPage() {
  const [models, setModels] = useState<ModelDto[]>([]);
  const [filtered, setFiltered] = useState<ModelDto[]>([]);
  const [search, setSearch] = useState('');
  const [typeFilter, setTypeFilter] = useState<'all' | 'hosted-llm' | 'local'>('all');
  const [modalOpen, setModalOpen] = useState(false);
  const [modalId, setModalId] = useState<string | undefined>();
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
          {record.type === 'hosted-llm' && (
            <Button
              icon={<EditOutlined />}
              aria-label="Edit model"
              onClick={() => {
                setModalId(record.id!);
                setModalOpen(true);
              }}
            />
          )}
          {record.type === 'hosted-llm' && (
            <Popconfirm
              title="Delete model?"
              onConfirm={async () => {
                await ModelsService.modelsDelete({ id: record.id! });
                notify('success', 'Model deleted');
                load();
              }}
            >
              <Button icon={<DeleteOutlined />} aria-label="Delete model" />
            </Popconfirm>
          )}
          {record.type === 'local' && (
            <>
              {!record.downloaded && (
                <Button
                  icon={<DownloadOutlined />}
                  aria-label="Start download"
                  onClick={async () => {
                    await ModelsService.modelsStartDownload({ id: record.id! });
                    notify('success', 'Download started');
                    load();
                  }}
                />
              )}
              <Button
                icon={<FileTextOutlined />}
                aria-label="View log"
                onClick={() => setLogModel(record.id!)}
              />
              <Popconfirm
                title="Delete model?"
                onConfirm={async () => {
                  await ModelsService.modelsDelete({ id: record.id! });
                  notify('success', 'Model deleted');
                  load();
                }}
              >
                <Button icon={<DeleteOutlined />} aria-label="Delete model" />
              </Popconfirm>
            </>
          )}
        </Space>
      ),
    },
  ];

  return (
    <div>
      <Space
        direction={isMobile ? 'vertical' : 'horizontal'}
        style={{ width: '100%', marginBottom: 16, justifyContent: 'space-between' }}
        align={isMobile ? undefined : 'start'}
        wrap
      >
        <Form
          layout={isMobile ? 'vertical' : 'inline'}
          style={{ flex: 1, display: 'flex', flexWrap: 'wrap', gap: 8 }}
        >
          <Form.Item label="Search" style={{ marginBottom: 0 }}>
            <Input.Search
              placeholder="Search by name"
              allowClear
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              onSearch={(v) => setSearch(v)}
              style={{ width: isMobile ? '100%' : 200 }}
            />
          </Form.Item>
          <Form.Item label="Type" style={{ marginBottom: 0 }}>
            <Select
              value={typeFilter}
              onChange={(v) => setTypeFilter(v)}
              style={{ width: isMobile ? '100%' : 160 }}
              options={[
                { value: 'all', label: 'All' },
                { value: 'hosted-llm', label: 'Hosted LLM' },
                { value: 'local', label: 'Local' },
              ]}
            />
          </Form.Item>
        </Form>
        <Button
          type="primary"
          icon={<PlusOutlined />}
          aria-label="Create Model"
          onClick={() => {
            setModalId(undefined);
            setModalOpen(true);
          }}
        >
          Create Model
        </Button>
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
                      ...(r.downloaded
                        ? []
                        : [
                            <Button
                              key="download"
                              icon={<DownloadOutlined />}
                              aria-label="Start download"
                              onClick={async () => {
                                await ModelsService.modelsStartDownload({ id: r.id! });
                                notify('success', 'Download started');
                                load();
                              }}
                            />,
                          ]),
                      <Button
                        key="log"
                        icon={<FileTextOutlined />}
                        aria-label="View log"
                        onClick={() => setLogModel(r.id!)}
                      />,
                      <Popconfirm
                        key="del"
                        title="Delete model?"
                        onConfirm={async () => {
                          await ModelsService.modelsDelete({ id: r.id! });
                          notify('success', 'Model deleted');
                          load();
                        }}
                      >
                        <Button icon={<DeleteOutlined />} aria-label="Delete model" />
                      </Popconfirm>,
                    ]
                  : [
                      <Button
                        key="edit"
                        icon={<EditOutlined />}
                        aria-label="Edit model"
                        onClick={() => {
                          setModalId(r.id!);
                          setModalOpen(true);
                        }}
                      />,
                      <Popconfirm
                        key="del"
                        title="Delete model?"
                        onConfirm={async () => {
                          await ModelsService.modelsDelete({ id: r.id! });
                          notify('success', 'Model deleted');
                          load();
                        }}
                      >
                        <Button icon={<DeleteOutlined />} aria-label="Delete model" />
                      </Popconfirm>,
                    ]
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
          modelId={modalId}
          onCancel={() => setModalOpen(false)}
          onSaved={() => {
            load();
          }}
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
