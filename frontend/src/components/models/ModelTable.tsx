import React from 'react';
import { Table, Button } from 'antd';
import type { ModelDto } from '@/services/modelsApi';

interface Props {
  data: ModelDto[];
  loading: boolean;
  onDownload: (id: string) => Promise<void>;
  onDelete: (id: string) => Promise<void>;
  onEdit: (model: ModelDto) => void;
}

const ModelTable: React.FC<Props> = ({ data, loading, onDownload, onDelete, onEdit }) => {
  const columns = [
    { title: 'Name', dataIndex: 'name', key: 'name' },
    {
      title: 'Type',
      dataIndex: 'type',
      key: 'type',
      render: (t: string) => (t === 'local' ? 'Local' : 'Hosted LLM'),
    },
    {
      title: 'Provider / HF Repo+File',
      key: 'provider',
      render: (_: any, r: ModelDto) =>
        r.type === 'local' ? `${r.hfRepo ?? ''}/${r.modelFile ?? ''}` : r.provider ?? '-',
    },
    {
      title: 'Downloaded',
      dataIndex: 'downloaded',
      key: 'downloaded',
      render: (d: boolean | null) => (d === null ? '–' : d ? 'Yes' : 'No'),
    },
    { title: 'Download Status', dataIndex: 'downloadStatus', key: 'downloadStatus' },
    {
      title: 'Actions',
      key: 'actions',
      render: (_: any, r: ModelDto) => (
        <div style={{ display: 'flex', gap: 8 }}>
          {r.type === 'local' && <Button onClick={() => onDownload(r.id)}>Download</Button>}
          <Button onClick={() => onEdit(r)}>Edit</Button>
          <Button danger onClick={() => onDelete(r.id)}>Delete</Button>
        </div>
      ),
    },
  ];

  return <Table rowKey="id" dataSource={data} loading={loading} columns={columns as any} pagination={false} />;
};

export default ModelTable;
