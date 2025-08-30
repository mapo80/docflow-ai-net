import { useEffect, useState } from 'react';
import {
  Badge,
  Button,
  Card,
  Drawer,
  Input,
  Modal,
  Progress,
  Space,
  Statistic,
  Table,
  Tag,
  message,
} from 'antd';
import Editor from '@monaco-editor/react';
import CoverageHeatmap from './CoverageHeatmap';
import { RuleTestsService } from '../../generated/services/RuleTestsService';

interface TestRow {
  id: string;
  name: string;
  expect: any;
}

interface TestRunResult {
  name: string;
  passed?: boolean;
  durationMs?: number;
  actual?: any;
  diff?: Array<{ field: string; rule: string }>;
  error?: string;
  logs?: string[];
}

export default function RuleTestsPanel({ ruleId }: { ruleId: string }) {
  const [tests, setTests] = useState<TestRow[]>([]);
  const [results, setResults] = useState<Record<string, TestRunResult>>({});
  const [selectedRowKeys, setSelectedRowKeys] = useState<React.Key[]>([]);
  const [concurrency, setConcurrency] = useState(4);
  const [testJson, setTestJson] = useState(
    '{"name":"Sample","input":{"fields":{}},"expect":{"fields":{}}}'
  );
  const [heatOpen, setHeatOpen] = useState(false);
  const [loading, setLoading] = useState(false);

  const load = async () => {
    const res: any = await RuleTestsService.getApiV1RulesTests({
      ruleId,
      page: 1,
      pageSize: 100,
    });
    const items = (res.items || []).map((t: any) => ({
      id: t.id,
      name: t.name,
      expect: JSON.parse(t.expectJson || '{}'),
    }));
    setTests(items);
  };

  useEffect(() => {
    load();
  }, [ruleId]);

  const runAll = async () => {
    setLoading(true);
    try {
      const res = await RuleTestsService.postApiV1RulesTestsRun({ ruleId });
      const map: Record<string, TestRunResult> = {};
      res.forEach((r: any) => (map[r.name] = r));
      setResults(map);
      message.success('All tests executed');
    } finally {
      setLoading(false);
    }
  };

  const runSelected = async () => {
    if (!selectedRowKeys.length) return;
    setLoading(true);
    try {
      const ids = tests
        .filter((t) => selectedRowKeys.includes(t.id))
        .map((t) => t.id);
      const res = await RuleTestsService.postApiV1RulesTestsRunSelected({
        ruleId,
        requestBody: { ids },
      });
      const map = { ...results };
      res.forEach((r: any) => (map[r.name] = r));
      setResults(map);
      message.success('Selected tests executed');
    } finally {
      setLoading(false);
    }
  };

  const rerunFailed = async () => {
    const failedIds = tests
      .filter((t) => results[t.name]?.passed === false)
      .map((t) => t.id);
    if (!failedIds.length) return;
    setLoading(true);
    try {
      const res = await RuleTestsService.postApiV1RulesTestsRunSelected({
        ruleId,
        requestBody: { ids: failedIds },
      });
      const map = { ...results };
      res.forEach((r: any) => (map[r.name] = r));
      setResults(map);
      message.success('Failed tests re-executed');
    } finally {
      setLoading(false);
    }
  };

  const addTest = async () => {
    try {
      const obj = JSON.parse(testJson);
      await RuleTestsService.postApiV1RulesTests({
        ruleId,
        requestBody: {
          name: obj.name,
          input: obj.input,
          expect: obj.expect,
          suite: obj.suite,
          tags: obj.tags,
          priority: obj.priority,
        },
      });
      message.success('Test added');
      await load();
    } catch (e: any) {
      message.error(e.message || 'Invalid JSON');
    }
  };

  const computeCoverage = async () => {
    const data = await RuleTestsService.getApiV1RulesTestsCoverage({ ruleId });
    Modal.info({
      title: 'Fields Coverage',
      content: (
        <Table
          size="small"
          pagination={false}
          rowKey="field"
          dataSource={data as any}
          columns={[
            { title: 'Field', dataIndex: 'field' },
            { title: 'Tested', dataIndex: 'tested' },
            { title: 'Mutated', dataIndex: 'mutated' },
            { title: 'Hits', dataIndex: 'hits' },
            { title: 'Pass', dataIndex: 'pass' },
          ]}
        />
      ),
    });
  };

  const columns = [
    {
      title: 'Status',
      dataIndex: 'status',
      render: (_: any, row: TestRow) => {
        const r = results[row.name];
        if (!r) return <Tag>not run</Tag>;
        return r.passed ? <Tag color="green">passed</Tag> : <Tag color="red">failed</Tag>;
      },
    },
    { title: 'Name', dataIndex: 'name' },
  ];

  const stats = {
    total: tests.length,
    passed: Object.values(results).filter((r) => r.passed).length,
    failed: Object.values(results).filter((r) => r.passed === false).length,
  };

  return (
    <Card
      title={<Badge status="processing" text="Unit Tests" />}
      extra={
        <Space wrap>
          <Button onClick={() => setHeatOpen(true)}>Heatmap</Button>
          <Input
            type="number"
            value={concurrency}
            onChange={(e) => setConcurrency(parseInt(e.target.value || '1'))}
            style={{ width: 120 }}
            prefix="Conc."
          />
          <Button onClick={runAll} loading={loading} type="primary">
            Run All
          </Button>
          <Button onClick={runSelected} loading={loading}>
            Run Selected
          </Button>
          <Button onClick={rerunFailed} loading={loading} danger>
            Re-run Failed
          </Button>
        </Space>
      }
    >
      <Space style={{ marginBottom: 12 }}>
        <Statistic title="Total" value={stats.total} />
        <Statistic title="Passed" value={stats.passed} valueStyle={{ color: '#3f8600' }} />
        <Statistic title="Failed" value={stats.failed} valueStyle={{ color: '#cf1322' }} />
        {loading ? <Progress percent={70} status="active" style={{ minWidth: 200 }} /> : null}
      </Space>
      <Table<TestRow>
        rowKey="id"
        dataSource={tests}
        columns={columns}
        rowSelection={{ selectedRowKeys, onChange: setSelectedRowKeys }}
        pagination={false}
        expandable={{
          expandedRowRender: (row) => {
            const r = results[row.name];
            if (!r) return <i>Not executed yet</i>;
            if (r.error) return <pre style={{ whiteSpace: 'pre-wrap' }}>{r.error}</pre>;
            return (
              <div>
                {r.logs && r.logs.length ? (
                  <pre style={{ whiteSpace: 'pre-wrap' }}>{r.logs.join('\n')}</pre>
                ) : null}
                <pre style={{ whiteSpace: 'pre-wrap' }}>{JSON.stringify(r.actual, null, 2)}</pre>
              </div>
            );
          },
        }}
      />
      <Card type="inner" title="New Test" style={{ marginTop: 16 }}>
        <Editor
          height="26vh"
          defaultLanguage="json"
          value={testJson}
          onChange={(v) => setTestJson(v || '')}
          options={{ minimap: { enabled: false } }}
        />
        <Space style={{ marginTop: 8 }}>
          <Button onClick={addTest}>Add Test</Button>
          <Button onClick={load}>Refresh</Button>
          <Button onClick={computeCoverage}>Compute Coverage</Button>
        </Space>
      </Card>
      <Drawer
        title="Coverage Heatmap"
        open={heatOpen}
        onClose={() => setHeatOpen(false)}
        width={"85%"}
      >
        <CoverageHeatmap
          tests={tests}
          results={results}
          onRunAll={async () => {
            setLoading(true);
            try {
              const res = await RuleTestsService.postApiV1RulesTestsRun({ ruleId });
              const map: Record<string, TestRunResult> = {};
              res.forEach((r: any) => (map[r.name] = r));
              setResults(map);
            } finally {
              setLoading(false);
            }
          }}
        />
      </Drawer>
    </Card>
  );
}
