import React from 'react';
import { Table, Button, Tag } from 'antd';
import type { ModelDto } from '@/services/modelsApi';

interface Props {
  data: ModelDto[];
  loading: boolean;
  onDownload: (id: string) => Promise<void>;
  onActivate: (id: string) => Promise<void>;
  onDelete: (id: string) => Promise<void>;
}

const ModelTable: React.FC<Props> = ({ data, loading, onDownload, onActivate, onDelete }) => {
  const columns = [
    { title: 'Name', dataIndex: 'name', key: 'name' },
    { title: 'Status', dataIndex: 'status', key: 'status', render: (s: string) => <Tag>{s}</Tag> },
    {
      title: 'Actions',
      key: 'actions',
      render: (_: any, r: ModelDto) => (
        <div style={{ display: 'flex', gap: 8 }}>
          <Button onClick={() => onDownload(r.id)}>Download</Button>
          <Button onClick={() => onActivate(r.id)}>Activate</Button>
          <Button danger onClick={() => onDelete(r.id)}>Delete</Button>
        </div>
      ),
    },
  ];

  return <Table rowKey="id" dataSource={data} loading={loading} columns={columns as any} pagination={false} />;
};

export default ModelTable;
