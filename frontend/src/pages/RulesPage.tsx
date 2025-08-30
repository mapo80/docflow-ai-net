import { useEffect, useState } from 'react';
import { Button, Card, Input, Space, Table, List, Grid } from 'antd';
import { Link, useNavigate } from 'react-router-dom';
import { RulesService } from '../generated';
import { notify } from '../components/notification';

interface RuleRow {
  id: string;
  name: string;
  version: number;
  enabled: boolean;
}

export default function RulesPage() {
  const [rows, setRows] = useState<RuleRow[]>([]);
  const [search, setSearch] = useState('');
  const nav = useNavigate();
  const screens = Grid.useBreakpoint();
  const isMobile = !screens.md;

  const load = async () => {
    try {
      const data = await RulesService.getApiV1Rules({ search });
      setRows(data as RuleRow[]);
    } catch {
      notify('error', 'Failed to load rules.');
    }
  };

  useEffect(() => {
    void load();
  }, [search]);

  const clone = async (row: RuleRow) => {
    try {
      const res = await RulesService.postApiV1RulesClone({
        id: row.id,
        requestBody: { name: `${row.name} (copy)`, withTests: true },
      } as any);
      notify('success', 'Rule cloned successfully.');
      nav(`/rules/${(res as any).id}`);
    } catch {
      notify('error', 'Failed to clone rule.');
    }
  };

  return (
    <Card title="Rules">
      <Space style={{ marginBottom: 12 }}>
        <Input
          placeholder="search"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
        />
        <Button onClick={() => nav('/rules/builder')}>No-code Builder</Button>
      </Space>
      {isMobile ? (
        <List
          dataSource={rows}
          rowKey="id"
          renderItem={(r) => (
            <List.Item
              actions={[
                <Button key="open" onClick={() => nav(`/rules/${r.id}`)}>
                  Open
                </Button>,
                <Button key="clone" onClick={() => clone(r)}>
                  Clone + tests
                </Button>,
              ]}
            >
              <List.Item.Meta
                title={<Link to={`/rules/${r.id}`}>{r.name}</Link>}
                description={`Version: ${r.version} • Enabled: ${r.enabled}`}
              />
            </List.Item>
          )}
          locale={{ emptyText: 'No data' }}
        />
      ) : (
        <Table
          rowKey={(r) => r.id}
          dataSource={rows}
          columns={[
            { title: 'Name', dataIndex: 'name', render: (v, r) => <Link to={`/rules/${r.id}`}>{v}</Link> },
            { title: 'Version', dataIndex: 'version', width: 120 },
            { title: 'Enabled', dataIndex: 'enabled', width: 100 },
            {
              title: '',
              width: 280,
              render: (_: unknown, r: RuleRow) => (
                <Space>
                  <Button onClick={() => nav(`/rules/${r.id}`)}>Open</Button>
                  <Button onClick={() => clone(r)}>Clone + tests</Button>
                </Space>
              ),
            },
          ]}
          locale={{ emptyText: 'No data' }}
        />
      )}
    </Card>
  );
}
