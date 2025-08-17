
import React, { useEffect, useState } from 'react'
import { Button, Card, Drawer, Form, Input, InputNumber, Modal, Select, Space, Switch, Table, Tag, message, Progress, Tabs } from 'antd'
import { useRulesClient } from './client'

export default function AdminLlm(){
  const client = useRulesClient()
  const [list, setList] = useState<any[]>([])
  const [loading, setLoading] = useState(false)
  const [editing, setEditing] = useState<any|null>(null)
  const [open, setOpen] = useState(false)
  const [turbo, setTurbo] = useState(false)
  const [form] = Form.useForm()
  const [gguf, setGguf] = useState<any[]>([])
  const [dlOpen, setDlOpen] = useState(false)
  const [dlRepo, setDlRepo] = useState('')
  const [dlFile, setDlFile] = useState('')
  const [dlRev, setDlRev] = useState('main')
  const [dlJob, setDlJob] = useState<any|null>(null)

  async function load(){
    setLoading(true)
    try { const data = await client.listLlmModels?.(); setList(data || []) } finally { setLoading(false) }
  }
  async function refreshGguf(){ try { const data = await client.listGgufAvailable?.(); setGguf(data || []) } catch{} }

  useEffect(()=>{ load(); refreshGguf() }, [])

  function onNew(){ setEditing(null); form.resetFields(); form.setFieldsValue({ provider:'LlamaSharp', enabled:true }); setOpen(true) }
  function onEdit(rec:any){ setEditing(rec); form.setFieldsValue(rec); setOpen(true) }

  async function onSave(){
    const v = await form.validateFields()
    if (editing) await client.updateLlmModel?.(editing.id, v)
    else await client.createLlmModel?.(v)
    message.success('Salvato')
    setOpen(false); await load()
  }

  async function onDelete(rec:any){
    Modal.confirm({ title:'Eliminare modello?', onOk: async()=>{ await client.deleteLlmModel?.(rec.id); message.success('Eliminato'); await load() } })
  }
  async function onActivate(rec:any){
    await client.activateLlmModel?.(rec.id, turbo); message.success('Modello attivato' + (turbo?' (turbo)':''))
  }
  async function onWarmup(rec?:any){
    await client.warmupLlmModel?.(rec?.id); message.info('Warmup richiesto')
  }

  return <Card title="LLM Admin">
    <Tabs items={[
      { key:'models', label:'Models', children:
        <div>
          <Space style={{ marginBottom: 12 }}>
            <Button type='primary' onClick={onNew}>Nuovo modello</Button>
            <Switch checked={turbo} onChange={setTurbo} checkedChildren='Turbo' unCheckedChildren='Standard' />
            <Button onClick={()=>onWarmup()}>Warmup active</Button>
          </Space>
          <Table loading={loading} rowKey='id' dataSource={list} columns={[
            { title:'Name', dataIndex:'name' },
            { title:'Provider', dataIndex:'provider', render:(v)=> <Tag>{v}</Tag>, width: 120 },
            { title:'Model/Path', dataIndex:'modelPathOrId', ellipsis: true },
            { title:'Endpoint', dataIndex:'endpoint', ellipsis: true },
            { title:'MaxTokens', dataIndex:'maxTokens', width: 120 },
            { title:'Temp', dataIndex:'temperature', width: 100 },
            { title:'Warmup', dataIndex:'warmupOnStart', render:(v)=> v?'Yes':'No', width: 100 },
            { title:'Enabled', dataIndex:'enabled', render:(v)=> v?<Tag color='green'>On</Tag>:<Tag color='red'>Off</Tag>, width: 90 },
            { title:'', render:(_:any,rec:any)=> <Space><Button size='small' onClick={()=>onEdit(rec)}>Edit</Button><Button size='small' onClick={()=>onActivate(rec)}>Activate</Button><Button size='small' onClick={()=>onWarmup(rec)}>Warmup</Button><Button size='small' danger onClick={()=>onDelete(rec)}>Delete</Button></Space>, width: 300 },
          ]} />

          <Drawer open={open} onClose={()=>setOpen(false)} title={editing? 'Modifica modello':'Nuovo modello'} width={520} extra={<Space><Button onClick={()=>setOpen(false)}>Chiudi</Button><Button type='primary' onClick={onSave}>Salva</Button></Space>}>
            <Form layout='vertical' form={form}>
              <Form.Item name='provider' label='Provider' rules={[{ required:true }]} initialValue='LlamaSharp'>
                <Select options={[ { value:'LlamaSharp' }, { value:'OpenAI' }, { value:'Mock' } ]} />
              </Form.Item>
              <Form.Item name='name' label='Name' rules={[{ required:true }]}><Input/></Form.Item>

              <Form.Item noStyle shouldUpdate={(p,c)=>p.provider!==c.provider}></Form.Item>
              { (form.getFieldValue('provider') === 'LlamaSharp') ? (
                <Form.Item name='modelPathOrId' label='Model GGUF'>
                  <Select showSearch placeholder='Seleziona GGUF' options={(gguf||[]).map((x:any)=>({ value:x.path, label:`${x.name} (${(x.size/1e6).toFixed(1)} MB)` }))} dropdownRender={menu=>(<div>{menu}<div style={{ padding:8 }}><Space><Button size='small' onClick={refreshGguf}>Refresh</Button><Button size='small' onClick={()=>setDlOpen(true)}>Scarica da HF</Button></Space></div></div>)} />
                  <Space style={{ marginTop: 8 }}>
                    <Button danger size='small' onClick={async()=>{
                      const p = form.getFieldValue('modelPathOrId'); if(!p){ message.warning('Seleziona un GGUF'); return }
                      Modal.confirm({ title:'Confermi cancellazione GGUF?', content:p, okText:'Elimina', okButtonProps:{ danger:true }, onOk: async()=>{ try{ await client.deleteGgufAvailable?.(p); message.success('Eliminato'); form.setFieldValue('modelPathOrId', undefined); await refreshGguf() } catch(e:any){ message.error(e?.data?.error || e?.message || 'Errore') } } })
                    }}>Elimina GGUF</Button>
                  </Space>
                </Form.Item>
              ) : (
                <Form.Item name='modelPathOrId' label='Model (ID)'><Input/></Form.Item>
              ) }

              <Form.Item name='endpoint' label='Endpoint (OpenAI/Azure)'><Input/></Form.Item>
              <Form.Item name='apiKey' label='API Key (OpenAI)'><Input.Password/></Form.Item>
              <Form.Item name='contextSize' label='ContextSize'><InputNumber min={128} max={32768} style={{ width:'100%' }}/></Form.Item>
              <Form.Item name='threads' label='Threads'><InputNumber min={1} max={128} style={{ width:'100%' }}/></Form.Item>
              <Form.Item name='batchSize' label='BatchSize'><InputNumber min={1} max={8192} style={{ width:'100%' }}/></Form.Item>
              <Form.Item name='maxTokens' label='MaxTokens'><InputNumber min={64} max={8192} style={{ width:'100%' }}/></Form.Item>
              <Form.Item name='temperature' label='Temperature'><InputNumber min={0} max={2} step={0.05} style={{ width:'100%' }}/></Form.Item>
              <Form.Item name='warmupOnStart' label='Warmup on Start' valuePropName='checked'><Switch/></Form.Item>
              <Form.Item name='enabled' label='Enabled' valuePropName='checked' initialValue={true}><Switch/></Form.Item>
            </Form>
          </Drawer>

          <Modal title='Scarica da Hugging Face' open={dlOpen} onCancel={()=>setDlOpen(false)} onOk={async()=>{
            try{
              const { jobId } = await client.startGgufDownload?.(dlRepo, dlFile, dlRev || 'main')
              setDlJob({ id: jobId, status:'queued', progress:0 })
              const t = setInterval(async()=>{
                const j = await client.getGgufJob?.(jobId); if(j) setDlJob(j)
                if (j && (j.status==='succeeded' || j.status==='failed')){ clearInterval(t); await refreshGguf(); message.info(j.status==='succeeded'?'Download completato':'Errore download') }
              }, 1000)
            }catch(e:any){ message.error(e?.message || 'Errore') }
          }} okText='Avvia download'>
            <Space direction='vertical' style={{ width:'100%' }}>
              <Input placeholder='repo (es. owner/repo)' value={dlRepo} onChange={e=>setDlRepo(e.target.value)} />
              <Input placeholder='file (es. model.q4_k_m.gguf)' value={dlFile} onChange={e=>setDlFile(e.target.value)} />
              <Input placeholder='revision (es. main)' value={dlRev} onChange={e=>setDlRev(e.target.value)} />
              { dlJob && <div>Stato: <b>{dlJob.status}</b> <Progress percent={dlJob.progress} size='small' /></div> }
            </Space>
          </Modal>
        </div>
      },
      { key:'gguf', label:'GGUF Files', children: <GgufTab /> }
    ]} />
  </Card>
}

function GgufTab(){
  const client = useRulesClient()
  const [rows, setRows] = React.useState<any[]>([])
  const [loading, setLoading] = React.useState(false)
  async function load(){ setLoading(true); try { const x = await client.listGgufAvailable?.(); setRows(x || []) } finally { setLoading(false) } }
  React.useEffect(()=>{ load() }, [])
  return <div>
    <Space style={{ marginBottom: 12 }}><Button onClick={load}>Refresh</Button></Space>
    <Table loading={loading} rowKey={(r:any)=>r.path} dataSource={rows} columns={[
      { title:'Name', dataIndex:'name' },
      { title:'Size (MB)', dataIndex:'size', render:(v:any)=> (v/1e6).toFixed(1), width:120 },
      { title:'Modified (UTC)', dataIndex:'modified', width:220 },
      { title:'Path', dataIndex:'path', ellipsis:true },
      { title:'', render:(_:any,rec:any)=> <Space><Button danger size='small' onClick={()=>{
        Modal.confirm({ title:'Confermi cancellazione?', content: rec.path, okButtonProps:{ danger:true }, okText:'Elimina', onOk: async()=>{
          try { await client.deleteGgufAvailable?.(rec.path); message.success('Eliminato'); load() } catch(e:any){ message.error(e?.data?.error || e?.message || 'Errore') }
        }})
      }}>Elimina</Button></Space>, width:120 }
    ]} />
  </div>
}
