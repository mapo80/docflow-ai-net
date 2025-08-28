import { Table, Tooltip } from 'antd';
import type { ExtractedField } from '../adapters/extractionAdapter';
import { useCallback, type Key } from 'react';

interface Props {
  docType: 'pdf' | 'image';
  fields: ExtractedField[];
  selectedFieldId?: string;
  onFieldSelect: (id: string) => void;
}

export default function FieldsTable({ docType, fields, selectedFieldId, onFieldSelect }: Props) {
  const onRow = useCallback(
    (record: ExtractedField) => ({
      onClick: () => onFieldSelect(record.id),
      'data-testid': `row-${record.id}`,
      style: { cursor: 'pointer' },
    }),
    [onFieldSelect],
  );

  const handleKey = (e: React.KeyboardEvent) => {
    if (!['ArrowDown', 'ArrowUp'].includes(e.key)) return;
    e.preventDefault();
    const idx = fields.findIndex((f) => f.id === selectedFieldId);
    const next = e.key === 'ArrowDown' ? idx + 1 : idx - 1;
    if (next >= 0 && next < fields.length) {
      onFieldSelect(fields[next].id);
    }
  };

  return (
    <div data-testid="fields-table" onKeyDown={handleKey} tabIndex={0}>
      <Table
        dataSource={fields}
        rowKey={(f) => f.id as Key}
        columns={[
          { title: 'Name', dataIndex: 'name' },
          { title: 'Value', dataIndex: 'value' },
          { title: 'Confidence', dataIndex: 'conf' },
          {
            title: 'Page',
            dataIndex: 'page',
            render: (value: number) => (docType === 'pdf' ? value : '-'),
          },
          {
            title: 'BBox',
            dataIndex: 'hasBbox',
            render: (v: boolean) => (
              <Tooltip title={v ? 'Bounding box available' : 'Bounding box unavailable'}>
                {v ? '✅' : '❌'}
              </Tooltip>
            ),
          },
        ]}
        pagination={false}
        size="small"
        rowClassName={(record) =>
          record.id === selectedFieldId ? 'selected-row' : ''
        }
        onRow={onRow as any}
        scroll={{ x: true }}
      />
    </div>
  );
}
