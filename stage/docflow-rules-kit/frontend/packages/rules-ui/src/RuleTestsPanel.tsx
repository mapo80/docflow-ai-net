import { useEffect, useMemo, useState } from 'react'
import { Badge, Button, Card, Checkbox, Col, Descriptions, Divider, Drawer, Flex, Form, Input, Modal, Progress, Row, Select, Space, Statistic, Table, Tag, Tooltip, message } from 'antd'
import type { ColumnsType } from 'antd/es/table'
import Editor from '@monaco-editor/react'
import dayjs from 'dayjs'
import CoverageHeatmap from './CoverageHeatmap'
import { EditorSyncBadge } from './RulesEditor'
import type { RulesClient, TestRunResult, TestCase } from '@docflow/rules-client'

type TestRow = { id: string; name: string; updatedAt?: string; suite?: string; tags?: string[]; priority?: number; expect?: any; input?: any }
type ResultMap = Record<string, TestRunResult & { id?: string }>

const PRIORITY_COLORS: Record<number, string> = { 1: 'red', 2: 'volcano', 3: 'blue', 4: 'geekblue', 5: 'purple' }

function FieldDiffTable({ expect, actual, diffs }: { expect: any, actual: any, diffs: any[] }) {
  const rows: { field: string; rule: string; expected: any; actual: any; tol?: any; status: 'pass'|'fail' }[] = []
  const failed = new Set<string>((diffs || []).map((d:any) => `${d.field}|${d.rule}`))

  const checks = expect?.fields || {}
  for (const field of Object.keys(checks)) {
    const cks = checks[field]
    for (const rule of Object.keys(cks)) {
      if (rule === 'tol') continue
      const key = `${field}|${rule}`
      const isFail = failed.has(key)
      const expected = cks[rule]
      const tol = cks['tol']
      const actVal = actual?.[field]
      rows.push({ field, rule, expected, actual: actVal, tol, status: isFail ? 'fail' : 'pass' })
    }
  }

  return (
    <Table
      size="small"
      rowKey={(r)=>r.field + r.rule}
      dataSource={rows}
      pagination={false}
      columns={[
        { title: 'Field', dataIndex: 'field', width: 160 },
        { title: 'Rule', dataIndex: 'rule', width: 120, render: (v)=> <Tag>{v}</Tag> },
        { title: 'Expected', dataIndex: 'expected', render: (v)=> <code>{typeof v === 'object' ? JSON.stringify(v) : String(v)}</code> },
        { title: 'Actual', dataIndex: 'actual', render: (v)=> <code>{typeof v === 'object' ? JSON.stringify(v) : String(v)}</code> },
        { title: 'Tol', dataIndex: 'tol', width: 90, render: (v)=> v!==undefined ? <code>{String(v)}</code> : '-' },
        { title: 'Status', dataIndex: 'status', width: 110, render: (s)=> s==='pass' ? <Tag color="green">pass</Tag> : <Tag color="red">fail</Tag> }
      ]}
    />
  )
}

export function RuleTestsPanel({ client, ruleId, persistUrl=false }: { client: RulesClient, ruleId: string, persistUrl?: boolean }) {
  const [tests, setTests] = useState<TestRow[]>([])
  const [allTags, setAllTags] = useState<any[]>([])
  const [testsPage, setTestsPage] = useState(1)
  const [testsTotal, setTestsTotal] = useState(0)
  const [pageSize, setPageSize] = useState(20)
  const [results, setResults] = useState<ResultMap>({})
  const [selectedRowKeys, setSelected] = useState<React.Key[]>([])
  const [filter, setFilter] = useState<'all'|'passed'|'failed'|'norun'>('all')
  const [search, setSearch] = useState('')
  const [selectedTags, setSelectedTags] = useState<string[]>([])
  const [tagsMode, setTagsMode] = useState<'and'|'or'>('or')
  const [busy, setBusy] = useState(false)
  const [sortBy, setSortBy] = useState<string>('name')
  const [aiList, setAiList] = useState<any[]>([])
  const [aiUsage, setAiUsage] = useState<{ inputTokens:number; outputTokens:number; durationMs:number; costUsd:number }|null>(null)
  const [aiModel, setAiModel] = useState<string>('')
  const [aiTotalSkel, setAiTotalSkel] = useState<number>(0)
  const [aiSel, setAiSel] = useState<React.Key[]>([])
  const [aiPrompt, setAiPrompt] = useState('')
  const [aiBudget, setAiBudget] = useState<number>(10)
  const [aiTemp, setAiTemp] = useState<number>(0.2)
  const [aiBusy, setAiBusy] = useState(false)
  const [sortDir, setSortDir] = useState<'asc'|'desc'>('asc')
  const [sorters, setSorters] = useState<any[]>([])
  const [concurrency, setConcurrency] = useState<number>(4)
  const [testJson, setTestJson] = useState<string>('{"name":"Sample","input":{"fields":{}},"expect":{"fields":{}},"suite":"default","tags":["smoke"],"priority":3}')
  const [heatOpen, setHeatOpen] = useState(false)

  const [editOpen, setEditOpen] = useState(false)
  const [editRow, setEditRow] = useState<TestRow | null>(null)
  const [form] = Form.useForm()

  const load = async () => {
    const res = await client.listTests(ruleId, { search, page: testsPage, pageSize, sortBy, sortDir, sort: sorters.length? sorters.map((s:any)=>`${(s.field==='Updated'?'updatedAt':(s.field==='Prio'?'priority':s.field?.toLowerCase()))}:${s.order==='descend'?'desc':'asc'}`).join(',') : undefined, tags: selectedTags, tagsMode }) as any; const list = res.items as any[]; setTestsTotal(res.total)
    const mapped = list.map(t => ({
      id: t.id, name: t.name, updatedAt: t.updatedAt,
      suite: t.suite, tags: t.tags || [], priority: t.priority || 3,
      expect: JSON.parse(t.expectJson || '{}'), input: JSON.parse(t.inputJson || '{}')
    }))
    setTests(mapped)
  }
  useEffect(()=>{

  useEffect(()=>{
    (async()=>{
      try {
        // try through client if available
        const anyClient = client as any;
        if (anyClient.listTags) {
          const t = await anyClient.listTags();
          setAllTags(t as any[]);
        } else {
          const res = await fetch('/api/tags'); const t = await res.json(); setAllTags(t);
        }
      } catch {}
    })();
  }, [])


  useEffect(()=>{

  useEffect(()=>{
    (async()=>{
      try {
        // try through client if available
        const anyClient = client as any;
        if (anyClient.listTags) {
          const t = await anyClient.listTags();
          setAllTags(t as any[]);
        } else {
          const res = await fetch('/api/tags'); const t = await res.json(); setAllTags(t);
        }
      } catch {}
    })();
  }, [])

    if (!persistUrl) return;
    const url = new URL(window.location.href);
    const s = url.searchParams.get('t.search'); if (s) setSearch(s);
    const p = url.searchParams.get('t.page'); if (p) setTestsPage(parseInt(p));
    const sb = url.searchParams.get('t.sort'); if (sb) { setSortBy(''); setSortDir('asc'); setSorters(sb.split(',').map(x => { const [f,d]=x.split(':'); return { field: f, order: d==='desc'?'descend':'ascend' } as any })) }
  }, [])
  useEffect(()=>{

  useEffect(()=>{
    (async()=>{
      try {
        // try through client if available
        const anyClient = client as any;
        if (anyClient.listTags) {
          const t = await anyClient.listTags();
          setAllTags(t as any[]);
        } else {
          const res = await fetch('/api/tags'); const t = await res.json(); setAllTags(t);
        }
      } catch {}
    })();
  }, [])

    if (!persistUrl) return;
    const url = new URL(window.location.href);
    url.searchParams.set('t.search', search||''); url.searchParams.set('t.page', String(testsPage));
    if (sorters.length>0) url.searchParams.set('t.sort', sorters.map((s:any)=>`${s.field}:${s.order==='descend'?'desc':'asc'}`).join(','));
    window.history.replaceState({}, '', url.toString());
  }, [search, testsPage, sorters])

   load() }, [ruleId])

  const suites = useMemo(()=> Array.from(new Set(tests.map(t => t.suite).filter(Boolean))), [tests])
  const tags = useMemo(()=> {
    const all = new Set<string>()
    tests.forEach(t => (t.tags||[]).forEach((x:string)=>all.add(x)))
    return Array.from(all)
  }, [tests])

  const stats = useMemo(() => {
    const all = tests.length
    const res = Object.values(results)
    const passed = res.filter(r => r.passed).length
    const failed = res.filter(r => r.passed === false).length
    return { all, passed, failed, norun: all - res.length }
  }, [tests, results])

  const [suiteFilter, setSuiteFilter] = useState<string | null>(null)
  const [tagFilter, setTagFilter] = useState<string[]>([])

  const filtered = useMemo(() => {
    return tests.filter(t => {
      const r = results[t.name]
      if (search && !t.name.toLowerCase().includes(search.toLowerCase())) return false
      if (suiteFilter && t.suite !== suiteFilter) return false
      if (tagFilter.length && !(t.tags||[]).some(x => tagFilter.includes(x))) return false
      if (filter === 'passed') return !!r && r.passed
      if (filter === 'failed') return !!r && r.passed === false
      if (filter === 'norun') return !r
      return true
    })
  }, [tests, results, filter, search, suiteFilter, tagFilter])

  const columns: ColumnsType<TestRow> = [
    {
      title: 'Status',
      dataIndex: 'status',
      width: 110,
      render: (_: any, row: TestRow) => {
        const r = results[row.name]
        if (!r) return <Tag>not run</Tag>
        return r.passed ? <Tag color="green">passed</Tag> : <Tag color="red">failed</Tag>
      }
    },
    { title: 'Name', dataIndex: 'name', sorter: { multiple: 1 } } ,
    { title: 'Suite', dataIndex: 'suite', width: 140, render: (v) => v ? <Tag>{v}</Tag> : '-' },
    { title: 'Tags', dataIndex: 'tags', sorter: { multiple: 3 }, render: (v:string[]) => (v||[]).map(x => <Tag key={x}>{x}</Tag>) },
    {
      title: 'Prio',
      dataIndex: 'priority',
      width: 90,
      render: (p:number = 3) => <Tag color={PRIORITY_COLORS[p] || 'blue'}>P{p}</Tag>
    },
    {
      title: 'Duration',
      width: 110,
      render: (_: any, row: TestRow) => {
        const r = results[row.name]
        return r ? <span>{r.durationMs} ms</span> : '-'
      }
    },
    {
      title: 'Updated',
      width: 180,
      render: (_: any, row: TestRow) => dayjs(row.updatedAt).format('YYYY-MM-DD HH:mm')
    },
    {
      title: 'Actions',
      width: 140,
      render: (_: any, row: TestRow) => <Space>
        <Button size="small" onClick={() => {
          const r = results[row.name]
          if (!r) return message.info('Esegui prima il test')
          const failedOnly = (r.diff||[]).length
          message.info(failedOnly ? `${failedOnly} checks failed` : 'Tutti i checks passed')
        }}>Info</Button>
        <Button size="small" onClick={() => {
          setEditRow(row)
          form.setFieldsValue({ name: row.name, suite: row.suite, tags: row.tags, priority: row.priority })
          setEditOpen(true)
        }}>Edit</Button>
      </Space>
    }
  ]

  const onRunAll = async () => {
    setBusy(true)
    try {
      const res = await client.runTests(ruleId, { maxParallelism: concurrency })
      const map: ResultMap = {}
      res.forEach(r => map[r.name] = r)
      setResults(map)
      message.success('All tests executed')
    } finally { setBusy(false) }
  }

  const onRunSelected = async () => {
    if (!selectedRowKeys.length) return message.info('Seleziona almeno un test')
    setBusy(true)
    try {
      const ids = tests.filter(t => selectedRowKeys.includes(t.id)).map(t => t.id)
      const res = await client.runSelectedTests(ruleId, ids, { maxParallelism: concurrency })
      const map: ResultMap = { ...results }
      res.forEach(r => map[r.name] = r)
      setResults(map)
      message.success('Selected tests executed')
    } finally { setBusy(false) }
  }

  const onRerunFailed = async () => {
    const failed = Object.values(results).filter(r => r.passed === false)
    if (!failed.length) return message.info('Nessun test fallito da rieseguire')
    const failedIds = tests.filter(t => failed.find(f => f.name === t.name)).map(t => t.id)
    setBusy(true)
    try {
      const res = await client.runSelectedTests(ruleId, failedIds, { maxParallelism: concurrency })
      const map: ResultMap = { ...results }
      res.forEach(r => map[r.name] = r)
      setResults(map)
      message.success('Failed tests re-executed')
    } finally { setBusy(false) }
  }

  const onSaveMeta = async () => {
    const vals = await form.validateFields()
    if (!editRow) return
    await client.updateTest(ruleId, editRow.id, { name: vals.name, suite: vals.suite, tags: vals.tags, priority: vals.priority })
    setEditOpen(false); setEditRow(null); await load()
  }

  return (
    <Card
      title={<Space align="center">
        <Badge status="processing" text="Unit Tests" />
      </Space>}
      extra={<Space wrap>
        <Input.Search placeholder="Cerca test..." allowClear onSearch={setSearch} onChange={e=>setSearch(e.target.value)} style={{ width: 220 }} />
        <Select
          placeholder="Suite"
          allowClear
          style={{ width: 160 }}
          options={suites.map(s => ({ value: s!, label: s }))}
          onChange={setSuiteFilter}
          value={suiteFilter || undefined}
        />
        <Select
          mode="multiple"
          placeholder="Tags"
          allowClear
          style={{ width: 240 }}
          options={tags.map(t => ({ value: t, label: t }))}
          onChange={setTagFilter}
          value={tagFilter}
        />
        <Tag color={filter==='all'?'blue':undefined} onClick={()=>setFilter('all')} style={{cursor:'pointer'}}>All {stats.all}</Tag>
        <Tag color={filter==='passed'?'green':undefined} onClick={()=>setFilter('passed')} style={{cursor:'pointer'}}>Passed {stats.passed}</Tag>
        <Tag color={filter==='failed'?'red':undefined} onClick={()=>setFilter('failed')} style={{cursor:'pointer'}}>Failed {stats.failed}</Tag>
        <Tag color={filter==='norun'?'gold':undefined} onClick={()=>setFilter('norun')} style={{cursor:'pointer'}}>Not run {stats.norun}</Tag>
      </Space>}
      style={{ marginTop: 16 }}
    >
      <Flex gap={16} align="center" style={{ marginBottom: 12 }} wrap="wrap">
        <Space style={{ flexWrap: 'wrap' }}>
          <EditorSyncBadge saving={busy} synced={!busy} />
          <Button onClick={()=>setHeatOpen(true)}>Heatmap</Button>
          <Input type='number' value={concurrency} onChange={e=>setConcurrency(parseInt(e.target.value||'1'))} style={{ width: 120 }} prefix='Conc.' />
          <Button onClick={onRunAll} loading={busy} type="primary">Run All</Button>
          <Button onClick={onRunSelected} loading={busy}>Run Selected</Button>
          <Button onClick={onRerunFailed} loading={busy} danger>Re-run Failed</Button>
        </Space>
        <Space>
          <Statistic title="Total" value={stats.all} />
          <Statistic title="Passed" value={stats.passed} valueStyle={{ color: '#3f8600' }} />
          <Statistic title="Failed" value={stats.failed} valueStyle={{ color: '#cf1322' }} />
        </Space>
        <div style={{flex:1}} />
        {busy ? <Progress percent={70} status="active" style={{ minWidth: 200 }} /> : null}
      </Flex>

      {aiUsage && <div style={{ marginBottom: 12, opacity: 0.85 }}>Model: <b>{aiModel}</b> • Prompt tokens: <b>{aiUsage.inputTokens}</b> • Completion tokens: <b>{aiUsage.outputTokens}</b> • Duration: <b>{aiUsage.durationMs}ms</b> • Cost: <b>${aiUsage.costUsd?.toFixed?.(4)}</b></div>}
      <Table<TestRow>
        rowKey="id"
        pagination={{ current: testsPage, total: testsTotal, pageSize, onChange: (p, ps)=>{ setTestsPage(p); setPageSize(ps); setTimeout(load, 0) } }}
        dataSource={filtered}
        columns={columns}
        rowSelection={{ selectedRowKeys, onChange: setSelected }}
        onChange={(pagination, filters, sorter:any)=>{
          const arr = Array.isArray(sorter) ? sorter : (sorter?.field ? [sorter] : [])
          setSorters(arr)
          if (arr.length>0) {
            const s0 = arr[0]
            const f = String(s0.field)
            setSortBy(f === 'Updated' ? 'updatedAt' : f.toLowerCase())
            setSortDir(s0.order === 'descend' ? 'desc' : 'asc')
          }
          setTimeout(load, 0)
        }}
        expandable={{
          expandedRowRender: (row) => {
            const r = results[row.name]
            if (!r) return <i>Not executed yet</i>
            const expect = row.expect || {}
            return (
              <div style={{ padding: 8 }}>
                {r.error && <>
                  <Tag color="red">Error</Tag>
                  <pre style={{ whiteSpace: 'pre-wrap' }}>{r.error}</pre>
                </>}
                <Divider orientation="left">Field-by-field checks</Divider>
                <FieldDiffTable expect={expect} actual={r.actual || {}} diffs={r.diff || []} />
                {r.logs && r.logs.length ? <>
                  <Divider orientation="left">Logs</Divider>
                  <pre style={{ maxHeight: 150, overflow: 'auto' }}>{r.logs.join('\n')}</pre>
                </> : null}
              </div>
            )
          }
        }}
      />

      <Card type="inner" title="New Test" style={{ marginTop: 16 }}>
        <Editor height="26vh" defaultLanguage="json" value={testJson} onChange={v=>setTestJson(v||'')} options={{ minimap: { enabled: false } }} />
        <Space style={{ marginTop: 8 }}>
          <Button onClick={async ()=>{
            try {
              const obj = JSON.parse(testJson)
              await client.addTest(ruleId, { name: obj.name || 'Test', input: obj.input || {}, expect: obj.expect || {}, suite: obj.suite, tags: obj.tags, priority: obj.priority })
              message.success('Test added'); await load()
            } catch (e:any) { message.error(e.message || 'Invalid JSON') }
          }}>Add Test</Button>
          <Button onClick={load}>Refresh</Button>
        </Space>
      </Card>

      <Drawer title={"Edit Test"} open={editOpen} onClose={()=>setEditOpen(false)} width={420}
        footer={<Space><Button onClick={()=>setEditOpen(false)}>Cancel</Button><Button type="primary" onClick={onSaveMeta}>Save</Button></Space>}>
        <Form form={form} layout="vertical">
          <Form.Item name="name" label="Name" rules={[{ required: true, message: 'Name is required' }]}>
            <Input />
          </Form.Item>
          <Form.Item name="suite" label="Suite">
            <Input placeholder="es. smoke / regression / default" />
          </Form.Item>
          <Form.Item name="tags" label="Tags">
            <Select mode="tags" placeholder="Aggiungi tags" />
          </Form.Item>
          <Form.Item name="priority" label="Priority">
            <Select options={[1,2,3,4,5].map(p=>({value:p,label:`P${p}`}))} />
          </Form.Item>
        </Form>
      </Drawer>
    
      <Card type="inner" title="Coverage" style={{ marginTop: 16 }}>
        <Button onClick={async ()=>{
          const data = await client.getCoverage(ruleId)
          Modal.info({
            width: 720,
            title: 'Fields Coverage',
            content: <Table
              size="small"
              rowKey="field"
              dataSource={data}
              pagination={{ pageSize: 10 }}
              columns={[
                { title: 'Field', dataIndex: 'field' },
                { title: 'Tested', dataIndex: 'tested' },
                { title: 'Mutated', dataIndex: 'mutated' },
                { title: 'Hits', dataIndex: 'hits' },
                { title: 'Pass', dataIndex: 'pass' },
              ]}
            />,
          })
        }}>Compute Coverage</Button>
      </Card>

    
      <Drawer title="Coverage Heatmap" open={heatOpen} onClose={()=>setHeatOpen(false)} width={'85%'}>
        <CoverageHeatmap
          client={client}
          ruleId={ruleId}
          tests={tests.map(t => ({ id: t.id, name: t.name, expect: t.expect }))}
          results={results}
          onRunAll={async (opts)=>{
            setBusy(true)
            try {
              const res = await client.runTests(ruleId, { maxParallelism: opts?.maxParallelism })
              const map = {} as any
              res.forEach(r => map[r.name] = r)
              setResults(map)
            } finally { setBusy(false) }
          }}
        />
      </Drawer>

    </Card>
  )
}


  async function onGenerateAI() {
    setAiBusy(true)
    try {
      const res = await client.suggestTests(ruleId, { userPrompt: aiPrompt || undefined, budget: aiBudget, temperature: aiTemp })
      setAiList(res.suggestions)
      setAiSel([])
      setAiUsage(res.usage)
      setAiModel(res.model)
      setAiTotalSkel(res.totalSkeletons)
    } finally { setAiBusy(false) }
  }
  async function onImportAI() {
    setAiBusy(true)
    try {
      const ids = aiSel.map(String)
      const r = await client.importSuggestedTests(ruleId, ids, { suite: 'ai', tags: ['ai'] })
      // refresh tests after import
      await load()
    } finally { setAiBusy(false) }
  }
