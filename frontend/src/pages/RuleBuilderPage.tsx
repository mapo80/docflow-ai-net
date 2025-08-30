import React from 'react';
import { Button, Card, Input, Select, Space, Table, List, Grid } from 'antd';
import Editor from '@monaco-editor/react';
import { useNavigate } from 'react-router-dom';
import { RuleBuilderService, PropertiesService, RulesService } from '../generated';
import { notify } from '../components/notification';

export type Block = {
  id: string;
  type: 'exists' | 'compare' | 'regex' | 'set' | 'normalize' | 'map' | 'deduce';
  field: string;
  op?: string;
  value?: any;
  pattern?: string;
  target?: string;
  kind?: 'number' | 'date';
  fn?: string;
  from?: string[];
};

export default function RuleBuilderPage() {
  const [blocks, setBlocks] = React.useState<Block[]>([]);
  const [code, setCode] = React.useState('// generated code will appear here');
  const [propResult, setPropResult] = React.useState<any | null>(null);
  const [name, setName] = React.useState('New Rule from Builder');
  const nav = useNavigate();
  const screens = Grid.useBreakpoint();
  const isMobile = !screens.md;

  function add(t: Block['type']) {
    setBlocks((b) => [
      ...b,
      { id: Math.random().toString(36).slice(2), type: t, field: '' },
    ]);
  }

  function remove(id: string) {
    setBlocks((b) => b.filter((x) => x.id !== id));
  }

  function update(id: string, patch: Partial<Block>) {
    setBlocks((b) => b.map((x) => (x.id === id ? { ...x, ...patch } : x)));
  }

  async function compile() {
    try {
      const data = await RuleBuilderService.postApiV1RulebuilderCompile({
        requestBody: { blocks: blocks as any },
      });
      setCode(data.code || '// no code');
    } catch {
      notify('error', 'Failed to compile blocks.');
    }
  }

  async function runProps() {
    try {
      const data = await PropertiesService.postApiV1RulesPropertiesRunFromBlocks({
        requestBody: { blocks: blocks as any, trials: 100 },
      });
      setPropResult(data);
    } catch {
      notify('error', 'Property check failed.');
    }
  }

  async function createRule() {
    try {
      const r = await RulesService.postApiV1Rules({
        requestBody: {
          name,
          description: 'Generated from builder',
          code,
          enabled: true,
        },
      });
      notify('success', 'Rule created successfully.');
      nav(`/rules/${r.id}`);
    } catch {
      notify('error', 'Failed to create rule.');
    }
  }

  return (
    <Card title="No-code Rule Builder">
      <Space style={{ marginBottom: 12 }} wrap>
        <Button onClick={() => add('exists')}>Add Exists</Button>
        <Button onClick={() => add('compare')}>Add Compare</Button>
        <Button onClick={() => add('regex')}>Add Regex</Button>
        <Button onClick={() => add('set')}>Add Set</Button>
        <Button onClick={() => add('normalize')}>Add Normalize</Button>
        <Button onClick={() => add('map')}>Add Map</Button>
        <Button onClick={() => add('deduce')}>Add Deduce</Button>
        <Button type="primary" onClick={compile}>
          Compile
        </Button>
        <Button onClick={runProps}>Property Check</Button>
        <Input
          placeholder="Rule name"
          value={name}
          onChange={(e) => setName(e.target.value)}
          style={{ width: 260 }}
        />
        <Button onClick={createRule}>Create Rule</Button>
      </Space>
      {isMobile ? (
        <List
          dataSource={blocks}
          rowKey="id"
          locale={{ emptyText: 'No blocks' }}
          renderItem={(r) => (
            <List.Item
              actions={[
                <Button danger onClick={() => remove(r.id)} key="rm">
                  Remove
                </Button>,
              ]}
            >
              <Space direction="vertical" style={{ width: '100%' }}>
                <Select
                  value={r.type}
                  onChange={(x) => update(r.id, { type: x })}
                  options={[
                    { value: 'exists', label: 'exists' },
                    { value: 'compare', label: 'compare' },
                    { value: 'regex', label: 'regex' },
                    { value: 'set', label: 'set' },
                    { value: 'normalize', label: 'normalize' },
                    { value: 'map', label: 'map' },
                    { value: 'deduce', label: 'deduce' },
                  ]}
                  style={{ width: '100%' }}
                />
                <Input
                  placeholder="field"
                  value={r.field}
                  onChange={(e) => update(r.id, { field: e.target.value })}
                />
                {r.type === 'compare' ? (
                  <Select
                    value={r.op}
                    onChange={(x) => update(r.id, { op: x })}
                    options={[
                      { value: '>', label: '>' },
                      { value: '>=', label: '>=' },
                      { value: '<', label: '<' },
                      { value: '<=', label: '<=' },
                      { value: '==', label: '==' },
                      { value: '!=', label: '!=' },
                    ]}
                    style={{ width: '100%' }}
                  />
                ) : null}
                {r.type === 'compare' ? (
                  <Input
                    placeholder="value"
                    value={r.value}
                    onChange={(e) => update(r.id, { value: e.target.value })}
                  />
                ) : r.type === 'regex' ? (
                  <Input
                    placeholder="pattern"
                    value={r.pattern}
                    onChange={(e) => update(r.id, { pattern: e.target.value })}
                  />
                ) : r.type === 'set' ? (
                  <Input
                    placeholder="target"
                    value={r.target}
                    onChange={(e) => update(r.id, { target: e.target.value })}
                  />
                ) : r.type === 'normalize' ? (
                  <Select
                    value={r.kind}
                    onChange={(x) => update(r.id, { kind: x })}
                    options={[
                      { value: 'number', label: 'number' },
                      { value: 'date', label: 'date' },
                    ]}
                    style={{ width: '100%' }}
                  />
                ) : r.type === 'map' ? (
                  <Input
                    placeholder="fn e.g. ((string)__x).ToUpper()"
                    value={r.fn}
                    onChange={(e) => update(r.id, { fn: e.target.value })}
                  />
                ) : r.type === 'deduce' ? (
                  <>
                    <Input
                      placeholder="target"
                      value={r.target}
                      onChange={(e) => update(r.id, { target: e.target.value })}
                    />
                    <Input
                      placeholder="field1,field2"
                      value={(r.from || []).join(',')}
                      onChange={(e) =>
                        update(r.id, {
                          from: e.target.value
                            .split(',')
                            .map((s: string) => s.trim())
                            .filter(Boolean),
                        })
                      }
                    />
                  </>
                ) : null}
              </Space>
            </List.Item>
          )}
        />
      ) : (
        <Table
          rowKey={(r: any) => r.id}
          dataSource={blocks}
          pagination={false}
          columns={[
            {
              title: 'Type',
              dataIndex: 'type',
              render: (_: any, r: any) => (
                <Select
                  value={r.type}
                  onChange={(x) => update(r.id, { type: x })}
                  options={[
                    { value: 'exists', label: 'exists' },
                    { value: 'compare', label: 'compare' },
                    { value: 'regex', label: 'regex' },
                    { value: 'set', label: 'set' },
                    { value: 'normalize', label: 'normalize' },
                    { value: 'map', label: 'map' },
                    { value: 'deduce', label: 'deduce' },
                  ]}
                  style={{ width: 120 }}
                />
              ),
            },
            {
              title: 'Field',
              dataIndex: 'field',
              render: (_: any, r: any) => (
                <Input
                  value={r.field}
                  onChange={(e) => update(r.id, { field: e.target.value })}
                />
              ),
            },
            {
              title: 'Op',
              dataIndex: 'op',
              render: (_: any, r: any) =>
                r.type === 'compare' ? (
                  <Select
                    value={r.op}
                    onChange={(x) => update(r.id, { op: x })}
                    options={[
                      { value: '>', label: '>' },
                      { value: '>=', label: '>=' },
                      { value: '<', label: '<' },
                      { value: '<=', label: '<=' },
                      { value: '==', label: '==' },
                      { value: '!=', label: '!=' },
                    ]}
                    style={{ width: 120 }}
                  />
                ) : null,
            },
            {
              title: 'Props',
              render: (_: any, r: any) =>
                r.type === 'compare' ? (
                  <Input
                    placeholder="value"
                    value={r.value}
                    onChange={(e) => update(r.id, { value: e.target.value })}
                  />
                ) : r.type === 'regex' ? (
                  <Input
                    placeholder="pattern"
                    value={r.pattern}
                    onChange={(e) => update(r.id, { pattern: e.target.value })}
                  />
                ) : r.type === 'set' ? (
                  <Input
                    placeholder="target"
                    value={r.target}
                    onChange={(e) => update(r.id, { target: e.target.value })}
                  />
                ) : r.type === 'normalize' ? (
                  <Select
                    value={r.kind}
                    onChange={(x) => update(r.id, { kind: x })}
                    options={[
                      { value: 'number', label: 'number' },
                      { value: 'date', label: 'date' },
                    ]}
                    style={{ width: 140 }}
                  />
                ) : r.type === 'map' ? (
                  <Input
                    placeholder="fn e.g. ((string)__x).ToUpper()"
                    value={r.fn}
                    onChange={(e) => update(r.id, { fn: e.target.value })}
                  />
                ) : r.type === 'deduce' ? (
                  <Input
                    placeholder="target"
                    value={r.target}
                    onChange={(e) => update(r.id, { target: e.target.value })}
                  />
                ) : null,
              width: 360,
            },
            {
              title: 'From',
              render: (_: any, r: any) =>
                r.type === 'deduce' ? (
                  <Input
                    placeholder="field1,field2"
                    value={(r.from || []).join(',')}
                    onChange={(e) =>
                      update(r.id, {
                        from: e.target.value
                          .split(',')
                          .map((s: string) => s.trim())
                          .filter(Boolean),
                      })
                    }
                  />
                ) : null,
            },
            {
              title: '',
              render: (_: any, r: any) => (
                <Button danger onClick={() => remove(r.id)}>
                  Remove
                </Button>
              ),
              width: 120,
            },
          ]}
        />
      )}
      <div style={{ marginTop: 12 }}>
        <Editor
          height={300}
          defaultLanguage="csharp"
          value={code}
          options={{ readOnly: true }}
        />
      </div>
      {propResult && (
        <Card type="inner" title="Property Check Result" style={{ marginTop: 12 }}>
          <pre style={{ margin: 0, whiteSpace: 'pre-wrap' }}>
            {JSON.stringify(propResult, null, 2)}
          </pre>
        </Card>
      )}
    </Card>
  );
}

