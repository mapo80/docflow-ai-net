import { useEffect, useState } from 'react'
import { Button, Card, Form, Input, Modal, Select, Space, Table, Tag, message } from 'antd'
import { createRulesClient } from '@docflow/rules-client'
import { useAuth } from './Auth'

type User = { id: string; username: string; email?: string; createdAt: string; roles: string[] }
const roles = ['viewer','editor','reviewer','admin']

export default function AdminUsers() {
  const auth = useAuth()
  const client = createRulesClient('/api', undefined, ()=>auth.token)
  const [data, setData] = useState<{ total: number; page: number; pageSize: number; items: User[] }>({ total:0, page:1, pageSize:20, items:[] })
  const [search, setSearch] = useState('')
  const [open, setOpen] = useState(false)
  const [form] = Form.useForm()

  const load = async (page=1) => {
    const res = await fetch(`/api/identity/users?search=${encodeURIComponent(search)}&page=${page}&pageSize=20`, { headers: { Authorization: `Bearer ${auth.token}` } })
    const json = await res.json()
    setData(json)
  }

  useEffect(()=>{ load() }, [])

  return (
    <Card title="Users & Roles" extra={<Space><Input placeholder="Search..." onChange={e=>setSearch(e.target.value)} onPressEnter={()=>load(1)} /><Button onClick={()=>setOpen(true)}>New User</Button><Button onClick={()=>load(data.page)}>Refresh</Button></Space>}>
      <Table<User>
        rowKey="id"
        dataSource={data.items}
        pagination={{ current: data.page, total: data.total, pageSize: data.pageSize, onChange: p => load(p) }}
        columns={[
          { title: 'Username', dataIndex: 'username' },
          { title: 'Email', dataIndex: 'email' },
          { title: 'Roles', dataIndex: 'roles', render: (r:string[]) => (r||[]).map(x => <Tag key={x}>{x}</Tag>) },
          { title: 'Actions', render: (_:any,row:User)=><Space>
            <Button size="small" onClick={async ()=>{
              Modal.confirm({ title: `Delete ${row.username}?`, onOk: async()=>{ await fetch(`/api/identity/users/${row.id}`, { method:'DELETE', headers: { Authorization: `Bearer ${auth.token}` } }); message.success('Deleted'); await load(data.page) } })
            }}>Delete</Button>
            <Button size="small" onClick={async ()=>{
              const v:any = await Form.useWatch('', form)
            }}>Edit</Button>
          </Space> }
        ]}
      />
      <Modal title="Create user" open={open} onCancel={()=>setOpen(false)} onOk={async ()=>{
        const v = await form.validateFields()
        await fetch('/api/identity/users', { method:'POST', headers: { 'Content-Type':'application/json', Authorization: `Bearer ${auth.token}` }, body: JSON.stringify({ username: v.username, email: v.email, roles: v.roles }) })
        message.success('Created'); setOpen(false); form.resetFields(); await load(1)
      }}>
        <Form form={form} layout="vertical">
          <Form.Item name="username" label="Username" rules={[{ required: true }]}><Input /></Form.Item>
          <Form.Item name="email" label="Email"><Input type="email" /></Form.Item>
          <Form.Item name="roles" label="Roles"><Select mode="multiple" options={roles.map(r=>({value:r,label:r}))} /></Form.Item>
        </Form>
      </Modal>
    </Card>
  )
}
