import { useEffect, useState } from 'react';
import { Button, Card, InputNumber, Select, Space, Table, Tag, message, Statistic } from 'antd';
import { PropertiesService, SuitesService, TagsService } from '../../generated';

interface Failure {
  property: string;
  message: string;
  counterexample: unknown;
}

interface RunResult {
  trials: number;
  passed: number;
  failed: number;
  failures: Failure[];
}

export default function PropertyReport({ ruleId }: { ruleId: string }) {
  const [trials, setTrials] = useState(100);
  const [seed, setSeed] = useState<number | undefined>();
  const [loading, setLoading] = useState(false);
  const [data, setData] = useState<RunResult | null>(null);
  const [suite, setSuite] = useState<string | undefined>('property-fails');
  const [tags, setTags] = useState<string[]>(['property']);
  const [suites, setSuites] = useState<string[]>([]);
  const [tagOptions, setTagOptions] = useState<string[]>([]);
  const [selected, setSelected] = useState<React.Key[]>([]);

  useEffect(() => {
    (async () => {
      try {
        const [sRes, tRes] = await Promise.all([
          SuitesService.getApiV1Suites(),
          TagsService.getApiV1Tags(),
        ]);
        setSuites((sRes as any[]).map((s: any) => s.name));
        setTagOptions((tRes as any[]).map((t: any) => t.name));
      } catch {
        /* ignore */
      }
    })();
  }, []);

  const run = async () => {
    setLoading(true);
    try {
      const res = (await PropertiesService.postApiV1RulesPropertiesRun({
        ruleId,
        trials,
        seed,
      } as any)) as RunResult;
      setData(res);
      if (res.failed > 0) {
        message.warning(`${res.failed} properties failed.`);
      } else {
        message.success('All properties passed.');
      }
    } finally {
      setLoading(false);
    }
  };

  const importFailures = async () => {
    if (!data?.failures?.length || selected.length === 0) return;
    const picked = data.failures.filter((_, idx) => selected.includes(idx));
    await PropertiesService.postApiV1RulesPropertiesImportFailures({
      ruleId,
      requestBody: {
        failures: picked,
        suite: suite || undefined,
        tags: tags && tags.length > 0 ? tags : undefined,
      },
    } as any);
    message.success(`Imported ${picked.length} tests.`);
  };

  return (
    <Card
      title="Property Report"
      extra={
        <Space>
          <span>Trials</span>
          <InputNumber min={10} max={5000} value={trials} onChange={(v) => setTrials(Number(v))} />
          <span>Seed</span>
          <InputNumber value={seed as any} onChange={(v) => setSeed(v as number)} />
          <Button type="primary" onClick={run} loading={loading}>
            Run
          </Button>
        </Space>
      }
    >
      <Space style={{ marginBottom: 12 }}>
        <Statistic title="Trials" value={data?.trials ?? 0} />
        <Statistic title="Passed" value={data?.passed ?? 0} />
        <Statistic
          title="Failed"
          value={data?.failed ?? 0}
          valueStyle={{ color: (data?.failed ?? 0) > 0 ? 'red' : undefined }}
        />
      </Space>
      <Space style={{ marginBottom: 12 }} wrap>
        <span>Suite:</span>
        <Select
          style={{ minWidth: 200 }}
          value={suite}
          onChange={setSuite}
          showSearch
          options={[{ value: 'property-fails', label: 'property-fails' }, ...suites.map((s) => ({ value: s, label: s }))]}
        />
        <span>Tags:</span>
        <Select
          mode="tags"
          style={{ minWidth: 260 }}
          value={tags}
          onChange={(v) => setTags(v)}
          options={tagOptions.map((t) => ({ value: t, label: t }))}
          tokenSeparators={[',']}
          placeholder="Add or select tag"
        />
      </Space>
      <Table
        rowKey={(_, idx) => idx!}
        dataSource={data?.failures ?? []}
        rowSelection={{ selectedRowKeys: selected, onChange: setSelected }}
        columns={[
          { title: 'Property', dataIndex: 'property', render: (v: string) => <Tag color="red">{v}</Tag>, width: 180 },
          { title: 'Message', dataIndex: 'message' },
          {
            title: 'Input (preview)',
            dataIndex: 'counterexample',
            render: (v: unknown) => (
              <pre style={{ whiteSpace: 'pre-wrap', maxWidth: 480 }}>{JSON.stringify(v, null, 2)}</pre>
            ),
          },
        ]}
        locale={{ emptyText: 'No data' }}
      />
      <Space>
        <Button disabled={!data?.failures?.length} onClick={() => setSelected(data?.failures.map((_, idx) => idx) ?? [])}>
          Select all
        </Button>
        <Button type="primary" disabled={!selected.length} onClick={importFailures}>
          Import as tests
        </Button>
      </Space>
    </Card>
  );
}

