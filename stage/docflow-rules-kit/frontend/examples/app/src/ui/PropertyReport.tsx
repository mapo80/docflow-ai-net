import { ResponsiveContainer, PieChart, Pie, Cell, Tooltip, Legend, BarChart, Bar, XAxis, YAxis, CartesianGrid } from 'recharts'
import React from 'react'
import { Button, Card, Col, Modal, Row, Space, Statistic, Table, Tag, Input, InputNumber, Select, message } from 'antd'
import { useRulesClient } from './client'

export default function PropertyReport({ ruleId }: { ruleId: string }){
  const client = useRulesClient()
  const [loading, setLoading] = React.useState(false)
  const [trials, setTrials] = React.useState(100)
  const [seed, setSeed] = React.useState<number | undefined>(undefined)
  const [data, setData] = React.useState<any|null>(null)
  const [suite, setSuite] = React.useState<string>('property-fails')
  const [tags, setTags] = React.useState<string[]>(['property'])
  const [suites, setSuites] = React.useState<any[]>([])
  const [allTags, setAllTags] = React.useState<any[]>([])
  const [selKeys, setSelKeys] = React.useState<React.Key[]>([])

  React.useEffect(()=>{(async()=>{ try { const [s,t] = await Promise.all([client.listSuites(), client.listTags()]); setSuites(s); setAllTags(t) } catch{} })()},[])

  async function run(){
    setLoading(true)
    try {
      const res = await client.runProperties(ruleId, trials, seed)
      setData(res)
      if (res.failed>0) message.warning(`${res.failed} proprietà fallite`)
      else message.success('Tutte le proprietà verdi')
    } finally { setLoading(false) }
  }

  async function importFailures(){
    if (!data?.failures?.length) return
    const picked = data.failures.filter((_:any,idx:number)=> selKeys.includes(idx))
    const res = await client.importPropertyFailures(ruleId, picked, suite || undefined, (tags && tags.length>0) ? tags : undefined)
    message.success(`Importati ${res.imported} test`)
  }

  return <Card title="Property Report" extra={<Space>    <span>Trials</span><InputNumber min={10} max={5000} value={trials} onChange={(v)=>setTrials(Number(v))} />    <span>Seed</span><InputNumber value={seed as any} onChange={(v)=>setSeed(v as number)} />    <Button type='primary' onClick={run} loading={loading}>Run</Button>  </Space>}>

    <Row gutter={16} style={{ marginBottom: 12 }}>
      <Col span={12}>
        <Card size="small" title="Pass/Fail">
          <div style={{ height: 220 }}>
            <ResponsiveContainer width="100%" height="100%">
              <PieChart>
                <Pie data={[{ name: 'Passed', value: data?.passed || 0 }, { name: 'Failed', value: data?.failed || 0 }]} dataKey="value" nameKey="name" outerRadius={80} label />
                <Tooltip /><Legend />
              </PieChart>
            </ResponsiveContainer>
          </div>
        </Card>
      </Col>
      <Col span={12}>
        <Card size="small" title="Failures by property">
          <div style={{ height: 220 }}>
            <ResponsiveContainer width="100%" height="100%">
              <BarChart data={Object.entries((data?.failures||[]).reduce((acc:any, f:any)=>{ acc[f.property]=(acc[f.property]||0)+1; return acc }, {})).map(([k,v])=>({ property:k, count:v as number }))}>
                <CartesianGrid strokeDasharray="3 3" />
                <XAxis dataKey="property" />
                <YAxis allowDecimals={false} />
                <Tooltip />
                <Legend />
                <Bar dataKey="count" />
              </BarChart>
            </ResponsiveContainer>
          </div>
        </Card>
      </Col>
    </Row>
      <Col span={6}><Statistic title="Trials" value={data?.trials ?? 0} /></Col>      <Col span={6}><Statistic title="Passed" value={data?.passed ?? 0} /></Col>      <Col span={6}><Statistic title="Failed" value={data?.failed ?? 0} valueStyle={{ color: (data?.failed ?? 0)>0 ? 'red' : undefined }} /></Col>    </Row>

    <Space style={{ marginTop: 12, marginBottom: 8 }} wrap>
      <span>Suite:</span>
      <Select
        style={{ minWidth: 200 }}
        value={suite}
        onChange={setSuite}
        showSearch
        options={[{ value: 'property-fails', label: 'property-fails' }, ...suites.map(s=>({ value: s.name, label: s.name }))]}
      />
      <span>Tags:</span>
      <Select
        mode="tags"
        style={{ minWidth: 260 }}
        value={tags}
        onChange={(v)=>setTags(v)}
        options={allTags.map((t:any)=>({ value: t.name, label: t.name }))}
        tokenSeparators={[',']}
        placeholder="Aggiungi o seleziona tag"
      />
    </Space>
    <Table rowKey={(r:any,idx)=> idx} dataSource={data?.failures ?? []} rowSelection={{ selectedRowKeys: selKeys, onChange: setSelKeys }} columns={[
      { title:'Property', dataIndex:'property', render:(v:string)=> <Tag color='red'>{v}</Tag>, width:180 },
      { title:'Message', dataIndex:'message' },
      { title:'Input (preview)', dataIndex:'counterexample', render:(v:any)=> <pre style={{ whiteSpace:'pre-wrap', maxWidth: 480 }}>{JSON.stringify(v, null, 2)}</pre> }
    ]} />
    <Space>      <Button disabled={!data?.failures?.length} onClick={()=> setSelKeys((data?.failures||[]).map((_:any,idx:number)=> idx))}>Seleziona tutti</Button>      <Button type='primary' disabled={!selKeys.length} onClick={importFailures}>Importa come test</Button>    </Space>
  </Card>
}
