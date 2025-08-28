import { useEffect, useState } from 'react';
import { Table, List, Button, Input, Space, Grid, Popconfirm } from 'antd';
import type { Breakpoint } from 'antd/es/_util/responsiveObserver';
import PlusOutlined from '@ant-design/icons/PlusOutlined';
import EditOutlined from '@ant-design/icons/EditOutlined';
import DeleteOutlined from '@ant-design/icons/DeleteOutlined';
import { MarkdownSystemsService, type MarkdownSystemDto } from '../generated';
import dayjs from 'dayjs';
import notify from '../components/notification';
import MarkdownSystemModal from '../components/MarkdownSystemModal';

export default function MarkdownSystemsPage() {
  const [systems, setSystems] = useState<MarkdownSystemDto[]>([]);
  const [filtered, setFiltered] = useState<MarkdownSystemDto[]>([]);
  const [search, setSearch] = useState('');
  const [modalOpen, setModalOpen] = useState(false);
  const [modalId, setModalId] = useState<string | undefined>();
  const [loading, setLoading] = useState(false);
  const screens = Grid.useBreakpoint();
  const isMobile = !screens.md;

  const load = async () => {
    setLoading(true);
    try {
      const data = await MarkdownSystemsService.markdownSystemsList();
      setSystems(data);
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
    let data = systems;
    if (search) data = data.filter((s) => (s.name || '').toLowerCase().includes(search.toLowerCase()));
    setFiltered(data);
  }, [systems, search]);

  const columns = [
    { title: 'Name', dataIndex: 'name' },
    { title: 'Provider', dataIndex: 'provider' },
    { title: 'Endpoint', dataIndex: 'endpoint' },
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
      render: (_: unknown, r: MarkdownSystemDto) => (
        <Space>
          <Button
            icon={<EditOutlined />}
            aria-label="Edit system"
            onClick={() => {
              setModalId(r.id!);
              setModalOpen(true);
            }}
          />
          <Popconfirm
            title="Delete system?"
            onConfirm={async () => {
              await MarkdownSystemsService.markdownSystemsDelete({ id: r.id! });
              notify('success', 'Markdown system deleted');
              load();
            }}
          >
            <Button icon={<DeleteOutlined />} aria-label="Delete system" />
          </Popconfirm>
        </Space>
      ),
    },
  ];

  return (
    <div>
      <Space style={{ marginBottom: 16 }}>
        <Input.Search placeholder="Search" value={search} onChange={(e) => setSearch(e.target.value)} allowClear />
        <Button
          type="primary"
          icon={<PlusOutlined />}
          aria-label="Add system"
          onClick={() => {
            setModalId(undefined);
            setModalOpen(true);
          }}
        />
      </Space>
      {isMobile ? (
        <List
          dataSource={filtered}
          loading={loading}
          renderItem={(r) => (
            <List.Item
              actions={[
                <Button
                  key="edit"
                  icon={<EditOutlined />}
                  aria-label="Edit system"
                  onClick={() => {
                    setModalId(r.id!);
                    setModalOpen(true);
                  }}
                />,
                <Popconfirm
                  key="del"
                  title="Delete system?"
                  onConfirm={async () => {
                    await MarkdownSystemsService.markdownSystemsDelete({ id: r.id! });
                    notify('success', 'Markdown system deleted');
                    load();
                  }}
                >
                  <Button icon={<DeleteOutlined />} aria-label="Delete system" />
                </Popconfirm>,
              ]}
            >
              <List.Item.Meta
                title={r.name}
                description={
                  <div>
                    <div>Provider: {r.provider}</div>
                    <div>Endpoint: {r.endpoint}</div>
                    <div>Created: {dayjs(r.createdAt).format('YYYY-MM-DD HH:mm')}</div>
                    <div>Updated: {dayjs(r.updatedAt).format('YYYY-MM-DD HH:mm')}</div>
                  </div>
                }
              />
            </List.Item>
          )}
        />
      ) : (
        <Table<MarkdownSystemDto>
          rowKey="id"
          columns={columns}
          dataSource={filtered}
          loading={loading}
          pagination={false}
        />
      )}
      {modalOpen && (
        <MarkdownSystemModal
          open={modalOpen}
          systemId={modalId}
          onCancel={() => setModalOpen(false)}
          onSaved={() => {
            load();
          }}
          existingNames={systems.map((s) => s.name!).filter(Boolean) as string[]}
        />
      )}
    </div>
  );
}
