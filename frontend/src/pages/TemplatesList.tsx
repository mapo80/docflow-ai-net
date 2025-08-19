import { useEffect, useState } from 'react';
import {
  Table,
  List,
  Grid,
  Button,
  Space,
  Input,
  Typography,
  Form,
  Select,
  Popconfirm,
} from 'antd';
import PlusOutlined from '@ant-design/icons/PlusOutlined';
import EditOutlined from '@ant-design/icons/EditOutlined';
import DeleteOutlined from '@ant-design/icons/DeleteOutlined';
import type { ColumnsType, TablePaginationConfig } from 'antd/es/table';
import dayjs from 'dayjs';
import { TemplatesService, type TemplateSummary } from '../generated';
import TemplateModal from '../components/TemplateModal';
import { useApiError } from '../components/ApiErrorProvider';
import notify from '../components/notification';

export default function TemplatesList() {
  const [templates, setTemplates] = useState<TemplateSummary[]>([]);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState<number>(() => Number(localStorage.getItem('tplPageSize') || 10));
  const [total, setTotal] = useState(0);
  const [loading, setLoading] = useState(false);
  const [search, setSearch] = useState('');
  const [q, setQ] = useState('');
  const [sort, setSort] = useState('createdAt desc');
  const [modalId, setModalId] = useState<string | undefined>();
  const screens = Grid.useBreakpoint();
  const isMobile = !screens.md;
  const { showError } = useApiError();

  const load = async () => {
    setLoading(true);
    try {
      const res = await TemplatesService.templatesList({ q, page, pageSize, sort });
      setTemplates(res.items || []);
      setTotal(res.total || 0);
    } catch (e: any) {
      if (e instanceof Error) showError(e.message);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    const id = setTimeout(() => setQ(search), 300);
    return () => clearTimeout(id);
  }, [search]);

  useEffect(() => {
    load();
  }, [page, pageSize, q, sort]);

  const columns: ColumnsType<TemplateSummary> = [
    { title: 'Name', dataIndex: 'name' },
    {
      title: 'Token',
      dataIndex: 'token',
      render: (v: string) => <Typography.Text copyable>{v}</Typography.Text>,
    },
    {
      title: 'Created At',
      dataIndex: 'createdAt',
      render: (v?: string) => (v ? dayjs(v).format('YYYY-MM-DD HH:mm') : ''),
    },
    {
      title: 'Last Updated',
      dataIndex: 'updatedAt',
      render: (v?: string) => (v ? dayjs(v).format('YYYY-MM-DD HH:mm') : ''),
    },
    {
      title: 'Actions',
      render: (_: any, record: TemplateSummary) => (
        <Space>
          <Button
            icon={<EditOutlined />}
            onClick={() => setModalId(record.id)}
            aria-label="Edit"
          />
          <Popconfirm
            title="Delete template?"
            onConfirm={async () => {
              try {
                await TemplatesService.templatesDelete({ id: record.id! });
                notify('success', 'Template deleted');
                load();
              } catch (e: any) {
                if (e instanceof Error) showError(e.message);
              }
            }}
          >
            <Button icon={<DeleteOutlined />} aria-label="Delete" />
          </Popconfirm>
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
      localStorage.setItem('tplPageSize', String(ps));
    },
  };

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
          onClick={() => setModalId('new')}
          aria-label="Create Template"
        >
          Create Template
        </Button>
        <Form
          layout={isMobile ? 'vertical' : 'inline'}
          style={{ flex: 1, display: 'flex', flexWrap: 'wrap', gap: 8 }}
        >
          <Form.Item label="Search" style={{ marginBottom: 0 }}>
            <Input
              placeholder="Search"
              aria-label="Search templates"
              allowClear
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              style={{ width: isMobile ? '100%' : 200 }}
            />
          </Form.Item>
          <Form.Item label="Sort" style={{ marginBottom: 0 }}>
            <Select
              value={sort}
              onChange={(v) => setSort(v)}
              style={{ width: isMobile ? '100%' : 180 }}
              data-testid="sort-select"
            >
              <Select.Option value="createdAt desc">Newest</Select.Option>
              <Select.Option value="createdAt asc">Oldest</Select.Option>
              <Select.Option value="name asc">Name A-Z</Select.Option>
              <Select.Option value="name desc">Name Z-A</Select.Option>
              <Select.Option value="updatedAt desc">Recently Updated</Select.Option>
              <Select.Option value="updatedAt asc">Least Recently Updated</Select.Option>
            </Select>
          </Form.Item>
        </Form>
      </Space>
      {isMobile ? (
        <List
          dataSource={templates}
          rowKey="id"
          loading={loading}
          pagination={pagination as any}
          renderItem={(item) => (
            <List.Item
              actions={[
                <Button
                  key="edit"
                  icon={<EditOutlined />}
                  onClick={() => setModalId(item.id)}
                  aria-label="Edit"
                />,
                <Popconfirm
                  key="del"
                  title="Delete template?"
                  onConfirm={async () => {
                    try {
                      await TemplatesService.templatesDelete({ id: item.id! });
                      notify('success', 'Template deleted');
                      load();
                    } catch (e: any) {
                      if (e instanceof Error) showError(e.message);
                    }
                  }}
                >
                  <Button icon={<DeleteOutlined />} aria-label="Delete" />
                </Popconfirm>,
              ]}
            >
              <List.Item.Meta
                title={item.name}
                description={<Typography.Text copyable>{item.token}</Typography.Text>}
              />
            </List.Item>
          )}
        />
      ) : (
        <Table
          rowKey="id"
          dataSource={templates}
          loading={loading}
          columns={columns}
          pagination={pagination}
        />
      )}
      {modalId && (
        <TemplateModal
          open={true}
          templateId={modalId === 'new' ? undefined : modalId}
          onClose={(changed) => {
            setModalId(undefined);
            if (changed) {
              load();
            }
          }}
        />
      )}
    </div>
  );
}
