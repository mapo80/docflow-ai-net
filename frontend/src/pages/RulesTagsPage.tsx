import { useEffect, useState } from 'react';
import { Button, Card, Col, Form, Input, Row, Space, Table, Tag } from 'antd';
import { SuitesService, TagsService, type SuiteUpsert, type TagUpsert } from '../generated';
import { notify } from '../components/notification';

interface Suite {
  id: string;
  name: string;
  color?: string;
  description?: string;
}

interface TagItem {
  id: string;
  name: string;
  color?: string;
  description?: string;
}

export default function RulesTagsPage() {
  const [suites, setSuites] = useState<Suite[]>([]);
  const [tags, setTags] = useState<TagItem[]>([]);
  const [sform] = Form.useForm<SuiteUpsert>();
  const [tform] = Form.useForm<TagUpsert>();

  const load = async () => {
    try {
      const [s, t] = await Promise.all([
        SuitesService.getApiV1Suites(),
        TagsService.getApiV1Tags(),
      ]);
      setSuites(s);
      setTags(t);
    } catch {
      notify('error', 'Failed to load taxonomies.');
    }
  };

  useEffect(() => {
    void load();
  }, []);

  const addSuite = async (values: SuiteUpsert) => {
    try {
      await SuitesService.postApiV1Suites({ requestBody: values });
      notify('success', 'Suite created successfully.');
      sform.resetFields();
      await load();
    } catch {
      notify('error', 'Failed to create suite.');
    }
  };

  const addTag = async (values: TagUpsert) => {
    try {
      await TagsService.postApiV1Tags({ requestBody: values });
      notify('success', 'Tag created successfully.');
      tform.resetFields();
      await load();
    } catch {
      notify('error', 'Failed to create tag.');
    }
  };

  const deleteSuite = async (id: string) => {
    try {
      await SuitesService.deleteApiV1Suites({ id });
      notify('success', 'Suite deleted successfully.');
      await load();
    } catch {
      notify('error', 'Failed to delete suite.');
    }
  };

  const deleteTag = async (id: string) => {
    try {
      await TagsService.deleteApiV1Tags({ id });
      notify('success', 'Tag deleted successfully.');
      await load();
    } catch {
      notify('error', 'Failed to delete tag.');
    }
  };

  return (
    <Row gutter={16}>
      <Col xs={24} md={12}>
        <Card title="Suites" extra={<Button onClick={load}>Refresh</Button>}>
          <Table
            rowKey="id"
            dataSource={suites}
            pagination={false}
            columns={[
              { title: 'Name', dataIndex: 'name' },
              {
                title: 'Color',
                dataIndex: 'color',
                render: (c: string) => (c ? <Tag color={c}>{c}</Tag> : '-'),
              },
              { title: 'Description', dataIndex: 'description' },
              {
                title: 'Actions',
                render: (_: unknown, row: Suite) => (
                  <Space>
                    <Button size="small" onClick={() => deleteSuite(row.id)}>
                      Delete
                    </Button>
                  </Space>
                ),
              },
            ]}
            locale={{ emptyText: 'No data' }}
          />
          <Form form={sform} layout="inline" onFinish={addSuite} style={{ marginTop: 12 }}>
            <Form.Item name="name" rules={[{ required: true }]}> 
              <Input placeholder="Suite name" />
            </Form.Item>
            <Form.Item name="color">
              <Input placeholder="#1677ff / red ..." />
            </Form.Item>
            <Form.Item name="description">
              <Input placeholder="Description" />
            </Form.Item>
            <Form.Item>
              <Button htmlType="submit" type="primary">
                Add Suite
              </Button>
            </Form.Item>
          </Form>
        </Card>
      </Col>
      <Col xs={24} md={12}>
        <Card title="Tags" extra={<Button onClick={load}>Refresh</Button>}>
          <Table
            rowKey="id"
            dataSource={tags}
            pagination={false}
            columns={[
              { title: 'Name', dataIndex: 'name' },
              {
                title: 'Color',
                dataIndex: 'color',
                render: (c: string) => (c ? <Tag color={c}>{c}</Tag> : '-'),
              },
              { title: 'Description', dataIndex: 'description' },
              {
                title: 'Actions',
                render: (_: unknown, row: TagItem) => (
                  <Space>
                    <Button size="small" onClick={() => deleteTag(row.id)}>
                      Delete
                    </Button>
                  </Space>
                ),
              },
            ]}
            locale={{ emptyText: 'No data' }}
          />
          <Form form={tform} layout="inline" onFinish={addTag} style={{ marginTop: 12 }}>
            <Form.Item name="name" rules={[{ required: true }]}> 
              <Input placeholder="Tag name" />
            </Form.Item>
            <Form.Item name="color">
              <Input placeholder="#52c41a / green ..." />
            </Form.Item>
            <Form.Item name="description">
              <Input placeholder="Description" />
            </Form.Item>
            <Form.Item>
              <Button htmlType="submit" type="primary">
                Add Tag
              </Button>
            </Form.Item>
          </Form>
        </Card>
      </Col>
    </Row>
  );
}
