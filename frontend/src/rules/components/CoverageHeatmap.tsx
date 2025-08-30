import { useMemo, useState } from 'react';
import { Badge, Button, Flex, InputNumber, Modal, Radio, Space, Tooltip } from 'antd';

export type HeatCell = 'na' | 'notrun' | 'pass' | 'fail';

export function cellColor(c: HeatCell): string {
  switch (c) {
    case 'pass':
      return 'var(--ant-color-success-bg)';
    case 'fail':
      return 'var(--ant-color-error-bg)';
    case 'notrun':
      return 'var(--ant-color-warning-bg)';
    case 'na':
      return 'var(--ant-color-bg-container)';
  }
}

function Legend() {
  const items: { label: string; v: HeatCell }[] = [
    { label: 'Pass', v: 'pass' },
    { label: 'Fail', v: 'fail' },
    { label: 'Not run', v: 'notrun' },
    { label: 'Not asserted', v: 'na' },
  ];
  return (
    <Space wrap>
      {items.map((i) => (
        <Flex key={i.v} align="center" gap={8}>
          <div
            style={{
              width: 16,
              height: 16,
              background: cellColor(i.v),
              border: '1px solid var(--ant-color-border)',
            }}
          />
          <span>{i.label}</span>
        </Flex>
      ))}
    </Space>
  );
}

interface TestRunResult {
  diff?: Array<{ field: string }>;
}

export default function CoverageHeatmap({
  tests,
  results,
  onRunAll,
}: {
  tests: Array<{ id: string; name: string; expect?: any }>;
  results: Record<string, TestRunResult>;
  onRunAll: (opts?: { maxParallelism?: number }) => Promise<void>;
}) {
  const [maxParallelism, setPar] = useState<number>(4);
  const [drill, setDrill] = useState<{ test?: string; field?: string; status?: string } | null>(null);

  const [mode, setMode] = useState<'fields' | 'mutations'>('fields');

  const fields = useMemo(() => {
    const set = new Set<string>();
    tests.forEach((t) => {
      const exp = t.expect || {};
      const f = exp.fields || {};
      Object.keys(f).forEach((k) => set.add(k));
    });
    return Array.from(set).sort();
  }, [tests]);

  const matrix: HeatCell[][] = useMemo(() => {
    const rows: HeatCell[][] = [];
    for (const t of tests) {
      const row: HeatCell[] = [];
      const expMap = t.expect?.fields || {};
      const r = results[t.name];
      for (const f of fields) {
        if (!expMap[f]) {
          row.push('na');
          continue;
        }
        if (!r) {
          row.push('notrun');
          continue;
        }
        const failed = (r.diff || []).some((d) => d.field === f);
        row.push(failed ? 'fail' : 'pass');
      }
      rows.push(row);
    }
    return rows;
  }, [tests, results, fields]);

  const rowPassPct = useMemo(
    () =>
      matrix.map(
        (r) => Math.round((100 * r.filter((c) => c === 'pass').length) / (r.filter((c) => c !== 'na').length || 1)),
      ),
    [matrix],
  );
  const colPassPct = useMemo(
    () =>
      fields.map((_, ci) => {
        let ok = 0,
          total = 0;
        matrix.forEach((r) => {
          if (r[ci] !== 'na') {
            total++;
            if (r[ci] === 'pass') ok++;
          }
        });
        return Math.round((100 * ok) / (total || 1));
      }),
    [matrix, fields],
  );

  return (
    <>
      <div>
        <Flex justify="space-between" align="center" style={{ marginBottom: 12 }} wrap>
          <Space>
            <Badge status="processing" text="Coverage Heatmap" />
            <Legend />
          </Space>
          <Space wrap>
            <Radio.Group value={mode} onChange={(e) => setMode(e.target.value)}>
              <Radio.Button value="fields">By Fields</Radio.Button>
              <Radio.Button value="mutations" disabled>
                By Mutations
              </Radio.Button>
            </Radio.Group>
            <InputNumber
              min={1}
              max={64}
              value={maxParallelism}
              onChange={(v) => setPar(Number(v || 1))}
              addonBefore="Concurrency"
            />

            <Button
              onClick={() => {
                const rows: string[] = [];
                rows.push(['Test', 'Field', 'Status'].join(','));
                tests.forEach((t, ri) => {
                  fields.forEach((f, ci) => {
                    rows.push([
                      `"${t.name.replaceAll('"', '""')}"`,
                      `"${f.replaceAll('"', '""')}"`,
                      `${matrix[ri][ci]}`,
                    ].join(','));
                  });
                });
                const blob = new Blob([rows.join('\n')], { type: 'text/csv' });
                const url = URL.createObjectURL(blob);
                const a = document.createElement('a');
                a.href = url;
                a.download = 'coverage.csv';
                a.click();
                URL.revokeObjectURL(url);
              }}
            >
              Export Coverage CSV
            </Button>

            <Button
              type="primary"
              onClick={async () => {
                await onRunAll({ maxParallelism });
              }}
            >
              Run for Heatmap
            </Button>
          </Space>
        </Flex>

        <div style={{ overflow: 'auto', border: '1px solid #f0f0f0', borderRadius: 8 }}>
          <table style={{ borderCollapse: 'separate', borderSpacing: 0 }}>
            <thead>
              <tr>
                <th
                  style={{
                    position: 'sticky',
                    left: 0,
                    background: '#fafafa',
                    zIndex: 2,
                    padding: '8px 12px',
                    borderBottom: '1px solid #eee',
                  }}
                >
                  Test \\ Field
                </th>
                {fields.map((f) => (
                  <th
                    key={f}
                    title={f}
                    style={{
                      writingMode: 'vertical-rl',
                      transform: 'rotate(180deg)',
                      padding: 6,
                      borderBottom: '1px solid var(--ant-color-border)',
                      borderLeft: '1px solid var(--ant-color-border)',
                      fontSize: 12,
                    }}
                  >
                    {f}
                  </th>
                ))}
                <th
                  style={{
                    background: 'var(--ant-color-bg-container)',
                    padding: '8px 12px',
                    borderBottom: '1px solid var(--ant-color-border)',
                    position: 'sticky',
                    right: 0,
                    zIndex: 2,
                  }}
                >
                  Row %
                </th>
              </tr>
            </thead>
            <tbody>
              {tests.map((t, ri) => (
                <tr key={t.id}>
                  <td
                    style={{
                      position: 'sticky',
                      left: 0,
                      background: 'var(--ant-color-bg-container)',
                      zIndex: 1,
                      padding: '6px 12px',
                      borderRight: '1px solid var(--ant-color-border)',
                    }}
                  >
                    {t.name}
                  </td>
                  {fields.map((f, ci) => (
                    <td
                      key={t.id + '-' + f}
                      style={{
                        padding: 0,
                        borderLeft: '1px solid var(--ant-color-border)',
                        borderBottom: '1px solid var(--ant-color-border)',
                      }}
                    >
                      <Tooltip title={`${t.name} · ${f}: ${matrix[ri][ci]}`}>
                        <div
                          onClick={() => setDrill({ test: t.name, field: f, status: matrix[ri][ci] as any })}
                          style={{ cursor: 'pointer', width: 18, height: 18, background: cellColor(matrix[ri][ci] as HeatCell) }}
                        />
                      </Tooltip>
                    </td>
                  ))}
                  <td
                    style={{
                      position: 'sticky',
                      right: 0,
                      background: 'var(--ant-color-bg-container)',
                      padding: '0 8px',
                      textAlign: 'right',
                      fontVariantNumeric: 'tabular-nums',
                    }}
                  >
                    {rowPassPct[ri]}%
                  </td>
                </tr>
              ))}
            </tbody>
            <tfoot>
              <tr>
                <td
                  style={{
                    position: 'sticky',
                    left: 0,
                    background: 'var(--ant-color-bg-container)',
                    zIndex: 2,
                    padding: '8px 12px',
                    borderTop: '1px solid var(--ant-color-border)',
                  }}
                >
                  Col %
                </td>
              {fields.map((_, ci) => (
                <td
                  key={'col' + ci}
                  style={{
                    textAlign: 'center',
                    fontVariantNumeric: 'tabular-nums',
                    background: 'var(--ant-color-bg-container)',
                    borderTop: '1px solid var(--ant-color-border)',
                  }}
                  >
                    {colPassPct[ci]}%
                  </td>
                ))}
                <td
                  style={{
                    position: 'sticky',
                    right: 0,
                    background: 'var(--ant-color-bg-container)',
                  }}
                ></td>
              </tr>
            </tfoot>
          </table>
        </div>
      </div>

      <Modal
        open={!!drill}
        onCancel={() => setDrill(null)}
        footer={null}
        title={drill ? `${drill.test} • ${drill.field}` : ''}
      >
        <p>
          Outcome: <b>{drill?.status}</b>
        </p>
        <p style={{ opacity: 0.75 }}>Hint: click multiple cells to compare cases.</p>
      </Modal>
    </>
  );
}

