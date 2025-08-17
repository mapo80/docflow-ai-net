import React from 'react'
import { Button, Card, Input, Space, Table, Modal, message } from 'antd'
import { Link, useNavigate } from 'react-router-dom'
import { useRulesClient } from './client'

export default function RulesPage(){
  const client = useRulesClient()
  const [rows, setRows] = React.useState<any[]>([])
  const [search, setSearch] = React.useState('')
  const nav = useNavigate()

  async function load(){ const x = await client.listRules({ search }); setRows(x) }
  React.useEffect(()=>{ load() }, [search])

  return <Card title="Rules">
    <Space style={{ marginBottom: 12 }}>
      <Input placeholder="search" value={search} onChange={e=>setSearch(e.target.value)} />
      <Button onClick={()=>nav('/builder')}>No-code Builder</Button>
    </Space>
    <Table rowKey={(r:any)=>r.id} dataSource={rows} columns={[
      { title:'Name', dataIndex:'name', render:(v:any, r:any)=> <Link to={`/rules/${r.id}`}>{v}</Link> },
      { title:'Version', dataIndex:'version', width:120 },
      { title:'Enabled', dataIndex:'enabled', width:100 },
      { title:'', width:280, render:(_:any, r:any)=> <Space>        <Button onClick={()=>nav(`/rules/${r.id}`)}>Open</Button>        <Button onClick={async()=>{ const { id } = await client.cloneRule(r.id, r.name+' (copy)', true); message.success('Cloned'); nav(`/rules/${id}`) }}>Clone + tests</Button>      </Space> }
    ]} />
  </Card>
}
