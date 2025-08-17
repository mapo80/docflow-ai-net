import { useEffect, useState } from 'react'
import { Button, Card, Col, Form, Input, Row, Space, Table, Tag, message } from 'antd'
import type { Suite, Tag as TagType } from '@docflow/rules-client'
import { createRulesClient } from '@docflow/rules-client'

const client = createRulesClient('/api', import.meta.env.VITE_API_KEY)

export default function Taxonomies() {
  const [suites, setSuites] = useState<Suite[]>([])
  const [tags, setTags] = useState<TagType[]>([])
  const [sform] = Form.useForm()
  const [tform] = Form.useForm()

  const load = async () => {
    setSuites(await client.listSuites())
    setTags(await client.listTags())
  }
  useEffect(()=>{ load() }, [])

  return (
    <Row gutter={16}>
      <Col span={12}>
        <Card title="Suites" extra={<Button onClick={load}>Refresh</Button>}>
          <Table
            rowKey="id"
            dataSource={suites}
            columns={[
              { title: 'Name', dataIndex: 'name' },
              { title: 'Color', dataIndex: 'color', render: (c)=> c ? <Tag color={c}>{c}</Tag> : '-' },
              { title: 'Description', dataIndex: 'description' },
              { title: 'Actions', render: (_:any,row:Suite)=><Space>
                <Button size="small" onClick={async()=>{ await client.deleteSuite(row.id); message.success('Deleted'); await load() }}>Delete</Button>
              </Space> }
            ]}
          />
          <Form form={sform} layout="inline" onFinish={async v=>{ await client.createSuite(v); message.success('Created'); sform.resetFields(); await load() }} style={{ marginTop: 12 }}>
            <Form.Item name="name" rules={[{required:true}]}><Input placeholder="Suite name" /></Form.Item>
            <Form.Item name="color"><Input placeholder="#1677ff / red ..." /></Form.Item>
            <Form.Item name="description"><Input placeholder="Description" /></Form.Item>
            <Form.Item><Button htmlType="submit" type="primary">Add Suite</Button></Form.Item>
          </Form>
        </Card>
      </Col>
      <Col span={12}>
        <Card title="Tags" extra={<Button onClick={load}>Refresh</Button>}>
          <Table
            rowKey="id"
            dataSource={tags}
            columns={[
              { title: 'Name', dataIndex: 'name' },
              { title: 'Color', dataIndex: 'color', render: (c)=> c ? <Tag color={c}>{c}</Tag> : '-' },
              { title: 'Description', dataIndex: 'description' },
              { title: 'Actions', render: (_:any,row:TagType)=><Space>
                <Button size="small" onClick={async()=>{ await client.deleteTag(row.id); message.success('Deleted'); await load() }}>Delete</Button>
              </Space> }
            ]}
          />
          <Form form={tform} layout="inline" onFinish={async v=>{ await client.createTag(v); message.success('Created'); tform.resetFields(); await load() }} style={{ marginTop: 12 }}>
            <Form.Item name="name" rules={[{required:true}]}><Input placeholder="Tag name" /></Form.Item>
            <Form.Item name="color"><Input placeholder="#52c41a / green ..." /></Form.Item>
            <Form.Item name="description"><Input placeholder="Description" /></Form.Item>
            <Form.Item><Button htmlType="submit" type="primary">Add Tag</Button></Form.Item>
          </Form>
        </Card>
      </Col>
    </Row>
  )
}
